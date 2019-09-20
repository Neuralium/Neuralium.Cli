
cd ../

dotnet restore --no-cache


dotnet publish -c Release -o ../build -r win-x64
dotnet clean
echo "publish completed"




   
