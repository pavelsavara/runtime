/* eslint-disable no-console */
/* eslint-disable @typescript-eslint/no-unused-vars */
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import cwraps from "./cwraps";
import { getU32, localHeapViewU8 } from "./memory";
import { utf8ToString } from "./strings";
import { BlobBuilder } from "./jiterpreter-support";
import { enumerateProxies } from "./gc-handles";
import type { VoidPtr, ManagedPointer, CharPtr } from "./types/emscripten";
import { Module, runtimeHelpers } from "./globals";

const packetBuilderCapacity = 65536;
const stringTable = new Map<string, number>();
const assemblies = new Map<string, number>();
const classes = new Map<string, number>();
const incompletePackets = new Map<string, BlobBuilder>();
const formatVersion = 1;

const nameFromKind = ["unknown", "def", "gtd", "ginst", "gparam", "array", "pointer"];
const handleTypes = ["weak", "weak_track", "normal", "pinned", "weak_fields"];

let totalObjects = 0, totalRefs = 0, totalClasses = 0, totalAssemblies = 0, totalRoots = 0;
let mostRecentObjectPointer : ManagedPointer = <any>0;


export function mono_wasm_perform_heapshot () {
    cwraps.mono_wasm_perform_heapshot();
}

export function mono_wasm_heapshot_assembly (assembly: VoidPtr, pName: CharPtr) {
    const name = utf8ToString(pName);
    if(assemblies.has(name)){
        return;
    }
    assemblies.set(name, 1);
    const builder = getBuilder("ASSM");
    totalAssemblies += 1;
    builder.appendU32(<any>assembly);
    // No point in using the string table for this
    builder.appendName(name);
}

export function mono_wasm_heapshot_class (klass: VoidPtr, elementKlass: VoidPtr, nestingKlass: VoidPtr, assembly: VoidPtr, pNamespace: CharPtr, pName: CharPtr, rank: number, kind: number, numGps: number, pGp: VoidPtr): void {
    const builder = getBuilder("TYPE");
    const kindName = nameFromKind[kind] || "unknown";
    const name = utf8ToString(pName);
    const ns = utf8ToString(pNamespace);
    const classId = `${ns}-${name}-${klass}-${elementKlass}-${nestingKlass}-${assembly}-${rank}-${kindName}`;
    if(classes.has(classId)){
        return;
    }
    classes.set(classId, 1);
    totalClasses += 1;
    builder.appendU32(<any>klass);
    builder.appendU32(<any>elementKlass);
    builder.appendU32(<any>nestingKlass);
    builder.appendU32(<any>assembly);
    builder.appendULeb(rank);
    builder.appendULeb(getStringTableIndex(kindName));
    builder.appendULeb(getStringTableIndex(ns));
    // We use the string table for names because each generic instance will have the same name
    builder.appendULeb(getStringTableIndex(name));
    builder.appendULeb(numGps);
    for (let i = 0; i < numGps; i++) {
        const gp = getU32(<any>pGp + (i * 4));
        builder.appendU32(gp);
    }
}

// NOTE: for objects (like arrays) containing more than 128 refs, this will get invoked multiple times
export function mono_wasm_heapshot_object (pObj: ManagedPointer, klass: VoidPtr, size: number, numRefs: number, pRefs: VoidPtr): void {
    // The object header and its refs are stored in separate streams
    if (pObj !== mostRecentObjectPointer) {

        totalObjects += 1;
        const objBuilder = getBuilder("OBJH", packetBuilderCapacity*256);
        objBuilder.appendU32(<any>pObj);
        objBuilder.appendU32(<any>klass);
        objBuilder.appendULeb(size);
        mostRecentObjectPointer = pObj;
    }

    totalRefs += numRefs;
    if (numRefs < 1)
        return;
    if (numRefs > 16) {
        numRefs = 16;
    }

    const refBuilder = getBuilder("REFS", packetBuilderCapacity*256*256);
    refBuilder.appendU32(<any>pObj);
    refBuilder.appendULeb(numRefs);
    for (let i = 0; i < numRefs; i++) {
        const pRef = getU32(<any>pRefs + (i * 4));
        refBuilder.appendU32(pRef);
    }
}

