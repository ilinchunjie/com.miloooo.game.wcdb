#!/usr/bin/env bash

set -euo pipefail

usage() {
    cat <<'EOF'
Usage: assemble-package.sh --version <VERSION> --output <DIR> [--artifacts <DIR>]

Arguments:
  --version <VERSION>   Package version to write into package.json.
  --output <DIR>        Output directory for the assembled UPM package.
  --artifacts <DIR>     Directory containing per-platform native binaries.
EOF
}

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
ROOT_DIR=$(cd "$SCRIPT_DIR/../.." && pwd)
TEMPLATE_DIR="$ROOT_DIR/unity/package-template"

VERSION=""
OUTPUT_DIR=""
ARTIFACTS_DIR=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --version)
            VERSION="$2"
            shift 2
            ;;
        --output)
            OUTPUT_DIR="$2"
            shift 2
            ;;
        --artifacts)
            ARTIFACTS_DIR="$2"
            shift 2
            ;;
        *)
            usage
            exit 1
            ;;
    esac
done

if [[ -z "$VERSION" || -z "$OUTPUT_DIR" ]]; then
    usage
    exit 1
fi

rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"
cp -R "$TEMPLATE_DIR"/. "$OUTPUT_DIR"/

export PACKAGE_JSON_PATH="$OUTPUT_DIR/package.json"
export PACKAGE_VERSION="$VERSION"
/usr/bin/python3 - <<'PY'
import json
import os
from pathlib import Path

path = Path(os.environ["PACKAGE_JSON_PATH"])
data = json.loads(path.read_text())
data["version"] = os.environ["PACKAGE_VERSION"]
path.write_text(json.dumps(data, indent=2) + "\n")
PY

if [[ -z "$ARTIFACTS_DIR" || ! -d "$ARTIFACTS_DIR" ]]; then
    echo "No artifacts directory provided; package assembled without native binaries."
    exit 0
fi

mkdir -p \
    "$OUTPUT_DIR/Plugins/Android/armeabi-v7a" \
    "$OUTPUT_DIR/Plugins/Android/arm64-v8a" \
    "$OUTPUT_DIR/Plugins/Android/x86_64" \
    "$OUTPUT_DIR/Plugins/iOS" \
    "$OUTPUT_DIR/Plugins/macOS" \
    "$OUTPUT_DIR/Plugins/x86" \
    "$OUTPUT_DIR/Plugins/x86_64" \
    "$OUTPUT_DIR/Plugins/Linux/x86_64" \
    "$OUTPUT_DIR/Plugins/Linux/arm64"

copy_if_exists() {
    local source_path="$1"
    local target_path="$2"
    if [[ -e "$source_path" ]]; then
        rm -rf "$target_path"
        mkdir -p "$(dirname "$target_path")"
        cp -R "$source_path" "$target_path"
    fi
}

copy_if_exists "$ARTIFACTS_DIR/android/armeabi-v7a/libwcdb.so" "$OUTPUT_DIR/Plugins/Android/armeabi-v7a/libwcdb.so"
copy_if_exists "$ARTIFACTS_DIR/android/arm64-v8a/libwcdb.so" "$OUTPUT_DIR/Plugins/Android/arm64-v8a/libwcdb.so"
copy_if_exists "$ARTIFACTS_DIR/android/x86_64/libwcdb.so" "$OUTPUT_DIR/Plugins/Android/x86_64/libwcdb.so"
copy_if_exists "$ARTIFACTS_DIR/ios/wcdb.xcframework" "$OUTPUT_DIR/Plugins/iOS/wcdb.xcframework"
copy_if_exists "$ARTIFACTS_DIR/macos/libwcdb.dylib" "$OUTPUT_DIR/Plugins/macOS/libwcdb.dylib"
copy_if_exists "$ARTIFACTS_DIR/windows/x86/wcdb.dll" "$OUTPUT_DIR/Plugins/x86/wcdb.dll"
copy_if_exists "$ARTIFACTS_DIR/windows/x86_64/wcdb.dll" "$OUTPUT_DIR/Plugins/x86_64/wcdb.dll"
copy_if_exists "$ARTIFACTS_DIR/linux/x86_64/libwcdb.so" "$OUTPUT_DIR/Plugins/Linux/x86_64/libwcdb.so"
copy_if_exists "$ARTIFACTS_DIR/linux/arm64/libwcdb.so" "$OUTPUT_DIR/Plugins/Linux/arm64/libwcdb.so"

if [[ -d "$OUTPUT_DIR/Plugins/iOS/wcdb.xcframework" ]]; then
    if find "$OUTPUT_DIR/Plugins/iOS/wcdb.xcframework" -mindepth 1 -maxdepth 1 -type d | grep -qv '/ios-arm64$'; then
        echo "iOS xcframework contains unsupported slices; expected ios-arm64 only." >&2
        exit 1
    fi
fi
