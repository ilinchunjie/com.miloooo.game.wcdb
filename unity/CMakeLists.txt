cmake_minimum_required(VERSION 3.20)
project(wcdb_unity LANGUAGES C CXX)

include(CheckIPOSupported)

option(UNITY_WCDB_ENABLE_CIPHER "Enable SQLCipher codec support." OFF)
option(UNITY_WCDB_ENABLE_JSON1 "Enable SQLite JSON1 extension." OFF)
option(UNITY_WCDB_ENABLE_FTS "Enable SQLite FTS5 extension." OFF)
option(UNITY_WCDB_ENABLE_RTREE "Enable SQLite RTREE extension." OFF)
option(UNITY_WCDB_ENABLE_SESSION "Enable SQLite session extension." OFF)
option(UNITY_WCDB_ENABLE_PREUPDATE_HOOK "Enable SQLite preupdate hook support." OFF)
option(UNITY_WCDB_ENABLE_ZSTD "Enable ZSTD support." OFF)
option(UNITY_WCDB_ENABLE_REPAIR "Enable repair/backup support." OFF)
option(UNITY_WCDB_ENABLE_WCDB_NATIVE_MIGRATION "Enable WCDB native migration support." OFF)
option(UNITY_WCDB_ENABLE_TRACE "Enable native tracing exports." OFF)
option(UNITY_WCDB_BUILD_TESTS "Build smoke tests for host platforms." ON)

if(UNITY_WCDB_ENABLE_ZSTD)
    message(FATAL_ERROR "UNITY_WCDB_ENABLE_ZSTD is not supported by the minimal Unity build.")
endif()

if(UNITY_WCDB_ENABLE_REPAIR)
    message(FATAL_ERROR "UNITY_WCDB_ENABLE_REPAIR is not supported by the minimal Unity build.")
endif()

if(UNITY_WCDB_ENABLE_WCDB_NATIVE_MIGRATION)
    message(FATAL_ERROR "UNITY_WCDB_ENABLE_WCDB_NATIVE_MIGRATION is not supported. Use C# user_version migrations instead.")
endif()

if(UNITY_WCDB_ENABLE_TRACE)
    message(FATAL_ERROR "UNITY_WCDB_ENABLE_TRACE is not supported by the minimal Unity build.")
endif()

set(WCDB_ROOT ${CMAKE_CURRENT_LIST_DIR}/../wcdb)
set(SQLCIPHER_ROOT ${WCDB_ROOT}/sqlcipher)
set(SQLCIPHER_SRC_ROOT ${SQLCIPHER_ROOT}/src)
set(SQLCIPHER_EXT_ROOT ${SQLCIPHER_ROOT}/ext)
set(SQLCIPHER_GENERATED_INCLUDE_DIR ${CMAKE_CURRENT_BINARY_DIR}/generated/sqlcipher)

if(UNITY_WCDB_ENABLE_SESSION AND NOT UNITY_WCDB_ENABLE_PREUPDATE_HOOK)
    set(UNITY_WCDB_ENABLE_PREUPDATE_HOOK ON)
endif()

file(MAKE_DIRECTORY ${SQLCIPHER_GENERATED_INCLUDE_DIR})
foreach(header_name IN ITEMS config.h keywordhash.h opcodes.h parse.h sqlite3.h)
    configure_file(
        ${SQLCIPHER_ROOT}/${header_name}
        ${SQLCIPHER_GENERATED_INCLUDE_DIR}/${header_name}
        COPYONLY
    )
endforeach()

