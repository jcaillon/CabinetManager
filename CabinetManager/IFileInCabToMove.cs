#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (IFileArchivedToDelete.cs) is part of Oetools.Utilities.
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
    /// Describes a file existing in a cabinet file that needs to be deleted in it.
    /// </summary>
    public interface IFileInCabToMove : IFileCabBase {
        
        /// <summary>
        /// <para>
        /// The new relative path of the file within the cabinet file.
        /// This is the destination path for a file to move.
        /// This path is normalized to windows style path inside the cabinet (i.e. using \ instead of /).
        /// </para>
        /// </summary>
        string NewRelativePathInCab { get; }
    }
}