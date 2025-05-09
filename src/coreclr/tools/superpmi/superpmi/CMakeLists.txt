project(superpmi)

remove_definitions(-DUNICODE)
remove_definitions(-D_UNICODE)

add_definitions(-DFEATURE_NO_HOST)
add_definitions(-DSELF_NO_HOST)
add_definitions(-DUSE_COREDISTOOLS)

if(CLR_CMAKE_HOST_WIN32)
  #use static crt
  set(CMAKE_MSVC_RUNTIME_LIBRARY MultiThreaded)
endif(CLR_CMAKE_HOST_WIN32)

if(CLR_CMAKE_HOST_UNIX)
    # Make sure a public symbol is created for Instrumentor_GetInsCount
    if(CLR_CMAKE_HOST_APPLE)
        add_linker_flag(-Wl,-export_dynamic)
    else()
        add_linker_flag(-Wl,--export-dynamic)
    endif(CLR_CMAKE_HOST_APPLE)
endif(CLR_CMAKE_HOST_UNIX)

include_directories(.)
include_directories(../superpmi-shared)

set(SUPERPMI_SOURCES
    commandline.cpp
    cycletimer.cpp
    icorjitinfo.cpp
    jitdebugger.cpp
    jitinstance.cpp
    methodstatsemitter.cpp
    neardiffer.cpp
    parallelsuperpmi.cpp
    streamingsuperpmi.cpp
    superpmi.cpp
    fileio.cpp
    jithost.cpp
    ../superpmi-shared/callutils.cpp
    ../superpmi-shared/compileresult.cpp
    ../superpmi-shared/errorhandling.cpp
    ../superpmi-shared/hash.cpp
    ../superpmi-shared/logging.cpp
    ../superpmi-shared/mclist.cpp
    ../superpmi-shared/methodcontext.cpp
    ../superpmi-shared/methodcontextreader.cpp
    ../superpmi-shared/simpletimer.cpp
    ../superpmi-shared/spmiutil.cpp
    ../superpmi-shared/tocfile.cpp
    ../superpmi-shared/typeutils.cpp
    ../superpmi-shared/spmidumphelper.cpp
)

add_executable_clr(superpmi
    ${SUPERPMI_SOURCES}
)

target_precompile_headers(superpmi PRIVATE "$<$<COMPILE_LANGUAGE:CXX>:standardpch.h>")

if(CLR_CMAKE_HOST_UNIX)
    target_link_libraries(superpmi
        PRIVATE
        utilcodestaticnohost
        mscorrc
        coreclrpal
        coreclrminipal
    )
else()
    target_link_libraries(superpmi
        PRIVATE
        version.lib
        advapi32.lib
        coreclrminipal
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_CPP_LIB}
    )
endif(CLR_CMAKE_HOST_UNIX)

install_clr(TARGETS superpmi DESTINATIONS . COMPONENT spmi)
