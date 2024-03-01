#!/bin/sh

# Temporary 2024 Hack for MS SpeechSDK to run on dotnet8
# https://github.com/Azure-Samples/cognitive-services-speech-sdk/issues/2204

SSLVERSION="1.1.1f"

ARCH=$(dpkg --print-architecture)
if [ "$ARCH" = "arm64" ] ; then
    BASE="http://ports.ubuntu.com/pool/main/o/openssl"
    RELEASE="1ubuntu2"
else
    BASE="http://security.ubuntu.com/ubuntu/pool/main/o/openssl"
    RELEASE="1ubuntu2"
fi

wget ${BASE}/libssl1.1_${SSLVERSION}-${RELEASE}_${ARCH}.deb
wget ${BASE}/libssl-dev_${SSLVERSION}-${RELEASE}_${ARCH}.deb 
dpkg -i libssl1.1_${SSLVERSION}-${RELEASE}_${ARCH}.deb 
dpkg -i libssl-dev_${SSLVERSION}-${RELEASE}_${ARCH}.deb
rm libssl1.1_${SSLVERSION}-${RELEASE}_${ARCH}.deb libssl-dev_${SSLVERSION}-${RELEASE}_${ARCH}.deb


