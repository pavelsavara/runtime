# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

cmake_minimum_required(VERSION 3.26)
project(monohost)

include(../../../eng/native/configurepaths.cmake)
include(${CLR_ENG_NATIVE_DIR}/configurecompiler.cmake)

set(MONOHOST_ROOT_DIR "${CMAKE_CURRENT_SOURCE_DIR}")

set(CMAKE_INCLUDE_CURRENT_DIR ON)
include_directories(${CMAKE_CURRENT_BINARY_DIR})
include_directories(${CMAKE_CURRENT_SOURCE_DIR})
include_directories(${CLR_ARTIFACTS_OBJ_DIR}) # Generated version files

if(CLR_CMAKE_TARGET_BROWSER)
    add_subdirectory(browserhost)
endif()