set(WCDB_SQLITE_CORE_SOURCES
    ${SQLCIPHER_SRC_ROOT}/alter.c
    ${SQLCIPHER_SRC_ROOT}/analyze.c
    ${SQLCIPHER_SRC_ROOT}/attach.c
    ${SQLCIPHER_SRC_ROOT}/auth.c
    ${SQLCIPHER_SRC_ROOT}/backup.c
    ${SQLCIPHER_SRC_ROOT}/bitvec.c
    ${SQLCIPHER_SRC_ROOT}/btmutex.c
    ${SQLCIPHER_SRC_ROOT}/btree.c
    ${SQLCIPHER_SRC_ROOT}/build.c
    ${SQLCIPHER_SRC_ROOT}/callback.c
    ${SQLCIPHER_SRC_ROOT}/complete.c
    ${SQLCIPHER_SRC_ROOT}/ctime.c
    ${SQLCIPHER_SRC_ROOT}/date.c
    ${SQLCIPHER_SRC_ROOT}/dbpage.c
    ${SQLCIPHER_SRC_ROOT}/dbstat.c
    ${SQLCIPHER_SRC_ROOT}/delete.c
    ${SQLCIPHER_SRC_ROOT}/expr.c
    ${SQLCIPHER_SRC_ROOT}/fault.c
    ${SQLCIPHER_SRC_ROOT}/fkey.c
    ${SQLCIPHER_SRC_ROOT}/func.c
    ${SQLCIPHER_SRC_ROOT}/global.c
    ${SQLCIPHER_SRC_ROOT}/hash.c
    ${SQLCIPHER_SRC_ROOT}/insert.c
    ${SQLCIPHER_SRC_ROOT}/legacy.c
    ${SQLCIPHER_SRC_ROOT}/loadext.c
    ${SQLCIPHER_SRC_ROOT}/main.c
    ${SQLCIPHER_SRC_ROOT}/malloc.c
    ${SQLCIPHER_SRC_ROOT}/mem0.c
    ${SQLCIPHER_SRC_ROOT}/mem1.c
    ${SQLCIPHER_SRC_ROOT}/mem2.c
    ${SQLCIPHER_SRC_ROOT}/mem3.c
    ${SQLCIPHER_SRC_ROOT}/mem5.c
    ${SQLCIPHER_SRC_ROOT}/memdb.c
    ${SQLCIPHER_SRC_ROOT}/memjournal.c
    ${SQLCIPHER_SRC_ROOT}/mutex.c
    ${SQLCIPHER_SRC_ROOT}/notify.c
    ${SQLCIPHER_SRC_ROOT}/os.c
    ${SQLCIPHER_SRC_ROOT}/pager.c
    ${SQLCIPHER_SRC_ROOT}/pcache.c
    ${SQLCIPHER_SRC_ROOT}/pcache1.c
    ${SQLCIPHER_SRC_ROOT}/pragma.c
    ${SQLCIPHER_SRC_ROOT}/prepare.c
    ${SQLCIPHER_SRC_ROOT}/printf.c
    ${SQLCIPHER_SRC_ROOT}/random.c
    ${SQLCIPHER_SRC_ROOT}/resolve.c
    ${SQLCIPHER_SRC_ROOT}/rowset.c
    ${SQLCIPHER_SRC_ROOT}/select.c
    ${SQLCIPHER_SRC_ROOT}/status.c
    ${SQLCIPHER_SRC_ROOT}/table.c
    ${SQLCIPHER_SRC_ROOT}/threads.c
    ${SQLCIPHER_SRC_ROOT}/tokenize.c
    ${SQLCIPHER_SRC_ROOT}/treeview.c
    ${SQLCIPHER_SRC_ROOT}/trigger.c
    ${SQLCIPHER_SRC_ROOT}/update.c
    ${SQLCIPHER_SRC_ROOT}/upsert.c
    ${SQLCIPHER_SRC_ROOT}/utf.c
    ${SQLCIPHER_SRC_ROOT}/util.c
    ${SQLCIPHER_SRC_ROOT}/vacuum.c
    ${SQLCIPHER_SRC_ROOT}/vdbe.c
    ${SQLCIPHER_SRC_ROOT}/vdbeapi.c
    ${SQLCIPHER_SRC_ROOT}/vdbeaux.c
    ${SQLCIPHER_SRC_ROOT}/vdbeblob.c
    ${SQLCIPHER_SRC_ROOT}/vdbemem.c
    ${SQLCIPHER_SRC_ROOT}/vdbesort.c
    ${SQLCIPHER_SRC_ROOT}/vdbetrace.c
    ${SQLCIPHER_SRC_ROOT}/vtab.c
    ${SQLCIPHER_SRC_ROOT}/wal.c
    ${SQLCIPHER_SRC_ROOT}/walker.c
    ${SQLCIPHER_SRC_ROOT}/where.c
    ${SQLCIPHER_SRC_ROOT}/wherecode.c
    ${SQLCIPHER_SRC_ROOT}/whereexpr.c
    ${SQLCIPHER_SRC_ROOT}/window.c
    ${SQLCIPHER_ROOT}/parse.c
    ${SQLCIPHER_ROOT}/opcodes.c
    ${CMAKE_CURRENT_LIST_DIR}/native/src/wcdb_unity.cpp
)

