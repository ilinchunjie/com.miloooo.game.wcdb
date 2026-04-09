cmake_minimum_required(VERSION 3.20)
project(wcdb_unity LANGUAGES C CXX)

option(UNITY_WCDB_BUILD_TESTS "Build smoke tests for host platforms." ON)

set(WCDB_ROOT ${CMAKE_CURRENT_LIST_DIR}/../wcdb)
set(SKIP_WCONAN ON CACHE BOOL "Skip wconan integration for Unity builds." FORCE)
set(TARGET_NAME wcdb CACHE STRING "Unity-facing WCDB library name." FORCE)

if(APPLE AND CMAKE_SYSTEM_NAME STREQUAL "iOS")
    set(BUILD_SHARED_LIBS OFF CACHE BOOL "Build iOS as a static library for xcframework packaging." FORCE)
endif()

add_subdirectory(${WCDB_ROOT}/src ${CMAKE_CURRENT_BINARY_DIR}/wcdb-upstream)

if(APPLE AND CMAKE_SYSTEM_NAME STREQUAL "iOS")
    # Disable compiler-level -flto (from upstream) to produce Mach-O .o files
    # instead of LLVM bitcode, which libtool/xcframework cannot process.
    # Then re-enable LTO via Xcode's build setting so dead code elimination
    # still happens at link time.
    foreach(tgt wcdb sqlcipher)
        target_compile_options(${tgt} PRIVATE -fno-lto)
        set_target_properties(${tgt} PROPERTIES XCODE_ATTRIBUTE_LLVM_LTO YES)
    endforeach()
    if(TARGET zstd)
        target_compile_options(zstd PRIVATE -fno-lto)
        set_target_properties(zstd PROPERTIES XCODE_ATTRIBUTE_LLVM_LTO YES)
    endif()
endif()

target_sources(wcdb PRIVATE
    ${CMAKE_CURRENT_LIST_DIR}/native/src/wcdb_unity.cpp
)

target_include_directories(wcdb
    PUBLIC
        ${CMAKE_CURRENT_LIST_DIR}/native/include
)

if(MSVC AND BUILD_SHARED_LIBS)
    target_compile_definitions(wcdb PRIVATE WCDB_BUILD_SHARED)
endif()

set_target_properties(wcdb PROPERTIES
    OUTPUT_NAME wcdb
    FRAMEWORK FALSE
)

if(CMAKE_CONFIGURATION_TYPES)
    foreach(config IN ITEMS Debug Release RelWithDebInfo MinSizeRel)
        string(TOUPPER ${config} config_upper)
        set_target_properties(wcdb PROPERTIES
            ARCHIVE_OUTPUT_DIRECTORY_${config_upper} ${CMAKE_BINARY_DIR}/${config}
            LIBRARY_OUTPUT_DIRECTORY_${config_upper} ${CMAKE_BINARY_DIR}/${config}
            RUNTIME_OUTPUT_DIRECTORY_${config_upper} ${CMAKE_BINARY_DIR}/${config}
        )
    endforeach()
else()
    set_target_properties(wcdb PROPERTIES
        ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}
        LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}
        RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}
    )
endif()

if(UNITY_WCDB_BUILD_TESTS AND NOT ANDROID AND NOT (APPLE AND CMAKE_SYSTEM_NAME STREQUAL "iOS"))
    enable_testing()
    add_executable(wcdb_smoke
        ${CMAKE_CURRENT_LIST_DIR}/native/tests/smoke.cpp
    )
    target_link_libraries(wcdb_smoke PRIVATE wcdb)
    add_test(NAME wcdb_smoke COMMAND wcdb_smoke)
endif()
