# Cabinet manager

Handles microsoft cabinet format (.cab files) in pure C#.

Status | Info
------ | --------
[![Build status](https://ci.appveyor.com/api/projects/status/8v4fn7trm69554ih/branch/master?svg=true)](https://ci.appveyor.com/project/jcaillon/cabinetmanager) | Windows CI Provided By [AppVeyor][]
![NuGet](https://img.shields.io/nuget/v/Noyacode.CabinetManager.svg) | Latest [Nuget][] Package
[![GitHub release](https://img.shields.io/github/release/jcaillon/CabinetManager.svg)](https://github.com/jcaillon/CabinetManager/releases/latest) | Latest github release
[![Total downloads](https://img.shields.io/github/downloads/jcaillon/CabinetManager/total.svg)](https://github.com/jcaillon/CabinetManager/releases) | Total download
[![GPLv3 licence](https://img.shields.io/badge/License-GPLv3-74A5C2.svg)](https://github.com/jcaillon/CabinetManager/blob/master/LICENSE) | GPLv3 License

[![logo](docs/images/logo.png)](https://jcaillon.github.io/CabinetManager/)

[AppVeyor]:http://www.appveyor.com/
[Nuget]:https://www.nuget.org/packages/Noyacode.CabinetManager/

## About

## Microsoft cabinet file specifications

[https://msdn.microsoft.com/en-us/library/bb417343.aspx#cabinet_format](https://msdn.microsoft.com/en-us/library/bb417343.aspx#cabinet_format)

## TODO, implement compression algo

- [MSZIP](https://msdn.microsoft.com/library/bb417343.aspx#microsoftmszipdatacompressionformat)
- [LZX](https://msdn.microsoft.com/en-us/library/bb417343.aspx#lzxdatacompressionformat)
- Quantum does not seems to be used very often...

Note : c++ implementation -> https://github.com/coderforlife/ms-compress/tree/master/src

## Thanks

### Jetbrain

This project was developped using an opensource license of the **awesome** :

[![resharper](docs/images/resharper.png)](https://www.jetbrains.com/)

### Icon8

The Sakoe logo is provided by the **awesome** :

[![icon8](https://png.icons8.com/color/48/000000/icons8-logo.png)](https://icons8.com/)