set(WCDB_SQLITE_OPTIONAL_SOURCES)
set(WCDB_SQLITE_OPTIONAL_INCLUDE_DIRS)
set(WCDB_SQLITE_DEFINITIONS
    SQLITE_THREADSAFE=2
    SQLITE_DEFAULT_MEMSTATUS=0
    SQLITE_DEFAULT_WAL_SYNCHRONOUS=1
    SQLITE_DEFAULT_WAL_AUTOCHECKPOINT=0
    SQLITE_DEFAULT_SYNCHRONOUS=1
    SQLITE_DEFAULT_LOCKING_MODE=0
    SQLITE_DEFAULT_CACHE_SIZE=-2000
    SQLITE_DEFAULT_PAGE_SIZE=4096
    SQLITE_DEFAULT_JOURNAL_SIZE_LIMIT=4194304
    SQLITE_TEMP_STORE=2
    SQLITE_DEFAULT_WORKER_THREADS=2
    SQLITE_OMIT_LOAD_EXTENSION
    SQLITE_OMIT_SHARED_CACHE
    SQLITE_OMIT_DEPRECATED
    SQLITE_OMIT_COMPILEOPTION_DIAGS
    SQLITE_OMIT_BUILTIN_TEST
    SQLITE_USE_URI=0
    SQLITE_LIKE_DOESNT_MATCH_BLOBS
    SQLITE_DQS=0
    SQLITE_MAX_ATTACHED=16
    SQLITE_MAX_EXPR_DEPTH=0
    SQLITE_MAX_SCHEMA_RETRY=64
    SQLITE_MALLOC_SOFT_LIMIT=0
    SQLITE_PRINT_BUF_SIZE=256
    SQLITE_HAS_CODEC
    SQLITE_WCDB=1
    SQLITE_WCDB_CHECKPOINT_HANDLER=1
    SQLITE_WCDB_IMPROVED_CHECKPOINT=1
    SQLITE_WCDB_LOCK_HOOK=1
    SQLITE_WCDB_SUSPEND=1
)

set(WCDB_SQLITE_PRIVATE_DEFINITIONS
    SQLITE_CORE
    SQLITE_UNTESTABLE
    SQLITE_USE_ALLOCA=1
    SQLITE_ENABLE_API_ARMOR=1
)

if(NOT WIN32 AND CMAKE_SIZEOF_VOID_P EQUAL 8)
    list(APPEND WCDB_SQLITE_DEFINITIONS SQLITE_INT64_TYPE=long)
endif()

if(UNITY_WCDB_ENABLE_CIPHER)
    list(APPEND WCDB_SQLITE_CORE_SOURCES
        ${SQLCIPHER_SRC_ROOT}/crypto.c
        ${SQLCIPHER_SRC_ROOT}/crypto_impl.c
    )
    list(APPEND WCDB_SQLITE_PRIVATE_DEFINITIONS SQLCIPHER_PREPROCESSED=1)
else()
    list(APPEND WCDB_SQLITE_CORE_SOURCES
        ${CMAKE_CURRENT_LIST_DIR}/native/src/sqlcipher_codec_stub.c
    )
    list(APPEND WCDB_SQLITE_PRIVATE_DEFINITIONS OMIT_MEMLOCK OMIT_MEM_SECURITY)
endif()

if(UNITY_WCDB_ENABLE_JSON1)
    list(APPEND WCDB_SQLITE_OPTIONAL_SOURCES
        ${SQLCIPHER_EXT_ROOT}/misc/json1.c
    )
    list(APPEND WCDB_SQLITE_DEFINITIONS SQLITE_ENABLE_JSON1)
endif()

if(UNITY_WCDB_ENABLE_FTS)
    list(APPEND WCDB_SQLITE_OPTIONAL_SOURCES
        ${SQLCIPHER_ROOT}/fts5.c
    )
    list(APPEND WCDB_SQLITE_DEFINITIONS SQLITE_ENABLE_FTS5)
endif()

if(UNITY_WCDB_ENABLE_RTREE)
    list(APPEND WCDB_SQLITE_OPTIONAL_SOURCES
        ${SQLCIPHER_EXT_ROOT}/rtree/rtree.c
    )
    list(APPEND WCDB_SQLITE_DEFINITIONS SQLITE_ENABLE_RTREE)
    list(APPEND WCDB_SQLITE_OPTIONAL_INCLUDE_DIRS
        ${SQLCIPHER_EXT_ROOT}/rtree
    )
