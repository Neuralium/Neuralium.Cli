
cd ../

dotnet restore --no-cache


dotnet publish -c Release -p:PublishTrimmed=true -p:PublishSingleFile=true -o ./build -r win-x64
dotnet clean
echo "publish completed"




   
