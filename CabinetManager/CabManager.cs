using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CabinetManager.core;
using CabinetManager.@internal;
using CabinetManager.Utilities;

namespace CabinetManager {
    
    /// <summary>
    /// A cabinet file manager, see <see cref="New"/> method to get an instance.
    /// </summary>
    public class CabManager : ICabManager {

        /// <summary>
        /// Get a new instance of the cabinet manager.
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
        public event EventHandler<ICabProgressionEventArgs> OnProgress;

        /// <inheritdoc cref="ICabManager.PackFileSet"/>
        public int PackFileSet(IEnumerable<IFileToAddInCab> filesToPackIn) {
            int nbFilesProcessed = 0;
            foreach (var cabGroupedFiles in filesToPackIn.GroupBy(f => f.CabPath)) {
                try {                
                    using (var cfCabinet = new CfCabinet(cabGroupedFiles.Key, _cancelToken)) {
                        cfCabinet.OnProgress += OnProgressionEvent;
                        try {
                            foreach (var fileToAddInCab in cabGroupedFiles) {
                                if (File.Exists(fileToAddInCab.SourcePath)) {
                                    var fileRelativePath = fileToAddInCab.RelativePathInCab.NormalizeRelativePath();
                                    cfCabinet.AddExternalFile(fileToAddInCab.SourcePath, fileRelativePath);
                                    nbFilesProcessed++;
                                    OnProgress?.Invoke(this, CabProgressionEventArgs.NewCompletedFile(cabGroupedFiles.Key, fileToAddInCab.RelativePathInCab));
                                }
                            }
                            cfCabinet.Save(_compressionType);
                        } finally {
                            cfCabinet.OnProgress -= OnProgressionEvent;
                        }
                    }
                } catch (OperationCanceledException) {
                    throw;
                } catch (Exception e) {
                    throw new CabException($"Failed to pack to {cabGroupedFiles.Key}.", e);
                }
                OnProgress?.Invoke(this, CabProgressionEventArgs.NewCompletedCabinet(cabGroupedFiles.Key));
            }
            return nbFilesProcessed;
        }

        /// <inheritdoc cref="ICabManager.ListFiles"/>
        public IEnumerable<IFileInCab> ListFiles(string cabPath) {
            using (var cfCabinet = new CfCabinet(cabPath, _cancelToken)) {
                return cfCabinet.GetFiles()
                    .Select(file => new FileInCab {
                        CabPath = cabPath,
                        RelativePathInCab = file.RelativePathInCab,
                        LastWriteTime = file.FileDateTime,
                        SizeInBytes = file.UncompressedFileSize,
                        FileAttributes = GetFileAttributes(file.FileAttributes)
                    } as IFileInCab);
            }
        }

        /// <inheritdoc cref="ICabManager.ExtractFileSet"/>
        public int ExtractFileSet(IEnumerable<IFileInCabToExtract> filesToExtract) {
            int nbFilesProcessed = 0;
            foreach (var cabGroupedFiles in filesToExtract.GroupBy(f => f.CabPath)) {
                try {      
                    // create all necessary extraction folders
                    foreach (var extractDirGroupedFiles in cabGroupedFiles.GroupBy(f => Path.GetDirectoryName(f.ExtractionPath))) {
                        if (!Directory.Exists(extractDirGroupedFiles.Key) && !string.IsNullOrWhiteSpace(extractDirGroupedFiles.Key)) {
                            Directory.CreateDirectory(extractDirGroupedFiles.Key);
                        }
                    }
                    using (var cfCabinet = new CfCabinet(cabGroupedFiles.Key, _cancelToken)) {
                        cfCabinet.OnProgress += OnProgressionEvent;
                        try {
                            foreach (var fileInCabToExtract in cabGroupedFiles) {
                                var fileRelativePath = fileInCabToExtract.RelativePathInCab.NormalizeRelativePath();
                                if (cfCabinet.ExtractToFile(fileRelativePath, fileInCabToExtract.ExtractionPath)) {
                                    nbFilesProcessed++;
                                    OnProgress?.Invoke(this, CabProgressionEventArgs.NewCompletedFile(cabGroupedFiles.Key, fileInCabToExtract.RelativePathInCab));
                                }
                            }
                        } finally {
                            cfCabinet.OnProgress -= OnProgressionEvent;
                        }
                    }
                } catch (OperationCanceledException) {
                    throw;
                } catch (Exception e) {
                    throw new CabException($"Failed to extract files from {cabGroupedFiles.Key}.", e);
                }
                OnProgress?.Invoke(this, CabProgressionEventArgs.NewCompletedCabinet(cabGroupedFiles.Key));
            }
            return nbFilesProcessed;
        }

        /// <inheritdoc cref="ICabManager.DeleteFileSet"/>
        public int DeleteFileSet(IEnumerable<IFileInCabToDelete> filesToDeleteIn) {
            int nbFilesProcessed = 0;
            foreach (var cabGroupedFiles in filesToDeleteIn.GroupBy(f => f.CabPath)) {
                try {                
                    using (var cfCabinet = new CfCabinet(cabGroupedFiles.Key, _cancelToken)) {
                        cfCabinet.OnProgress += OnProgressionEvent;
                        try {
                            foreach (var fileToAddInCab in cabGroupedFiles) {
                                var fileRelativePath = fileToAddInCab.RelativePathInCab.NormalizeRelativePath();
                                if (cfCabinet.DeleteFile(fileRelativePath)) {
                                    nbFilesProcessed++;
                                    OnProgress?.Invoke(this, CabProgressionEventArgs.NewCompletedFile(cabGroupedFiles.Key, fileToAddInCab.RelativePathInCab));
                                }
                            }
                            cfCabinet.Save(_compressionType);
                        } finally {
                            cfCabinet.OnProgress -= OnProgressionEvent;
                        }
                    }
                } catch (OperationCanceledException) {
                    throw;
                } catch (Exception e) {
                    throw new CabException($"Failed to delete files from {cabGroupedFiles.Key}.", e);
                }
                OnProgress?.Invoke(this, CabProgressionEventArgs.NewCompletedCabinet(cabGroupedFiles.Key));
            }
            return nbFilesProcessed;
        }
        
        /// <summary>
        /// Returns a string representation of a given cabinet file.
        /// </summary>
        /// <param name="cabPath"></param>
        /// <returns></returns>
        public string ToString(string cabPath) {
            using (var cfCabinet = new CfCabinet(cabPath, _cancelToken)) {
                return cfCabinet.GetStringFullRepresentation();
            }
        }

        private void OnProgressionEvent(object sender, CfSaveEventArgs e) {
            if (sender is CfCabinet cabinet) {
                OnProgress?.Invoke(this, CabProgressionEventArgs.NewProgress(cabinet.CabPath, e.RelativePathInCab, Math.Round(e.TotalBytesDone / (double) e.TotalBytesToProcess * 100, 2)));
            }
        }

        private FileAttributes GetFileAttributes(CfFileAttribs fileFileAttributes) {
            FileAttributes attr = 0;
            if (fileFileAttributes.HasFlag(CfFileAttribs.Hiddden)) {
                attr |= FileAttributes.Hidden;
            }
            if (fileFileAttributes.HasFlag(CfFileAttribs.Rdonly)) {
                attr |= FileAttributes.ReadOnly;
            }
            return attr;
        }
        
    }
}