endif()

if(UNITY_WCDB_ENABLE_SESSION)
    list(APPEND WCDB_SQLITE_OPTIONAL_SOURCES
        ${SQLCIPHER_EXT_ROOT}/session/sqlite3session.c
    )
    list(APPEND WCDB_SQLITE_DEFINITIONS SQLITE_ENABLE_SESSION)
    list(APPEND WCDB_SQLITE_OPTIONAL_INCLUDE_DIRS
        ${SQLCIPHER_EXT_ROOT}/session
    )
endif()

if(UNITY_WCDB_ENABLE_PREUPDATE_HOOK)
    list(APPEND WCDB_SQLITE_DEFINITIONS SQLITE_ENABLE_PREUPDATE_HOOK)
endif()

if(WIN32)
    list(APPEND WCDB_SQLITE_CORE_SOURCES
        ${SQLCIPHER_SRC_ROOT}/mutex_noop.c
        ${SQLCIPHER_SRC_ROOT}/mutex_w32.c
        ${SQLCIPHER_SRC_ROOT}/os_win.c
    )
    list(APPEND WCDB_SQLITE_PRIVATE_DEFINITIONS
        SQLITE_OS_WIN=1
        SQLITE_4_BYTE_ALIGNED_MALLOC=1
        SQLITE_SYSTEM_MALLOC=1
        INCLUDE_MSVC_H=1
    )
else()
    list(APPEND WCDB_SQLITE_CORE_SOURCES
        ${SQLCIPHER_SRC_ROOT}/mutex_noop.c
        ${SQLCIPHER_SRC_ROOT}/mutex_unix.c
        ${SQLCIPHER_SRC_ROOT}/os_unix.c
    )
    list(APPEND WCDB_SQLITE_PRIVATE_DEFINITIONS SQLITE_OS_UNIX=1)
endif()

if(ANDROID)
    list(APPEND WCDB_SQLITE_PRIVATE_DEFINITIONS
        __ANDROID__
        HAVE_USLEEP=1
        HAVE_FDATASYNC=1
        HAVE_PREAD=1
        HAVE_PWRITE=1
        USE_PREAD64=1
    )
elseif(APPLE)
    list(APPEND WCDB_SQLITE_PRIVATE_DEFINITIONS
        _HAVE_SQLITE_CONFIG_H
        HAVE_USLEEP=1
        HAVE_PREAD=1
        HAVE_PWRITE=1
        HAVE_READLINK=1
        HAVE_LSTAT=1
        USE_PREAD=1
        SQLITE_ENABLE_BATCH_ATOMIC_WRITE=1
        SQLITE_SYSTEM_MALLOC=1
    )
else()
    list(APPEND WCDB_SQLITE_PRIVATE_DEFINITIONS
        _HAVE_SQLITE_CONFIG_H
        HAVE_USLEEP=1
        HAVE_PREAD=1
        HAVE_PWRITE=1
        HAVE_FCHOWN=1
        HAVE_READLINK=1
        HAVE_LSTAT=1
    )
endif()

if(UNITY_WCDB_ENABLE_CIPHER)
    if(APPLE)
        list(APPEND WCDB_SQLITE_CORE_SOURCES
            ${SQLCIPHER_SRC_ROOT}/crypto_cc.c
        )
        list(APPEND WCDB_SQLITE_PRIVATE_DEFINITIONS SQLCIPHER_CRYPTO_CC=1)
    else()
        list(APPEND WCDB_SQLITE_CORE_SOURCES
            ${SQLCIPHER_SRC_ROOT}/crypto_openssl.c
        )
        list(APPEND WCDB_SQLITE_PRIVATE_DEFINITIONS SQLCIPHER_CRYPTO_OPENSSL=1)
    endif()
endif()

if(APPLE AND CMAKE_SYSTEM_NAME STREQUAL "iOS")
    set(WCDB_LIBRARY_TYPE STATIC)
else()
    set(WCDB_LIBRARY_TYPE SHARED)
endif()

add_library(wcdb ${WCDB_LIBRARY_TYPE}
    ${WCDB_SQLITE_CORE_SOURCES}
    ${WCDB_SQLITE_OPTIONAL_SOURCES}
)

