#!/bin/bash

cd /home/jdb/work/Neuralia/neuralium/Neuralium.Cli/src/ || exit

rm -r bin/publish

if  dotnet publish -c Release --self-contained true /p:PublishTrimmed=true -r alpine-x64 -o bin/publish ; then
     echo "publish completed"
     
    docker rm neuralium-cli
    docker rmi neuralium-cli

    docker build -t neuralium-cli .

     docker tag neuralium-cli neuralium/neuralium-cli:mainnet-1.0.0
else
    echo "build failed"
fi


# docker push neuralium/neuralium-cli:mainnet-1.0.0
