dotnet build -c Release
dotnet pack .\Community.Data.OData.Linq\Community.OData.Linq.csproj -c Release -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -o .\nupkg
dotnet pack .\Community.OData.Linq.Json\Community.OData.Linq.Json.csproj -c Release -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -o .\nupkg
dotnet pack .\Community.OData.Linq.AspNetCore\Community.OData.Linq.AspNetCore.csproj -c Release -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -o .\nupkg