target_include_directories(wcdb
    PUBLIC
        ${CMAKE_CURRENT_LIST_DIR}/native/include
    PRIVATE
        ${SQLCIPHER_GENERATED_INCLUDE_DIR}
        ${SQLCIPHER_SRC_ROOT}
        ${WCDB_ROOT}/tools/prebuild/openssl/include
        ${WCDB_SQLITE_OPTIONAL_INCLUDE_DIRS}
)

target_compile_features(wcdb PRIVATE cxx_std_17)

if(MSVC)
    target_compile_definitions(wcdb PRIVATE WCDB_BUILD_SHARED)
endif()

target_compile_definitions(wcdb PRIVATE
    ${WCDB_SQLITE_DEFINITIONS}
    ${WCDB_SQLITE_PRIVATE_DEFINITIONS}
)

set_target_properties(wcdb PROPERTIES
    OUTPUT_NAME wcdb
    C_VISIBILITY_PRESET hidden
    CXX_VISIBILITY_PRESET hidden
    VISIBILITY_INLINES_HIDDEN YES
    POSITION_INDEPENDENT_CODE ON
)

if(MSVC)
    target_compile_options(wcdb PRIVATE /W4 /utf-8 /EHsc /wd4100 /wd4244 /wd4706)
    target_compile_options(wcdb PRIVATE $<$<CONFIG:Release>:/O1 /GL>)
    target_link_options(wcdb PRIVATE $<$<CONFIG:Release>:/LTCG>)
else()
    target_compile_options(wcdb PRIVATE
        -Wall
        -Wextra
        -Wpedantic
        -Wno-unused-parameter
        -Wno-unused-function
        -Wno-unused-variable
        -Wno-sign-compare
        -Wno-unused-but-set-variable
    )
    target_compile_options(wcdb PRIVATE $<$<CONFIG:Release>:-ffunction-sections -fdata-sections>)
    if(CMAKE_C_COMPILER_ID MATCHES "Clang")
        target_compile_options(wcdb PRIVATE $<$<CONFIG:Release>:-Oz>)
    else()
        target_compile_options(wcdb PRIVATE $<$<CONFIG:Release>:-Os>)
    endif()
    if(APPLE)
        target_link_options(wcdb PRIVATE $<$<CONFIG:Release>:-Wl,-dead_strip>)
    else()
        target_link_options(wcdb PRIVATE $<$<CONFIG:Release>:-Wl,--gc-sections>)
    endif()
endif()

check_ipo_supported(RESULT WCDB_LTO_SUPPORTED)
if(WCDB_LTO_SUPPORTED)
    set_property(TARGET wcdb PROPERTY INTERPROCEDURAL_OPTIMIZATION_RELEASE TRUE)
endif()

if(UNITY_WCDB_ENABLE_CIPHER)
    if(APPLE)
        find_library(SECURITY_FRAMEWORK Security REQUIRED)
        find_library(COREFOUNDATION_FRAMEWORK CoreFoundation REQUIRED)
        target_link_libraries(wcdb PRIVATE ${SECURITY_FRAMEWORK} ${COREFOUNDATION_FRAMEWORK})
    else()
        find_package(OpenSSL REQUIRED)
        target_link_libraries(wcdb PRIVATE OpenSSL::Crypto)
    endif()
elseif(APPLE)
    find_library(COREFOUNDATION_FRAMEWORK CoreFoundation REQUIRED)
    target_link_libraries(wcdb PRIVATE ${COREFOUNDATION_FRAMEWORK})
endif()

if(UNIX AND NOT APPLE AND NOT ANDROID)
    target_link_libraries(wcdb PRIVATE dl pthread)
endif()

if(ANDROID)
    find_library(LOG_LIB log)
    target_link_libraries(wcdb PRIVATE ${LOG_LIB})
endif()

if(UNITY_WCDB_BUILD_TESTS AND NOT ANDROID AND NOT (APPLE AND CMAKE_SYSTEM_NAME STREQUAL "iOS"))
    enable_testing()
    add_executable(wcdb_smoke
        ${CMAKE_CURRENT_LIST_DIR}/native/tests/smoke.cpp
    )
    target_link_libraries(wcdb_smoke PRIVATE wcdb)
    add_test(NAME wcdb_smoke COMMAND wcdb_smoke)
endif()
