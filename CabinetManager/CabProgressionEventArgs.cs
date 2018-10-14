#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (ArchiverProgressionEventArgs.cs) is part of Oetools.Utilities.
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

namespace Oetools.Utilities.Archive {
    
    public class CabProgressionEventArgs : EventArgs {
        
        public CabProgressionType ProgressionType { get; set; }

        /// <summary>
        /// Gets the name of the file being processed. (The name of the file within the Archive; not the external
        /// file path.) Also includes the internal path of the file, if any.
        /// </summary>
        /// <value>
        /// The name of the file currently being processed, or null if processing
        /// is currently at the stream or archive level.
        /// </value>
        public string SourcePath { get; private set; }
        
        public string RelativePathInArchive { get; private set; }
        
        public string ArchivedPath { get; private set; }

        public CabProgressionEventArgs(CabProgressionType progressionType, string archivePath, string sourcePath, string relativePathInArchive) {
            ArchivedPath = archivePath;
            SourcePath = sourcePath;
            RelativePathInArchive = relativePathInArchive;
            ProgressionType = progressionType;
        }
    }
}