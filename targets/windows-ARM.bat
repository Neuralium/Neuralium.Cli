
cd ../

dotnet restore --no-cache


dotnet publish --self-contained -c Release -o ./build -r win8-arm
dotnet clean
echo "publish completed"




   
