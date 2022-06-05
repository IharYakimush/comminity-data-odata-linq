Remove-Item -Path .\nupkg -ErrorAction SilentlyContinue -Recurse
dotnet pack -c Release -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -p:VersionPrefix=0-local -o .\nupkg