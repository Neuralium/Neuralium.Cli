#!/bin/bash

cd ../

dotnet restore  --no-cache


if  dotnet publish --self-contained -c Release -o ./build -r linux-arm ; then
dotnet clean ;
 echo "publish completed"
else
echo "publish failed"
fi



   