export function mono_wasm_heapshot_gchandle (pObj: ManagedPointer, handleType: number): void {
    const handleTypeName = handleTypes[handleType] || "unknown";
    const builder = getBuilder("GCHL");
    builder.appendULeb(getStringTableIndex(handleTypeName));
    builder.appendU32(<any>pObj);
}

function getStringTableIndex (text: string) {
    if (text.length === 0)
        return 0;

    let index = stringTable.get(text);
    if (!index) {
        index = (stringTable.size + 1);
        stringTable.set(text, index);
        const builder = getBuilder("STBL", packetBuilderCapacity*16);
        builder.appendULeb(index);
        builder.appendName(text);
    }

    return index >>> 0;
}

function utf8ToStringTableIndex (pText: CharPtr) {
    if (!pText)
        return 0;

    const text = utf8ToString(pText);
    return getStringTableIndex(text);
}

export function mono_wasm_heapshot_roots (kind: CharPtr, count: number, pAddresses: VoidPtr, pObjects: VoidPtr): void {
    const builder = getBuilder("ROOT");
    builder.appendULeb(utf8ToStringTableIndex(kind));
    builder.appendULeb(count);
    totalRoots += count;
    for (let i = 0; i < count; i++) {
        const addr = getU32(<any>pAddresses + (i * 4));
        const obj = getU32(<any>pObjects + (i * 4));
        builder.appendU32(addr);
        builder.appendU32(obj);
    }
}

export function mono_wasm_heapshot_start (): void {
    stringTable.clear();
    for (const kvp of incompletePackets)
        kvp[1].clear();
    totalObjects = totalRefs = totalClasses = totalAssemblies = totalRoots = 0;
    mostRecentObjectPointer = <any>0;
}

function pagesToMegabytes (pages: number) {
    return (pages * 65536 / (1024 * 1024)).toFixed(1);
}

function bytesToMegabytes (bytes: number) {
    return (bytes / (1024 * 1024)).toFixed(1);
}

function getBuilder (chunkId: string, capacity:number=packetBuilderCapacity) {
    let result = incompletePackets.get(chunkId);
    if (!result) {
        result = new BlobBuilder(capacity);
        incompletePackets.set(chunkId, result);
    } 
    return result;
}


function heapshotCounter (name: string, value: number) {
    const builder = getBuilder("CNTR");
    // Using the stringtable here would just make the format harder to decode.
    builder.appendName(name);
    // We use F64 because some counters (like jiterpreter counters) are not int32's
    builder.appendF64(value);
}

export function mono_wasm_heapshot_counter (pName: CharPtr, value: number): void {
    heapshotCounter(`mono/${utf8ToString(pName)}`, value);
}

export function mono_wasm_heapshot_stats (largeObjectHeapSize: number, sgenHeapCapacity: number): void {
    heapshotCounter("sgen/heap-capacity", sgenHeapCapacity);
    heapshotCounter("sgen/los-size", largeObjectHeapSize);
}

function recordObject (chunkId: string, handle: number, obj: any) {
    const builder = getBuilder(chunkId);
    builder.appendU32(handle);
    builder.appendULeb(getStringTableIndex(typeof (obj)));
    // We can't use obj.toString since for certain types it's defined to generate an absolute truckload of text
    let name = obj && obj.constructor && obj.constructor.name
        ? obj.constructor.name
        : (obj && obj[Symbol.toStringTag]
            ? obj[Symbol.toStringTag]
            : "unknown"
        );
    if ((typeof (obj) === "function") && obj.name)
        name = `function ${obj.name}`;
    builder.appendULeb(getStringTableIndex(name));
}

