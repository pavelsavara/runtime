# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(monohost)
set(DOTNET_PROJECT_NAME "MonoHost-Static")

# the host library

include(configure.cmake)

option(DISABLE_THREADS "defined if the build does NOT support multithreading" ON)
option(ENABLE_JS_INTEROP_BY_VALUE "defined if JS interop uses by-value semantics" OFF)

if(NOT CONFIGURATION_INTERPSIMDTABLES_LIB)
    set(CONFIGURATION_INTERPSIMDTABLES_LIB "nosimd")
endif()

set(MONOHOST_STATIC_SOURCES
    ./monohost.c
    ../../../mono/browser/runtime/runtime.c
    ../../../mono/browser/runtime/pinvoke.c
    ../../../mono/browser/runtime/driver.c
    ../../../mono/browser/runtime/corebindings.c
)

add_compile_definitions(FEATURE_APPHOST)
add_definitions(-DFEATURE_APPHOST=1)
add_definitions(-DFEATURE_STATIC_HOST=1)
add_definitions(-DGEN_PINVOKE=1)

add_library(MonoHost-Static
    STATIC
    ${MONOHOST_STATIC_SOURCES}
)
set_target_properties(MonoHost-Static PROPERTIES OUTPUT_NAME MonoHost CLEAN_DIRECT_OUTPUT 1)

# Mono runtime include dirs
set(MONO_ARTIFACTS_DIR
    ${CLR_ARTIFACTS_BIN_DIR}/mono/browser.wasm.${CMAKE_BUILD_RUNTIME_CONFIGURATION})

target_include_directories(MonoHost-Static PUBLIC
    ${MONO_ARTIFACTS_DIR}/include/mono-2.0
    ${CLR_ARTIFACTS_OBJ_DIR}/mono/browser.wasm.${CMAKE_BUILD_RUNTIME_CONFIGURATION}
    ${CMAKE_CURRENT_BINARY_DIR}/include/wasm
    ${CMAKE_CURRENT_SOURCE_DIR}/../../../mono/browser/runtime
    ${CMAKE_CURRENT_SOURCE_DIR}/../../corehost
)

install(TARGETS MonoHost-Static DESTINATION sharedFramework COMPONENT runtime)

# the executable
set(DOTNET_PROJECT_NAME "monohost")

set(MONOHOST_SOURCES
    empty.c
)

add_definitions(-DGEN_PINVOKE=1)

add_executable(monohost ${MONOHOST_SOURCES})
set_target_properties(monohost PROPERTIES OUTPUT_NAME dotnet.native)
set(CMAKE_EXECUTABLE_SUFFIX ".js")



set(SHARED_LIB_DESTINATION
    ${CLR_ARTIFACTS_BIN_DIR}/native/net${CMAKE_NET_CORE_APP_CURRENT_VERSION}-browser-${CMAKE_BUILD_LIBRARIES_CONFIGURATION}-wasm/sharedFramework)

# Mono runtime .a libraries
list(APPEND MONO_LIBS
    ${MONO_ARTIFACTS_DIR}/libmono-component-hot_reload-static.a
    ${MONO_ARTIFACTS_DIR}/libmono-component-debugger-static.a
    ${MONO_ARTIFACTS_DIR}/libmono-component-diagnostics_tracing-stub-static.a
    ${MONO_ARTIFACTS_DIR}/libmono-component-marshal-ilgen-static.a
    ${MONO_ARTIFACTS_DIR}/libmono-ee-interp.a
    ${MONO_ARTIFACTS_DIR}/libmonosgen-2.0.a
    ${MONO_ARTIFACTS_DIR}/libmono-icall-table.a
    ${MONO_ARTIFACTS_DIR}/libmono-wasm-eh-js.a
    ${MONO_ARTIFACTS_DIR}/libmono-wasm-${CONFIGURATION_INTERPSIMDTABLES_LIB}.a
)

# Shared platform .a libraries
list(APPEND NATIVE_LIBS
    ${SHARED_LIB_DESTINATION}/libSystem.Native.Browser.a
    ${SHARED_LIB_DESTINATION}/libSystem.Runtime.InteropServices.JavaScript.Native.a
    ${SHARED_LIB_DESTINATION}/libSystem.Native.a
    ${SHARED_LIB_DESTINATION}/libSystem.Globalization.Native.a
    ${CMAKE_ICU_DIR}/lib/libicuuc.a
    ${CMAKE_ICU_DIR}/lib/libicui18n.a
    ${CMAKE_ICU_DIR}/lib/libicudata.a
    ${SHARED_LIB_DESTINATION}/libSystem.IO.Compression.Native.a
    ${SHARED_LIB_DESTINATION}/libz.a
    # WASM-TODO respect  $(InvariantTimezone)
    # libSystem.Native.TimeZoneData.Invariant.a
    ${SHARED_LIB_DESTINATION}/libSystem.Native.TimeZoneData.a
)

