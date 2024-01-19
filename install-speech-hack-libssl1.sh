#!/bin/sh

# Temporary 2024 Hack for MS SpeechSDK to run on dotnet8
# https://github.com/Azure-Samples/cognitive-services-speech-sdk/issues/2204

ARCH=$(dpkg --print-architecture)

if [ "$ARCH" = "arm64" ] ; then
    BASE="http://ports.ubuntu.com/ubuntu-ports/pool/main/o/openssl/"
else
    BASE="http://security.ubuntu.com/ubuntu/pool/main/o/openssl/"
fi

wget $BASE/libssl1.1_1.1.1f-1ubuntu2.20_${ARCH}.deb
wget $BASE/libssl-dev_1.1.1f-1ubuntu2.20_${ARCH}.deb 
dpkg -i libssl1.1_1.1.1f-1ubuntu2.20_${ARCH}.deb 
dpkg -i libssl-dev_1.1.1f-1ubuntu2.20_${ARCH}.deb
rm libssl1.1_1.1.1f-1ubuntu2.20_${ARCH}.deb libssl-dev_1.1.1f-1ubuntu2.20_${ARCH}.deb


