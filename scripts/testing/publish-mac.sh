#!/bin/bash

cd ../.. #expected to be ran from neuralium/Neuralium/src



if  dotnet publish -c Release -r osx-x64 --self-contained true /p:PublishTrimmed=true  /p:PublishSingleFile=true -o bin/publish ; then
    echo "published image"
else
    echo "publish failed"
fi
