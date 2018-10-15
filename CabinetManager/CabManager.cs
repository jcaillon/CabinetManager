using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CabinetManager.core;
using Oetools.Utilities.Archive;

namespace CabinetManager {
    
    public class CabManager : ICabManager {

        private CfFolderTypeCompress _compressionType;

        private CancellationToken? _cancelToken;
        
        /// <inheritdoc cref="ICabManager.SetCompressionLevel"/>
        public void SetCompressionLevel(CabCompressionLevel compressionLevel) {
            switch (compressionLevel) {
                case CabCompressionLevel.None:
                    _compressionType = CfFolderTypeCompress.None;
                    break;
                default:
                    throw new NotImplementedException($"The compression level {compressionLevel} is not implemented yet.");
            }
        }

        /// <inheritdoc cref="ICabManager.SetCancellationToken"/>
        public void SetCancellationToken(CancellationToken? cancelToken) {
            _cancelToken = cancelToken;
        }

        /// <inheritdoc cref="ICabManager.OnProgress"/>
        public event EventHandler<CabProgressionEventArgs> OnProgress;

        /// <inheritdoc cref="ICabManager.PackFileSet"/>
        public void PackFileSet(IEnumerable<IFileToAddInCab> filesToPack) {
            foreach (var fileToCab in filesToPack) {
                if (string.IsNullOrEmpty(fileToCab.RelativePathInCab) || string.IsNullOrEmpty(fileToCab.SourcePath)) {
                    throw new ArgumentNullException("Arguments can't be empty or null");
                }
            }

            foreach (var cabGroupedFiles in filesToPack.GroupBy(f => f.CabPath)) {
                using (var cfCabinet = new CfCabinet(cabGroupedFiles.Key)) {
                    foreach (var fileToAddInCab in cabGroupedFiles) {
                        cfCabinet.AddExternalFile(fileToAddInCab.SourcePath, fileToAddInCab.RelativePathInCab);
                    }
                    cfCabinet.Save(_compressionType);
                }
            }
        }

        /// <inheritdoc cref="ICabManager.ListFiles"/>
        public IEnumerable<IFileInCab> ListFiles(string archivePath) {
            using (var cfCabinet = new CfCabinet(archivePath)) {
                return cfCabinet.GetFiles()
                    .Select(file => new FileInCab {
                        CabPath = archivePath,
                        RelativePathInCab = file.RelativePathInCab,
                        LastWriteTime = file.FileDateTime,
                        SizeInBytes = file.UncompressedFileSize
                    } as IFileInCab);
            }
        }

        /// <inheritdoc cref="ICabManager.ExtractFileSet"/>
        public void ExtractFileSet(IEnumerable<IFileInCabToExtract> filesToExtract) {
            foreach (var cabGroupedFiles in filesToExtract.GroupBy(f => f.CabPath)) {
                if (!File.Exists(cabGroupedFiles.Key)) {
                    throw new CabException($"The cabinet file does not exist : {cabGroupedFiles.Key}.");
                }
                try {
                    // create all necessary extraction folders
                    foreach (var extractDirGroupedFiles in cabGroupedFiles.GroupBy(f => Path.GetDirectoryName(f.ExtractionPath))) {
                        if (!Directory.Exists(extractDirGroupedFiles.Key)) {
                            Directory.CreateDirectory(extractDirGroupedFiles.Key);
                        }
                    }
                    using (var cfCabinet = new CfCabinet(cabGroupedFiles.Key)) {
                        foreach (var fileToExtract in cabGroupedFiles) {
                            _cancelToken?.ThrowIfCancellationRequested();
                            try {
                                cfCabinet.ExtractToFile(fileToExtract.RelativePathInCab, fileToExtract.ExtractionPath);
                            } catch (Exception e) {
                                throw new CabException($"Failed to extract {fileToExtract.ExtractionPath} from {cabGroupedFiles.Key} and relative archive path {fileToExtract.RelativePathInCab}.", e);
                            }
                            OnProgress?.Invoke(this, new CabProgressionEventArgs(CabProgressionType.FinishFile, cabGroupedFiles.Key, fileToExtract.ExtractionPath, fileToExtract.RelativePathInCab));
                        }
                    }
                } catch (OperationCanceledException) {
                    throw;
                } catch (Exception e) {
                    throw new CabException($"Failed to unpack files from {cabGroupedFiles.Key}.", e);
                }
                OnProgress?.Invoke(this, new CabProgressionEventArgs(CabProgressionType.FinishArchive, cabGroupedFiles.Key, null, null));
            }
        }

        /// <inheritdoc cref="ICabManager.DeleteFileSet"/>
        public void DeleteFileSet(IEnumerable<IFileInCabToDelete> filesToDelete) {
            OnProgress?.Invoke(this, null);
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Returns a string representation of a given cabinet file.
        /// </summary>
        /// <param name="archivePath"></param>
        /// <returns></returns>
        public string GetCabDetails(string archivePath) {
            using (var cfCabinet = new CfCabinet(archivePath)) {
                return cfCabinet.GetStringFullRepresentation();
            }
        }

        
    }
}