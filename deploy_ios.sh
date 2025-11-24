#!/bin/bash
unset DEVELOPER_DIR
dotnet build src/NAVIGEST.iOS/NAVIGEST.iOS.csproj -f net9.0-ios -p:RuntimeIdentifier=ios-arm64 -t:Run -p:_DeviceName=00008110-001259663689401E