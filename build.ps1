if(Test-Path .\artifacts) { Remove-Item .\artifacts -Force -Recurse }

dotnet restore .\Community.Data.OData.Linq\Community.OData.Linq.csproj

dotnet build .\Community.Data.OData.Linq\Community.OData.Linq.csproj -c Release

# dotnet pack .\Community.Data.OData.Linq\Community.OData.Linq.csproj -c Release -o ..\artifacts