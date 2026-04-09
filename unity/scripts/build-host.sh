#!/usr/bin/env bash

set -euo pipefail

if [[ $# -lt 4 ]]; then
    echo "Usage: build-host.sh <platform> <arch> <build-dir> <output-dir>"
    exit 1
fi

PLATFORM="$1"
ARCH="$2"
BUILD_DIR="$3"
OUTPUT_DIR="$4"
SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
ROOT_DIR=$(cd "$SCRIPT_DIR/../.." && pwd)

mkdir -p "$OUTPUT_DIR"
rm -rf "$BUILD_DIR"

case "$PLATFORM" in
    linux)
        cmake -S "$ROOT_DIR/unity" \
            -B "$BUILD_DIR" \
            -DCMAKE_BUILD_TYPE=Release \
            -DCMAKE_SYSTEM_PROCESSOR="$ARCH"
        cmake --build "$BUILD_DIR" --config Release --target wcdb
        cp "$BUILD_DIR/libwcdb.so" "$OUTPUT_DIR/libwcdb.so"
        strip --strip-unneeded "$OUTPUT_DIR/libwcdb.so" || true
        ;;
    windows)
        cmake -S "$ROOT_DIR/unity" \
            -B "$BUILD_DIR" \
            -DCMAKE_BUILD_TYPE=Release \
            -A "$ARCH"
        cmake --build "$BUILD_DIR" --config Release --target wcdb
        cp "$BUILD_DIR/Release/wcdb.dll" "$OUTPUT_DIR/wcdb.dll"
        ;;
    *)
        echo "Unsupported host platform: $PLATFORM"
        exit 1
        ;;
esac
