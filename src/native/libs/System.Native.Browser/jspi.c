// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <emscripten.h>

#ifndef EXTERN_C
#define EXTERN_C extern
#endif//EXTERN_C

EXTERN_C void SystemJS_ExecuteTimerCallback ();
EXTERN_C void SystemJS_ExecuteBackgroundJobCallback ();
EXTERN_C void SystemJS_ExecuteFinalizationCallback ();
EXTERN_C int BrowserHost_InitializeDotnet(int propertiesCount, const char** propertyKeys, const char** propertyValues);
EXTERN_C int BrowserHost_ExecuteAssembly(const char* assemblyPath, int argc, const char** argv);
EXTERN_C void SystemInteropJS_CallJSExport(int arg0, void * arg1);
EXTERN_C void SystemInteropJS_CompleteTask(void * arg0);
EXTERN_C void SystemInteropJS_BindAssemblyExports(void * arg0);

EXTERN_C const int SystemJS_AsyncExports(void **exports);

EXTERN_C const int SystemJS_AsyncExports(void **exports)
{
    exports[0] = (void*)&SystemJS_ExecuteTimerCallback;
    exports[1] = (void*)&SystemJS_ExecuteBackgroundJobCallback;
    exports[2] = (void*)&SystemJS_ExecuteFinalizationCallback;
    exports[3] = (void*)&BrowserHost_InitializeDotnet;
    exports[4] = (void*)&BrowserHost_ExecuteAssembly;
    exports[5] = (void*)&SystemInteropJS_CallJSExport;
    exports[6] = (void*)&SystemInteropJS_CompleteTask;
    exports[7] = (void*)&SystemInteropJS_BindAssemblyExports;
    return 8;
}
