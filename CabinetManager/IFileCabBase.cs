#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (IFileArchivedBase.cs) is part of Oetools.Utilities.
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

namespace CabinetManager {
    
    /// <summary>
    /// Basic file describer.
    /// </summary>
    public interface IFileCabBase {
        
        /// <summary>
        /// Path to the cabinet file in which this file is archived.
        /// </summary>
        string CabPath { get; }
        
        /// <summary>
        /// <para>
        /// Relative path of the file within the cabinet file.
        /// This path is normalized to windows style path inside the cabinet (i.e. using \ instead of /).
        /// </para>
        /// </summary>
        string RelativePathInCab { get; }
        
        /// <summary>
        /// Boolean set after an archiver action which indicates if this file was actually processed.
        /// </summary>
        bool Processed { get; set; }
        
    }
}