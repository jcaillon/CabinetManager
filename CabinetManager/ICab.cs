using System;
using System.Collections.Generic;

namespace CabinetManager {
    public interface ICab {
        /// <summary>
        /// Copy/compress files into an archive
        /// </summary>
        /// <param name="files">List of files to archive</param>
        /// <param name="cabCompressionLevel">The compression level used when creating the archive</param>
        /// <param name="progressHandler">Handler for receiving progress information; this may be null if progress is not desired</param>
        void PackFileSet(List<IFileToCab> files, CabCompressionLevel cabCompressionLevel, EventHandler<CabProgressionEventArgs> progressHandler = null);

        /// <summary>
        /// List all the files in an archive
        /// </summary>
        /// <param name="archivePath"></param>
        /// <returns></returns>
        List<IFileInCab> ListFiles(string archivePath);

        /// <summary>
        /// Extracts the given files from cab files
        /// </summary>
        /// <param name="files"></param>
        /// <param name="progressHandler"></param>
        void ExtractFileSet(List<IFileToExtractFromCab> files, EventHandler<CabProgressionEventArgs> progressHandler = null);
    }
}