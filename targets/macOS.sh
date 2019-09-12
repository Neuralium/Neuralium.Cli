#!/bin/bash

cd ../

dotnet restore


if  dotnet publish -c Release -o ../build -r osx-x64 ; then
dotnet clean ;
 echo "publish completed"
else
echo "publish failed"
fi



   
