
cd ../

dotnet restore --no-cache


dotnet publish --self-contained true -p:PublishTrimmed=true -p:PublishSingleFile=true -c Release -o ./build -r win-x64
dotnet clean
echo "publish completed"




   