# JS libraries (SHARED_LIB_DESTINATION is where rollup outputs go)
set(JS_SYSTEM_NATIVE_BROWSER
    "${SHARED_LIB_DESTINATION}/libSystem.Native.Browser.js")
set(JS_SYSTEM_BROWSER_UTILS
    "${SHARED_LIB_DESTINATION}/libSystem.Native.Browser.Utils.js")
set(JS_SYSTEM_RUNTIME_INTEROPSERVICES_JAVASCRIPT_NATIVE
    "${SHARED_LIB_DESTINATION}/libSystem.Runtime.InteropServices.JavaScript.Native.js")
set(JS_MONO_HOST
    "${SHARED_LIB_DESTINATION}/libMonoHost.js")
set(JS_SYSTEM_NATIVE_BROWSER_EXTPOST
    "${CMAKE_CURRENT_SOURCE_DIR}/../../libs/System.Native.Browser/libSystem.Native.Browser.extpost.js")

set_target_properties(monohost PROPERTIES
    LINK_DEPENDS "${JS_SYSTEM_NATIVE_BROWSER};${JS_SYSTEM_BROWSER_UTILS};${JS_SYSTEM_RUNTIME_INTEROPSERVICES_JAVASCRIPT_NATIVE};${JS_MONO_HOST};${JS_SYSTEM_NATIVE_BROWSER_EXTPOST};"
    LINK_FLAGS "--js-library ${JS_SYSTEM_NATIVE_BROWSER} --js-library ${JS_SYSTEM_BROWSER_UTILS} --js-library ${JS_SYSTEM_RUNTIME_INTEROPSERVICES_JAVASCRIPT_NATIVE} --js-library ${JS_MONO_HOST} --extern-post-js ${JS_SYSTEM_NATIVE_BROWSER_EXTPOST}"
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}")

if (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG)
    target_link_options(monohost PRIVATE
#        -sVERBOSE
#        -sASSERTIONS=1
        --emit-symbol-map
        -sLLD_REPORT_UNDEFINED
        -sERROR_ON_UNDEFINED_SYMBOLS=1
    )
endif ()

    # TODO !!!!!!!!!!!!!! -sWASM_BIGINT=1
target_link_options(monohost PRIVATE
    -sINITIAL_MEMORY=33554432
    -sMAXIMUM_MEMORY=2147483648
    -sALLOW_MEMORY_GROWTH=1
    -sSTACK_SIZE=5MB
    -sMODULARIZE=1
    -sEXPORT_ES6=1
    -sEXIT_RUNTIME=1
    -sALLOW_TABLE_GROWTH=1
    -sEXPORTED_RUNTIME_METHODS=MONO_HOST,addFunction,${CMAKE_EMCC_EXPORTED_RUNTIME_METHODS}
    -sEXPORTED_FUNCTIONS=${CMAKE_EMCC_EXPORTED_FUNCTIONS}
    -sEXPORT_NAME=createDotnetRuntime
    -sENVIRONMENT=web,webview,worker,node,shell
    -Wl,-error-limit=0)

target_link_libraries(monohost PRIVATE
    MonoHost-Static
    ${MONO_LIBS}
    ${NATIVE_LIBS}
)

# Generate wasm-config.h (browser variant)
configure_file(../../../mono/browser/runtime/wasm-config.h.in include/wasm/wasm-config.h)

install(TARGETS monohost DESTINATION corehost COMPONENT runtime)
install(TARGETS monohost DESTINATION sharedFramework COMPONENT runtime)
install(FILES ${CMAKE_CURRENT_BINARY_DIR}/dotnet.native.wasm DESTINATION corehost COMPONENT runtime)
install(FILES ${CMAKE_CURRENT_BINARY_DIR}/dotnet.native.wasm DESTINATION sharedFramework COMPONENT runtime)
install(FILES ${CMAKE_CURRENT_BINARY_DIR}/dotnet.native.js.symbols DESTINATION corehost COMPONENT runtime)
install(FILES ${CMAKE_CURRENT_BINARY_DIR}/dotnet.native.js.symbols DESTINATION sharedFramework COMPONENT runtime)

set_source_files_properties(${MONOHOST_SOURCES} PROPERTIES OBJECT_DEPENDS
    "${JS_SYSTEM_NATIVE_BROWSER};${JS_SYSTEM_BROWSER_UTILS};${JS_SYSTEM_RUNTIME_INTEROPSERVICES_JAVASCRIPT_NATIVE};${JS_MONO_HOST};${JS_SYSTEM_NATIVE_BROWSER_EXTPOST}")
