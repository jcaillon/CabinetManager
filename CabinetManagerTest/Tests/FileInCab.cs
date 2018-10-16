#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (FileInCab.cs) is part of CabinetManagerTest.
// 
// CabinetManagerTest is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// CabinetManagerTest is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with CabinetManagerTest. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion

using System;
using CabinetManager;

namespace CabinetManagerTest.Tests {
    public class FileInCab : IFileInCab, IFileInCabToDelete, IFileInCabToExtract, IFileToAddInCab {
        public string CabPath { get; set; }
        public string RelativePathInCab { get; set; }
        public ulong SizeInBytes { get; set; }
        public DateTime LastWriteTime { get; set; }
        public string ExtractionPath { get; set; }
        public string SourcePath { get; set; }
    }
}