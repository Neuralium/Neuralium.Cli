#!/bin/bash

cd ../

dotnet restore 


if  dotnet publish -c Release -o ../build -r linux-x64 ; then
	dotnet clean ;
 echo "publish completed"
else
echo "publish failed"
fi



   
