#!/bin/bash

cd /home/jdb/work/Neuralia/neuralium/Neuralium.Cli/src/ || exit

rm -r bin/publish

if  dotnet publish -c Release --self-contained true /p:PublishTrimmed=true -r linux-arm -o bin/publish ; then
     echo "publish completed"
     
    docker rm neuralium-cli-arm
    docker rmi neuralium-cli-arm

    docker build -t neuralium-cli-arm .

    docker tag neuralium-cli-arm neuralium/neuralium-cli:arm-mainnet-1.0.0
else
    echo "build failed"
fi


# docker push neuralium/neuralium-cli:arm-mainnet-1.0.0
