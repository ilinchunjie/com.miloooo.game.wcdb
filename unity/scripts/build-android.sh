#!/usr/bin/env bash

set -euo pipefail

if [[ $# -lt 3 ]]; then
    echo "Usage: build-android.sh <abi> <ndk-path> <output-dir>"
    exit 1
fi

ABI="$1"
NDK_PATH="$2"
OUTPUT_DIR="$3"
SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
ROOT_DIR=$(cd "$SCRIPT_DIR/../.." && pwd)
BUILD_DIR="$ROOT_DIR/.build/android-$ABI"

mkdir -p "$OUTPUT_DIR"
rm -rf "$BUILD_DIR"

cmake -S "$ROOT_DIR/unity" \
    -B "$BUILD_DIR" \
    -DCMAKE_BUILD_TYPE=Release \
    -DANDROID_ABI="$ABI" \
    -DANDROID_PLATFORM=android-24 \
    -DANDROID_NDK="$NDK_PATH" \
    -DCMAKE_TOOLCHAIN_FILE="$NDK_PATH/build/cmake/android.toolchain.cmake" \
    -DUNITY_WCDB_BUILD_TESTS=OFF

cmake --build "$BUILD_DIR" --config Release --target wcdb
cp "$BUILD_DIR/libwcdb.so" "$OUTPUT_DIR/libwcdb.so"
