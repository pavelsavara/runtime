// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

declare function autoAddDeps(obj: any, name: string): void;
declare function addToLibrary(obj: any): void;

// JSPI (WebAssembly JavaScript Promise Integration) API
// Standardized phase 4: Chrome 137+, Firefox 139+, Safari TP 238+
declare namespace WebAssembly {
    class Suspending {
        constructor(fn: (...args: any[]) => any);
    }
    function promising(fn: Function): (...args: any[]) => Promise<any>;
}
