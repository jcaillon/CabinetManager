# CabinetManager

## About

## Microsoft cabinet file specifications

[https://msdn.microsoft.com/en-us/library/bb417343.aspx#cabinet_format](https://msdn.microsoft.com/en-us/library/bb417343.aspx#cabinet_format)

## TODO, implement compression algo

[MSZIP](https://msdn.microsoft.com/library/bb417343.aspx#microsoftmszipdatacompressionformat)
[LZX](https://msdn.microsoft.com/en-us/library/bb417343.aspx#lzxdatacompressionformat)

Quantum does not seems to be used very often...

Note : c++ implementation -> https://github.com/coderforlife/ms-compress/tree/master/src


## Init

```bash
dotnet new sln
mkdir CabinetManager
cd CabinetManager
dotnet new classlib
cd ..
dotnet sln add CabinetManager\CabinetManager.csproj
dotnet sln add CabinetManager\CabinetManager.csproj
mkdir CabinetManagerTest
cd CabinetManagerTest
dotnet new mstest
dotnet add reference ..\CabinetManager\CabinetManager.csproj
cd ..
dotnet sln add CabinetManagerTest\CabinetManagerTest.csproj
```