function mono_wasm_heapshot_cs_object (handle: number, proxy: any) {
    recordObject("CSOB", handle, proxy);
}

function mono_wasm_heapshot_js_object (handle: number, obj: any) {
    recordObject("JSOB", handle, obj);
}

export function mono_wasm_heapshot_end (): void {
    heapshotCounter("snapshot/version", formatVersion);
    heapshotCounter("snapshot/num-strings", stringTable.size);
    heapshotCounter("snapshot/num-objects", totalObjects);
    heapshotCounter("snapshot/num-refs", totalRefs);
    heapshotCounter("snapshot/num-roots", totalRoots);
    heapshotCounter("snapshot/num-classes", totalClasses);
    heapshotCounter("snapshot/num-assemblies", totalAssemblies);
    enumerateProxies(mono_wasm_heapshot_cs_object, mono_wasm_heapshot_js_object);

    const blobParts:Uint8Array[] = [];

    // write out all the incomplete packets
    for (const kvp of incompletePackets) {
        const builder = kvp[1];
        const chunkId = kvp[0];
        if (builder && (builder.size > 0)) {
            const chunk = builder.getArrayView(false);

            const headerArray = new Uint8Array(8);
            const headerView = new DataView(headerArray.buffer);
            for (let i = 0; i < 4; i++)
                headerArray[i] = chunkId.charCodeAt(i);
            headerView.setUint32(4, chunk.length, true);

            if (chunkId === "CNTR") {
                blobParts.splice(0, 0, headerArray.slice(), chunk);
            } else {
                blobParts.push(headerArray.slice());
                blobParts.push(chunk);
            }
            builder.clear();
        }
    }
    incompletePackets.clear();
    downloadChunk (blobParts, `${(new Date).toISOString().replace(":", "-")}.mono-heap`);
}

export function mono_wasm_download_heap() {
    const mb512 = 512 * 1024 * 1024;
    const heapData = localHeapViewU8();
    console.log(`Heap size: ${heapData.byteLength} bytes, ${heapData.byteLength / mb512} chunks`);
    let id = 0;
    for (let i = 0; i < heapData.length; i += mb512) {
        const chunkData = heapData.slice(i, i + mb512);
        console.log(`Chunk ${id}: ${chunkData.byteLength} bytes at ${i}`);
        downloadChunk([chunkData], `heap-chunk${id}.bin`);
        id++;
    }
}

function downloadChunk (chunks:Uint8Array[], fileName:string ) {
    const a = document.createElement("a");
    const blob = new Blob(chunks, { type: "application/octet-stream" });
    a.href = URL.createObjectURL(blob);
    a.download = fileName;
    // Append anchor to body.
    document.body.appendChild(a);
    a.click();

    // Remove anchor from body
    document.body.removeChild(a);
}

async function loadMemoryFromBuffers(promises:Promise<Uint8Array>[]) {
    const buffers = await Promise.all(promises);
    let offset = 0;
    for (const buffer of buffers) {
        const uint8Buffer = new Uint8Array(buffer);
        Module.HEAPU8.set(uint8Buffer, offset);
        offset += buffer.byteLength;
    }
    mono_wasm_perform_heapshot();
}

export function mono_wasm_load_heap(ev:any){
    ev.preventDefault();
    const items = [...ev.dataTransfer.items].map(item => item.getAsFile());
    items.sort((a, b) => a.name.localeCompare(b.name));
    const totalSize = items.reduce((acc, file) => acc + file.size, 0);
    const wasmMemory = (Module.asm?.memory || Module.wasmMemory)!;
    if(wasmMemory.buffer.byteLength<totalSize){
        wasmMemory.grow((totalSize - wasmMemory.buffer.byteLength + 65535) >>> 16);
        runtimeHelpers.updateMemoryViews();
    }

    loadMemoryFromBuffers(items.map(file => file.arrayBuffer()));
}
