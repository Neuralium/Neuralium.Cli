#!/bin/bash

cd ../.. #expected to be ran from neuralium/Neuralium/src



if  dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishTrimmed=true /p:PublishSingleFile=true /p:PublishReadyToRun=true -o bin/publish ; then
    echo "published image"
else
    echo "publish failed"
fi
