#!/bin/bash

cd "./$(dirname "$0")"
cd "../"

platforms=("linux-x64" "linux-arm64" "win-x64")

for platform in "${platforms[@]}"
do
    rm -rf "./App"
    
    printf "Building Confab release package for platform: ${platform}...\n"
    python3 confab-builder.py --clean --platform ${platform} --bundle-runtime

    7za a ./release_pkgs/Confab-v$(cat version)-g$(git rev-parse --short HEAD)-${platform}.zip ./App/*
done

printf "Preparing for Docker image build...\n"

rm -rf "./App"
python3 confab-builder.py --clean --platform linux-musl-x64