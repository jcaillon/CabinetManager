using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CabinetManager.core;
using CabinetManager.Utilities;

namespace CabinetManager {
    
    /// <summary>
    /// A cabinet file manager
    /// </summary>
    public class CabManager : ICabManager {

        /// <summary>
        /// Get a new instance of cabinet manager.
        /// </summary>
        /// <returns></returns>
        public static ICabManager New() => new CabManager();

        private CabManager() { }

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
        public void PackFileSet(IEnumerable<IFileToAddInCab> filesToPackIn) {
            var filesToPack = filesToPackIn.ToList();
            filesToPack.ForEach(f => f.RelativePathInCab = f.RelativePathInCab.NormalizeRelativePath());
            foreach (var cabGroupedFiles in filesToPack.GroupBy(f => f.CabPath)) {
                try {                
                    using (var cfCabinet = new CfCabinet(cabGroupedFiles.Key)) {
                        foreach (var fileToAddInCab in cabGroupedFiles) {
                            cfCabinet.AddExternalFile(fileToAddInCab.SourcePath, fileToAddInCab.RelativePathInCab);
                            OnProgress?.Invoke(this, new CabProgressionEventArgs(CabProgressionType.FileProcessed, cabGroupedFiles.Key, fileToAddInCab.SourcePath, fileToAddInCab.RelativePathInCab));
                        }
                        cfCabinet.Save(_compressionType, args => {
                            if (args != null && filesToPack.Exists(f => f.RelativePathInCab.Equals(args.RelativePathInCab, StringComparison.OrdinalIgnoreCase))) {
                                
                            }
                        });
                    }
                } catch (OperationCanceledException) {
                    throw;
                } catch (Exception e) {
                    throw new CabException($"Failed to pack to {cabGroupedFiles.Key}.", e);
                }
                OnProgress?.Invoke(this, new CabProgressionEventArgs(CabProgressionType.ArchiveCompleted, cabGroupedFiles.Key, null, null));
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
                        if (!Directory.Exists(extractDirGroupedFiles.Key) && !string.IsNullOrWhiteSpace(extractDirGroupedFiles.Key)) {
                            Directory.CreateDirectory(extractDirGroupedFiles.Key);
                        }
                    }
                    using (var cfCabinet = new CfCabinet(cabGroupedFiles.Key)) {
                        foreach (var file in cfCabinet.GetFiles()) {
                            _cancelToken?.ThrowIfCancellationRequested();
                            var fileToExtract = cabGroupedFiles.FirstOrDefault(f => f.RelativePathInCab.NormalizeRelativePath().Equals(file.RelativePathInCab, StringComparison.OrdinalIgnoreCase));
                            if (fileToExtract != null) {
                                try {
                                    if (cfCabinet.ExtractToFile(fileToExtract.RelativePathInCab.NormalizeRelativePath(), fileToExtract.ExtractionPath)) {
                                        OnProgress?.Invoke(this, new CabProgressionEventArgs(CabProgressionType.FileProcessed, cabGroupedFiles.Key, fileToExtract.ExtractionPath, fileToExtract.RelativePathInCab));
                                    }
                                } catch (Exception e) {
                                    throw new CabException($"Failed to extract {fileToExtract.ExtractionPath} from {cabGroupedFiles.Key} and relative archive path {fileToExtract.RelativePathInCab}.", e);
                                }
                            }
                        }
                    }
                } catch (OperationCanceledException) {
                    throw;
                } catch (Exception e) {
                    throw new CabException($"Failed to unpack files from {cabGroupedFiles.Key}.", e);
                }
                OnProgress?.Invoke(this, new CabProgressionEventArgs(CabProgressionType.ArchiveCompleted, cabGroupedFiles.Key, null, null));
            }
        }

        /// <inheritdoc cref="ICabManager.DeleteFileSet"/>
        public void DeleteFileSet(IEnumerable<IFileInCabToDelete> filesToDeleteIn) {
            var filesToDelete = filesToDeleteIn.ToList();
            filesToDelete.ForEach(f => f.RelativePathInCab = f.RelativePathInCab.NormalizeRelativePath());
            foreach (var cabGroupedFiles in filesToDelete.GroupBy(f => f.CabPath)) {
                if (!File.Exists(cabGroupedFiles.Key)) {
                    throw new CabException($"The cabinet file does not exist : {cabGroupedFiles.Key}.");
                }
                try {
                    using (var cfCabinet = new CfCabinet(cabGroupedFiles.Key)) {
                        foreach (var file in cabGroupedFiles) {
                            _cancelToken?.ThrowIfCancellationRequested();
                            
                            if (cfCabinet.DeleteFile(file.RelativePathInCab)) {
                                OnProgress?.Invoke(this, new CabProgressionEventArgs(CabProgressionType.FileProcessed, cabGroupedFiles.Key, null, file.RelativePathInCab));
                            }
                        }
                        cfCabinet.Save(_compressionType, null);
                    }
                } catch (OperationCanceledException) {
                    throw;
                } catch (Exception e) {
                    throw new CabException($"Failed to delete files from {cabGroupedFiles.Key}.", e);
                }
                OnProgress?.Invoke(this, new CabProgressionEventArgs(CabProgressionType.ArchiveCompleted, cabGroupedFiles.Key, null, null));
            }
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