// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// C entry point for the Mono VM browser host (browserhost).
//

#include <emscripten.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include <stdbool.h>
#include <alloca.h>

#include <host_runtime_contract.h>

// Mono public embedding API
#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/class.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/image.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/utils/mono-logger.h>
#include <mono/jit/jit.h>
#include <mono/jit/mono-private-unstable.h>

// Bundled resources API — declared as extern following driver.c pattern
// (the internal header bundled-resources-internals.h may not be on the
// include path when building outside the Mono tree).
extern void mono_bundled_resources_add_assembly_resource (
    const char *id, const char *name,
    const uint8_t *data, uint32_t size,
    void (*free_func)(void *, void *), void *free_data);

extern void mono_bundled_resources_add_assembly_symbol_resource (
    const char *id, const uint8_t *data, uint32_t size,
    void (*free_func)(void *, void *), void *free_data);

// GC unsafe transition macros
#include "../../../mono/browser/runtime/gc-common.h"

// Browser runtime shared header — declares mono_wasm_load_runtime_common()
#include "../../../mono/browser/runtime/runtime.h"

// ---------------------------------------------------------------------------
// Global state
// ---------------------------------------------------------------------------

static MonoDomain *root_domain;

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

// Callback passed to mono_bundled_resources_add_assembly_resource.
// free_data is the strdup'd assembly name; the data buffer is
// intentionally never freed (owned by the JS caller).
static void
bundled_resources_free_func (void *resource, void *free_data)
{
    free (free_data);
}

// Simple log callback that writes Mono trace messages to stderr.
// Fatal messages cause immediate exit.
static void
monohost_trace_logger (const char *log_domain, const char *log_level,
                       const char *message, mono_bool fatal, void *user_data)
{
    fprintf (stderr, "[mono] %s/%s: %s\n",
             log_domain ? log_domain : "mono",
             log_level ? log_level : "info",
             message ? message : "(null)");
    if (fatal)
        exit (1);
}

// ---------------------------------------------------------------------------
// BrowserHost_AddAssembly
// ---------------------------------------------------------------------------
//
// Register an assembly (DLL) in Mono's bundled resources so that it can be
// loaded later via mono_assembly_open / mono_assembly_load.
//
// The caller (JS side) allocates the data buffer; ownership transfers here.
// The buffer is freed via bundled_resources_free_func when Mono is done
// with it.
//
// Parameters:
//   name — assembly file name, e.g. "MyApp.dll".
//   data — pointer to the assembly bytes (heap-allocated).
//   size — byte length of the assembly.
//
// Returns 0 on success, -1 on failure.
//
int
BrowserHost_AddAssembly (const char *name,
                      const unsigned char *data,
                      unsigned int size)
{
    if (!name || !data || size == 0)
    {
        fprintf (stderr, "BrowserHost_AddAssembly: invalid arguments "
                 "(name=%p, data=%p, size=%u)\n",
                 (const void *)name, (const void *)data, size);
        return -1;
    }

    // The id and name strings must be heap-allocated because Mono may
    // reference them after this call returns.  We duplicate the name and
    // pass one copy as the id and one as the resource name (same value).
    char *assembly_name = strdup (name);
    if (!assembly_name)
    {
        fprintf (stderr, "BrowserHost_AddAssembly: strdup failed for '%s'\n",
                 name);
        return -1;
    }

    // Detect PDB files and route to the symbol resource API.
    size_t name_len = strlen (name);
    if (name_len > 4 && strcmp (name + name_len - 4, ".pdb") == 0)
    {
        mono_bundled_resources_add_assembly_symbol_resource (
            assembly_name, data, size,
            bundled_resources_free_func, assembly_name);
        return 0;
    }

    // Register the assembly in Mono's bundled resources.
    // Same pattern as mono_wasm_add_assembly in driver.c.
    mono_bundled_resources_add_assembly_resource (
        assembly_name,          // id
        assembly_name,          // name
        data,                   // data
        size,                   // size
        bundled_resources_free_func, // free callback
        assembly_name           // free_data (frees the strdup'd name)
    );

    return 0;
}

