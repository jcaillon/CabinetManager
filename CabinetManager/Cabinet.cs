using System;
using System.Collections.Generic;

namespace CabinetManager.core {

    public class Cabinet : ICabinet {

        internal const uint CabinetMaximumSize = uint.MaxValue;
        internal const int FileMaximumSize = 0x7FFF8000;
        internal const int CabPathMaximumLength = 256;

        public List<FileToAdd> FilesToAdd;

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
            if (string.IsNullOrEmpty(relativePathInCab) || string.IsNullOrEmpty(absoluteFilePath)) {
                throw new ArgumentNullException("Arguments can't be empty or null");
            }
            if (_cabPathToSourcePathMap.ContainsKey(relativePathInCab)) {
                throw new ArgumentException($"The path already exists in this cab : {relativePathInCab}");
            }
            if (relativePathInCab.Length > CabPathMaximumLength) {
                throw new ArgumentException($"The provided path is too long for .cab file, maximum length is {CabPathMaximumLength}, provided path is {relativePathInCab.Length}");
            }

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