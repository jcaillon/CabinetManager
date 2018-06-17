#region header

// ========================================================================
// Copyright (c) 2017 - Julien Caillon (julien.caillon@gmail.com)
// This file (IUnpackStreamContext.cs) is part of csdeployer.
// 
// csdeployer is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// csdeployer is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with csdeployer. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================

#endregion

using System;
using System.IO;

namespace CabinetManagerTest.Compression {
    /// <summary>
    /// This interface provides the methods necessary for the <see cref="CompressionEngine"/> to open
    /// and close streams for archives and files. The implementor of this interface can use any
    /// kind of logic to determine what kind of streams to open and where 
    /// </summary>
    public interface IUnpackStreamContext {
        /// <summary>
        /// Opens the archive stream for reading.
        /// </summary>
        /// <param name="archiveNumber">The zero-based index of the archive to open.</param>
        /// <param name="archiveName">The name of the archive being opened.</param>
        /// <param name="compressionEngine">Instance of the compression engine doing the operations.</param>
        /// <returns>A stream from which archive bytes are read, or null to cancel extraction
        /// of the archive.</returns>
        /// <remarks>
        /// When the first archive in a chain is opened, the name is not yet known, so the
        /// provided value will be an empty string. When opening further archives, the
        /// provided value is the next-archive name stored in the previous archive. This
        /// name is often, but not necessarily, the same as the filename of the archive
        /// package to be opened.
        /// <para>If this method returns null, the archive engine will throw a
        /// FileNotFoundException.</para>
        /// </remarks>
        Stream OpenArchiveReadStream(int archiveNumber, string archiveName, CompressionEngine compressionEngine);

        /// <summary>
        /// Closes a stream where an archive package was read.
        /// </summary>
        /// <param name="archiveNumber">The archive number of the stream to close.</param>
        /// <param name="archiveName">The name of the archive being closed.</param>
        /// <param name="stream">The stream that was previously returned by
        /// <see cref="OpenArchiveReadStream"/> and is now ready to be closed.</param>
        void CloseArchiveReadStream(int archiveNumber, string archiveName, Stream stream);

        /// <summary>
        /// Opens a stream for writing extracted file bytes.
        /// </summary>
        /// <param name="path">The path of the file within the archive. This is often, but
        /// not necessarily, the same as the relative path of the file outside the archive.</param>
        /// <param name="fileSize">The uncompressed size of the file to be extracted.</param>
        /// <param name="lastWriteTime">The last write time of the file to be extracted.</param>
        /// <returns>A stream where extracted file bytes are to be written, or null to skip
        /// extraction of the file and continue to the next file.</returns>
        /// <remarks>
        /// The implementor may use the path, size and date information to dynamically
        /// decide whether or not the file should be extracted.
        /// </remarks>
        Stream OpenFileWriteStream(string path, long fileSize, DateTime lastWriteTime);

        /// <summary>
        /// Closes a stream where an extracted file was written.
        /// </summary>
        /// <param name="path">The path of the file within the archive.</param>
        /// <param name="stream">The stream that was previously returned by <see cref="OpenFileWriteStream"/>
        /// and is now ready to be closed.</param>
        /// <param name="attributes">The attributes of the extracted file.</param>
        /// <param name="lastWriteTime">The last write time of the file.</param>
        /// <remarks>
        /// The implementor may wish to apply the attributes and date to the newly-extracted file.
        /// </remarks>
        void CloseFileWriteStream(string path, Stream stream, FileAttributes attributes, DateTime lastWriteTime);
    }
}