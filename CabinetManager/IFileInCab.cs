#region header

// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (IFilePackaged.cs) is part of Oetools.Utilities.
// 
// Oetools.Utilities is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Utilities is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Utilities. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================

#endregion

using System;

namespace CabinetManager {
    public interface IFileInCab {
        /// <summary>
        ///     Path to the archive in which this file is archived
        /// </summary>
        string CabPath { get; set; }

        /// <summary>
        /// Give the relative path of the file in the archive/package
        /// </summary>
        string RelativePathInCab { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        ulong SizeInBytes { get; set; }

        /// <summary>
        /// Date last modified
        /// </summary>
        DateTime LastWriteTime { get; set; }
    }
}