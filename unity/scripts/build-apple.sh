#!/usr/bin/env bash

set -euo pipefail

if [[ $# -lt 2 ]]; then
    echo "Usage: build-apple.sh <platform> <output-dir>"
    echo "platform: macos | ios"
    exit 1
fi

PLATFORM="$1"
OUTPUT_DIR="$2"
SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
ROOT_DIR=$(cd "$SCRIPT_DIR/../.." && pwd)
BUILD_ROOT="$ROOT_DIR/.build/unity-$PLATFORM"

mkdir -p "$OUTPUT_DIR"
rm -rf "$BUILD_ROOT"

case "$PLATFORM" in
    macos)
        cmake -S "$ROOT_DIR/unity" \
            -B "$BUILD_ROOT" \
            -G Xcode \
            -DCMAKE_BUILD_TYPE=Release \
            -DCMAKE_OSX_ARCHITECTURES="arm64;x86_64"
        cmake --build "$BUILD_ROOT" --config Release --target wcdb
        cp "$BUILD_ROOT/Release/libwcdb.dylib" "$OUTPUT_DIR/libwcdb.dylib"
        strip -x "$OUTPUT_DIR/libwcdb.dylib" || true
        archs=$(lipo -archs "$OUTPUT_DIR/libwcdb.dylib")
        if [[ " $archs " != *" arm64 "* || " $archs " != *" x86_64 "* ]]; then
            echo "Expected macOS libwcdb.dylib to contain arm64 and x86_64 slices, got: $archs" >&2
            exit 1
        fi
        codesign --force --sign - --timestamp=none "$OUTPUT_DIR/libwcdb.dylib"
        codesign --verify --verbose=4 "$OUTPUT_DIR/libwcdb.dylib"
        ;;
    ios)
        cmake -S "$ROOT_DIR/unity" \
            -B "$BUILD_ROOT" \
            -G Xcode \
            -DCMAKE_SYSTEM_NAME=iOS \
            -DCMAKE_OSX_ARCHITECTURES="arm64" \
            -DCMAKE_OSX_DEPLOYMENT_TARGET=13.0 \
            -DUNITY_WCDB_BUILD_TESTS=OFF
        cmake --build "$BUILD_ROOT" --config Release --target wcdb
        MERGED_LIB_PATH="$BUILD_ROOT/libwcdb_merged.a"
        LIBTOOL_INPUTS=()
        for lib in libwcdb.a libsqlcipher.a libzstd.a; do
            found=$(find "$BUILD_ROOT" -name "$lib" -not -path "*/Objects-normal/*" | head -1)
            if [[ -n "$found" ]]; then
                LIBTOOL_INPUTS+=("$found")
            fi
        done
        libtool -static -o "$MERGED_LIB_PATH" "${LIBTOOL_INPUTS[@]}"
        strip -x "$MERGED_LIB_PATH"
        mv "$MERGED_LIB_PATH" "$BUILD_ROOT/libwcdb.a"
        MERGED_LIB_PATH="$BUILD_ROOT/libwcdb.a"
        rm -rf "$OUTPUT_DIR/wcdb.xcframework"
        xcodebuild -create-xcframework \
            -library "$MERGED_LIB_PATH" \
            -output "$OUTPUT_DIR/wcdb.xcframework"
        ;;
    *)
        echo "Unsupported Apple platform: $PLATFORM"
        exit 1
        ;;
esac
