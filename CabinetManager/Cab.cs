using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CabinetManager.core;

namespace CabinetManager {
    public class Cab : ICab {
        public void PackFileSet(List<IFileToCab> files, CabCompressionLevel cabCompressionLevel, EventHandler<CabProgressionEventArgs> progressHandler = null) {
            foreach (var fileToCab in files) {
                if (string.IsNullOrEmpty(fileToCab.RelativePathInCab) || string.IsNullOrEmpty(fileToCab.SourcePath)) {
                    throw new ArgumentNullException("Arguments can't be empty or null");
                }

                if (_cabPathToSourcePathMap.ContainsKey(fileToCab.RelativePathInCab)) {
                    throw new ArgumentException($"The path already exists in this cab : {fileToCab.RelativePathInCab}");
                }
            }
        }

        public List<IFileInCab> ListFiles(string archivePath) {
            var cfCabinet = new CfCabinet(archivePath);
            return cfCabinet.Folders
                .SelectMany(folder => folder.Files)
                .Select(file => new FileInCab {
                    CabPath = archivePath,
                    RelativePathInCab = file.RelativePathInCab,
                    LastWriteTime = file.FileDateTime,
                    SizeInBytes = file.UncompressedFileSize
                } as IFileInCab)
                .ToList();
        }

        public void ExtractFileSet(List<IFileToExtractFromCab> files, EventHandler<CabProgressionEventArgs> progressHandler = null) {
            foreach (var cabGroupedFiles in files.GroupBy(f => f.CabPath)) {
                if (string.IsNullOrEmpty(cabGroupedFiles.Key)) {
                    throw new CabException("Invalid cab path, can't be null");
                }

                var cfCabinet = new CfCabinet(cabGroupedFiles.Key);
                if (!cfCabinet.Exists) {
                    throw new CabException($"The .cab doesn't exist : {cabGroupedFiles.Key}");
                }

                var filesInCab = cfCabinet.Folders.SelectMany(folder => folder.Files).ToList();

                using (Stream stream = File.OpenRead(cabGroupedFiles.Key)) {
                    foreach (var fileToExtractFromCab in cabGroupedFiles) {
                        var fileInCab = filesInCab.FirstOrDefault(f => f.RelativePathInCab.Equals(fileToExtractFromCab.RelativePathInCab, StringComparison.CurrentCultureIgnoreCase));
                        if (fileInCab == null) {
                            throw new CabException($"The file {fileToExtractFromCab.RelativePathInCab ?? "null"} doesn't exist in {cabGroupedFiles.Key}");
                        }

                        fileInCab.ExtractToFile(stream, fileToExtractFromCab.ToPath);
                    }
                }
            }
        }

        public string GetCabDetails(string archivePath) {
            var cfCabinet = new CfCabinet(archivePath);
            using (Stream stream = File.OpenRead(archivePath)) {
                foreach (var cfFolder in cfCabinet.Folders) {
                    cfFolder.ReadDataHeaderFromStream(stream);
                }
            }

            return cfCabinet.ToString();
        }

        /// <summary>
        /// Map a relative path in the cab file to the absolute file path
        /// </summary>
        /// <example>file1.cool -> C:\file1.txt</example>
        /// <example>folder\file1.cool -> C:\file1.txt</example>
        private Dictionary<string, string> _cabPathToSourcePathMap = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

        private CfCabinet _cabinet;
        private List<CfFolder> _folders;
        private List<CfFile> _files;
        private List<CfData> _data;

        public void AddFile(string relativePathInCab, string absoluteFilePath) {
            _cabPathToSourcePathMap.Add(relativePathInCab, absoluteFilePath);
        }

        public void Save(string cabFilePath) {
            /*
            Stream dest = ...
            using(Stream source = File.OpenRead(path)) {
                byte[] buffer = new byte[2048];
                int bytesRead;
                while((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0) {
                    dest.Write(buffer, 0, bytesRead);
                }
            }
            */

            /*
            try {
                

                    foreach (string file in files) {
                        FileAttributes attributes;
                        DateTime lastWriteTime;
                        Stream fileStream = context.OpenFileReadStream(
                            file,
                            out attributes,
                            out lastWriteTime);
                        if (fileStream != null) {
                            totalFileBytes += fileStream.Length;
                            totalFiles++;
                            context.CloseFileReadStream(file, fileStream);
                        }
                    }

                    long uncompressedBytesInFolder = 0;
                    currentFileNumber = -1;

                    foreach (string file in files) {
                        FileAttributes attributes;
                        DateTime lastWriteTime;
                        Stream fileStream = context.OpenFileReadStream(
                            file, out attributes, out lastWriteTime);
                        if (fileStream == null) {
                            continue;
                        }

                        if (fileStream.Length >= NativeMethods.FCI.MAX_FOLDER) {
                            throw new NotSupportedException(String.Format(
                                CultureInfo.InvariantCulture,
                                "File {0} exceeds maximum file size " +
                                "for cabinet format.",
                                file));
                        }

                        if (uncompressedBytesInFolder > 0) {
                            // Automatically create a new folder if this file
                            // won't fit in the current folder.
                            bool nextFolder = uncompressedBytesInFolder
                                              + fileStream.Length >= NativeMethods.FCI.MAX_FOLDER;

                            // Otherwise ask the client if it wants to
                            // move to the next folder.
                            if (!nextFolder) {
                                object nextFolderOption = streamContext.GetOption(
                                    "nextFolder",
                                    new object[] {file, currentFolderNumber});
                                nextFolder = Convert.ToBoolean(
                                    nextFolderOption, CultureInfo.InvariantCulture);
                            }

                            if (nextFolder) {
                                FlushFolder();
                                uncompressedBytesInFolder = 0;
                            }
                        }

                        if (currentFolderTotalBytes > 0) {
                            currentFolderTotalBytes = 0;
                            currentFolderNumber++;
                            uncompressedBytesInFolder = 0;
                        }

                        currentFileName = file;
                        currentFileNumber++;

                        currentFileTotalBytes = fileStream.Length;
                        currentFileBytesProcessed = 0;
                        OnProgress(ArchiveProgressType.StartFile);

                        uncompressedBytesInFolder += fileStream.Length;

                        AddFile(
                            file,
                            fileStream,
                            attributes,
                            lastWriteTime,
                            false,
                            CompressionLevel);
                    }

                    FlushFolder();
                    FlushCabinet();
                } finally {
                    if (CabStream != null) {
                        context.CloseArchiveWriteStream(
                            currentArchiveNumber,
                            currentArchiveName,
                            CabStream);
                        CabStream = null;
                    }

                    if (FileStream != null) {
                        context.CloseFileReadStream(
                            currentFileName, FileStream);
                        FileStream = null;
                    }
                    context = null;

                    if (fciHandle != null) {
                        fciHandle.Dispose();
                        fciHandle = null;
                    }
                }

             */
        }
    }
}