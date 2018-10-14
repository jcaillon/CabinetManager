using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CabinetManager.core;
using Oetools.Utilities.Archive;

namespace CabinetManager {
    
    public class CabManager : ICabManager {

        private CabCompressionLevel _compressionLevel;

        private CancellationToken? _cancelToken;
        
        public void SetCompressionLevel(CabCompressionLevel compressionLevel) {
            _compressionLevel = compressionLevel;
        }

        public void SetCancellationToken(CancellationToken? cancelToken) {
            _cancelToken = cancelToken;
        }

        public event EventHandler<CabProgressionEventArgs> OnProgress;

        public void PackFileSet(IEnumerable<IFileToAddInCab> filesToPack) {
            foreach (var fileToCab in filesToPack) {
                if (string.IsNullOrEmpty(fileToCab.RelativePathInCab) || string.IsNullOrEmpty(fileToCab.SourcePath)) {
                    throw new ArgumentNullException("Arguments can't be empty or null");
                }
            }
        }

        public IEnumerable<IFileInCab> ListFiles(string archivePath) {
            var cfCabinet = new CfCabinet(archivePath);
            return cfCabinet.Folders
                .SelectMany(folder => folder.Files)
                .Select(file => new FileInCab {
                    CabPath = archivePath,
                    RelativePathInCab = file.RelativePathInCab,
                    LastWriteTime = file.FileDateTime,
                    SizeInBytes = file.UncompressedFileSize
                } as IFileInCab);
        }

        public void ExtractFileSet(IEnumerable<IFileInCabToExtract> filesToExtract) {
            foreach (var cabGroupedFiles in filesToExtract.GroupBy(f => f.CabPath)) {
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

                        fileInCab.ExtractToFile(stream, fileToExtractFromCab.ExtractionPath);
                    }
                }
            }
        }

        public void DeleteFileSet(IEnumerable<IFileInCabToDelete> filesToDelete) {
            OnProgress?.Invoke(this, null);
            throw new NotImplementedException();
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

        
    }
}