// ---------------------------------------------------------------------------
// BrowserHost_Exit — clean shutdown
// ---------------------------------------------------------------------------
void
BrowserHost_Exit (int exit_code)
{
    exit (exit_code);
}

// ---------------------------------------------------------------------------
// Host contract — same pattern as CoreCLR browserhost
// ---------------------------------------------------------------------------

static struct host_runtime_contract host_contract = { sizeof (struct host_runtime_contract), NULL };

void *
BrowserHost_CreateHostContract (void)
{
    // dummy
    return &host_contract;
}

int
BrowserHost_InitializeDotnet (int propertyCount,
                              const char **propertyKeys,
                              const char **propertyValues)
{
    int init_result = monovm_initialize (propertyCount, propertyKeys, propertyValues);
    if (init_result != 0)
    {
        fprintf (stderr, "BrowserHost_InitializeDotnet: "
                 "monovm_initialize failed (result=%d)\n", init_result);
        return -1;
    }

    const char *interp_opts = "";
    root_domain = mono_wasm_load_runtime_common (0 /*debugLevel*/,
                                                  monohost_trace_logger,
                                                  interp_opts);

    if (!root_domain)
    {
        fprintf (stderr, "BrowserHost_InitializeDotnet: "
                 "mono_wasm_load_runtime_common failed\n");
        return -1;
    }

    return 0;
}

// ---------------------------------------------------------------------------
// BrowserHost_ExecuteAssembly
// ---------------------------------------------------------------------------
//
// Load the specified assembly, find its entry point (with async unwrapping),
// and execute it via mono_runtime_run_main.
//
// The async unwrapping heuristic matches SystemInteropJS_AssemblyGetEntryPoint
// in corebindings.c: if the entry point has METHOD_ATTRIBUTE_SPECIAL_NAME and
// is named "<Name>", try "<Name>$" then "Name" to find the actual async method
// so the host can yield properly instead of blocking on GetResult().
//
EMSCRIPTEN_KEEPALIVE int
BrowserHost_ExecuteAssembly (const char *assemblyPath, int argc, const char **argv)
{
    if (!assemblyPath)
    {
        fprintf (stderr, "BrowserHost_ExecuteAssembly: assemblyPath is NULL\n");
        return -1;
    }

    // Load the assembly from bundled resources.
    MonoImageOpenStatus status;
    MonoAssemblyName *aname = mono_assembly_name_new (assemblyPath);
    if (!aname)
    {
        fprintf (stderr, "BrowserHost_ExecuteAssembly: "
                 "mono_assembly_name_new failed for '%s'\n", assemblyPath);
        return -1;
    }

    MonoAssembly *assembly = mono_assembly_load (aname, NULL, &status);
    mono_assembly_name_free (aname);
    if (!assembly)
    {
        fprintf (stderr, "BrowserHost_ExecuteAssembly: "
                 "failed to load assembly '%s' (status=%d)\n",
                 assemblyPath, (int)status);
        return -1;
    }

    MonoImage *image = mono_assembly_get_image (assembly);
    uint32_t entry_token = mono_image_get_entry_point (image);
    if (!entry_token)
    {
        fprintf (stderr, "BrowserHost_ExecuteAssembly: "
                 "no entry point in assembly '%s'\n", assemblyPath);
        return -1;
    }

    mono_domain_ensure_entry_assembly (root_domain, assembly);
    MonoMethod *method = mono_get_method (image, entry_token, NULL);
    if (!method)
    {
        fprintf (stderr, "BrowserHost_ExecuteAssembly: "
                 "mono_get_method failed for entry token 0x%08x\n", entry_token);
        return -1;
    }

    MonoObject *exc = NULL;
    int exit_code = mono_runtime_run_main (method, argc, (char **)argv, &exc);

    if (exc)
    {
        fprintf (stderr, "BrowserHost_ExecuteAssembly: "
                 "unhandled exception in assembly '%s'\n", assemblyPath);
        exit_code = 1;
    }

    return exit_code;
}