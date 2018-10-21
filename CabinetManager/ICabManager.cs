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

    /// <summary>
    /// <para>
    /// A file cabinet manager.
    /// Allows CRUD operations on windows cabinet file.
    /// </para>
    /// </summary>
    public interface ICabManager {
        
        /// <summary>
        /// Sets the compression level to use for the next <see cref="PackFileSet"/> process.
        /// </summary>
        /// <param name="compressionLevel"></param>
        void SetCompressionLevel(CabCompressionLevel compressionLevel);

        /// <summary>
        /// Sets a cancellation token that can be used to interrupt the process if needed.
        /// </summary>
        void SetCancellationToken(CancellationToken? cancelToken);
        
        /// <summary>
        /// <para>
        /// Event published when the archiving process is progressing.
        /// </para>
        /// </summary>
        event EventHandler<ICabProgressionEventArgs> OnProgress;
        
        /// <summary>
        /// <para>
        /// Pack (i.e. add or replace) files into cabinets.
        /// Non existing source files will not throw an exception.
        /// You can inspect which files are processed with the <see cref="OnProgress"/> event.
        /// Packing into an existing cabinet will update it.
        /// Packing existing files will update them.
        /// </para>
        /// </summary>
        /// <param name="filesToPack"></param>
        /// <exception cref="CabException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns>Total number of files actually packed.</returns>
        int PackFileSet(IEnumerable<IFileToAddInCab> filesToPack);

        /// <summary>
        /// List all the files in a cabinet.
        /// </summary>
        /// <param name="cabPath"></param>
        /// <returns></returns>
        /// <exception cref="CabException"></exception>
        IEnumerable<IFileInCab> ListFiles(string cabPath);
        
        /// <summary>
        /// <para>
        /// Extracts the given files from cabinets.
        /// Requesting the extraction from a non existing cabinet will not throw an exception.
        /// Requesting the extraction a file that does not exist in the cabinet will not throw an exception.
        /// You can inspect which files are processed with the <see cref="OnProgress"/> event.
        /// </para>
        /// </summary>
        /// <param name="filesToExtract"></param>
        /// <exception cref="CabException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns>Total number of files actually extracted.</returns>
        int ExtractFileSet(IEnumerable<IFileInCabToExtract> filesToExtract);
        
        /// <summary>
        /// <para>
        /// Deletes the given files from cabinets.
        /// Requesting the deletion from a non existing cabinet will not throw an exception.
        /// Requesting the deletion a file that does not exist in the cabinet will not throw an exception.
        /// You can inspect which files are processed with the <see cref="OnProgress"/> event.
        /// </para>
        /// </summary>
        /// <param name="filesToDelete"></param>
        /// <exception cref="CabException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns>Total number of files actually deleted.</returns>
        int DeleteFileSet(IEnumerable<IFileInCabToDelete> filesToDelete);
        
        /// <summary>
        /// <para>
        /// Moves the given files within cabinets.
        /// Requesting the movement from a non existing cabinet will not throw an exception.
        /// Requesting the movement a file that does not exist in the cabinet will not throw an exception.
        /// You can inspect which files are processed with the <see cref="OnProgress"/> event.
        /// </para>
        /// </summary>
        /// <param name="filesToMove"></param>
        /// <exception cref="CabException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns>Total number of files actually deleted.</returns>
        int MoveFileSet(IEnumerable<IFileInCabToMove> filesToMove);

        /// <summary>
        /// Returns a string representation of the <paramref name="cabPath"/>.
        /// </summary>
        /// <returns></returns>
        string ToString(string cabPath);
    }

}