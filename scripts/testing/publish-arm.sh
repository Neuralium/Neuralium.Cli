#!/bin/bash

cd /home/jdb/work/Neuralia/neuralium/Neuralium.Cli/src || exit

rm -r bin/publish

if  dotnet publish -c Release --self-contained true  /p:PublishTrimmed=true /p:PublishSingleFile=true /p:PublishReadyToRun=true -r linux-arm64 -o bin/publish-arm ; then
     echo "publish completed"
    
else
    echo "build failed"
fi

