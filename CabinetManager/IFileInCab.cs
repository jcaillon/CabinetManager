#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (IFileArchived.cs) is part of Oetools.Utilities.
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
using System.IO;

namespace CabinetManager {
    
    /// <summary>
    /// Describes a file present in a cabinet file.
    /// </summary>
    public interface IFileInCab : IFileCabBase {
               
        /// <summary>
        /// File size in bytes.
        /// </summary>
        /// <remarks>
        /// This is the real file size once extracted from the cabinet file.
        /// </remarks>
        ulong SizeInBytes { get; }
        
        /// <summary>
        /// Date last modified.
        /// </summary>
        DateTime LastWriteTime { get; }
        
        /// <summary>
        /// File attributes inside the cabinet.
        /// </summary>
        FileAttributes FileAttributes { get; }
    }
}