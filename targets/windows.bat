
cd ../

dotnet restore


dotnet publish -c Release -o ../build -r win-x64
dotnet clean
echo "publish completed"




   
