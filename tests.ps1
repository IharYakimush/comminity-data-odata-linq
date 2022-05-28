dotnet restore
dotnet build --no-restore --configuration Debug
dotnet test Community.Data.OData.Linq.xTests -f net6.0 --no-build --verbosity normal --configuration Debug 