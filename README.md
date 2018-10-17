# Cabinet manager

Handles microsoft cabinet format (.cab files) in pure C#.

[![logo](docs/logo.png)](https://jcaillon.github.io/CabinetManager/)

Status | Info
------ | --------
[![Build status](https://ci.appveyor.com/api/projects/status/8v4fn7trm69554ih/branch/master?svg=true)](https://ci.appveyor.com/project/jcaillon/cabinetmanager) | Windows CI Provided By [AppVeyor][]
![NuGet](https://img.shields.io/nuget/v/Noyacode.CabinetManager.svg) | Latest [Nuget][] Package
[![GitHub release](https://img.shields.io/github/release/jcaillon/CabinetManager.svg)](https://github.com/jcaillon/CabinetManager/releases/latest) | Latest github release
[![Total downloads](https://img.shields.io/github/downloads/jcaillon/CabinetManager/total.svg)](https://github.com/jcaillon/CabinetManager/releases) | Total download
[![GPLv3 licence](https://img.shields.io/badge/License-GPLv3-74A5C2.svg)](https://github.com/jcaillon/CabinetManager/blob/master/LICENSE) | GPLv3 License


[AppVeyor]:http://www.appveyor.com/
[Nuget]:https://www.nuget.org/packages/Noyacode.CabinetManager/

## About

### Microsoft cabinet file specifications

This library implements the following specifications:

[https://msdn.microsoft.com/en-us/library/bb417343.aspx#cabinet_format](https://msdn.microsoft.com/en-us/library/bb417343.aspx#cabinet_format)

### Limitations of this lib

This library is not a complete implementation of the specifications (mostly because I didn't need all the features).

The limitations are listed below:

- Does not handle multi-cabinet file (a single cabinet can hold up to ~2Gb of data)
- Does not handle compressed data, you can only store/retrieve uncompressed data
- Does not compute nor verify checksums

## Thanks

This project was developped using an opensource license of the [rider by jetbrains](https://www.jetbrains.com/).

The logo is provided by the [icons8](https://icons8.com/).
