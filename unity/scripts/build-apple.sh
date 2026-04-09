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
        ;;
    ios)
        cmake -S "$ROOT_DIR/unity" \
            -B "$BUILD_ROOT" \
            -G Xcode \
            -DCMAKE_SYSTEM_NAME=iOS \
            -DCMAKE_OSX_ARCHITECTURES="arm64" \
            -DCMAKE_OSX_DEPLOYMENT_TARGET=13.0 \
            -DCMAKE_BUILD_TYPE=Release \
            -DUNITY_WCDB_BUILD_TESTS=OFF
        cmake --build "$BUILD_ROOT" --config Release --target wcdb
        LIB_PATH="$BUILD_ROOT/Release-iphoneos/libwcdb.a"
        xcodebuild -create-xcframework \
            -library "$LIB_PATH" \
            -output "$OUTPUT_DIR/wcdb.xcframework"
        ;;
    *)
        echo "Unsupported Apple platform: $PLATFORM"
        exit 1
        ;;
esac
