#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (IArchiver.cs) is part of Oetools.Utilities.
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
using System.Collections.Generic;
using System.Threading;

namespace CabinetManager {

    public interface ICabManager {
        

        /// <summary>
        /// Sets the compression level to use when archiving.
        /// </summary>
        /// <param name="compressionLevel"></param>
        void SetCompressionLevel(CabCompressionLevel compressionLevel);

        /// <summary>
        /// Sets a cancellation token that can be used to interrupt the process if needed.
        /// </summary>
        void SetCancellationToken(CancellationToken? cancelToken);
        
        /// <summary>
        /// Event published when the archiving process is progressing.
        /// Either when a new file is archived successfully or when an archive is done.
        /// </summary>
        event EventHandler<CabProgressionEventArgs> OnProgress;
        
        /// <summary>
        /// Pack files into archives.
        /// Non existing source files will cause an <see cref="CabException"/>.
        /// Packing into an existing archive will update it.
        /// Packing existing files with update them.
        /// </summary>
        /// <param name="filesToPack"></param>
        /// <exception cref="CabException"></exception>
        void PackFileSet(IEnumerable<IFileToAddInCab> filesToPack);

        /// <summary>
        /// List all the files in an archive.
        /// </summary>
        /// <param name="archivePath"></param>
        /// <returns></returns>
        /// <exception cref="CabException"></exception>
        IEnumerable<IFileInCab> ListFiles(string archivePath);
        
        /// <summary>
        /// Extracts the given files from archives.
        /// Requesting the extraction a file that does not exist in the archive will not throw an exception.
        /// However, the <see cref="OnProgress"/> event will only be called on actually processed files.
        /// </summary>
        /// <param name="filesToExtract"></param>
        /// <exception cref="CabException"></exception>
        void ExtractFileSet(IEnumerable<IFileInCabToExtract> filesToExtract);
        
        /// <summary>
        /// Deletes the given files from archives.
        /// Requesting the deletion a file that does not exist in the archive will not throw an exception.
        /// However, the <see cref="OnProgress"/> event will only be called on actually processed files.
        /// </summary>
        /// <param name="filesToDelete"></param>
        /// <exception cref="CabException"></exception>
        void DeleteFileSet(IEnumerable<IFileInCabToDelete> filesToDelete);
    }

}