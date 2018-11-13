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
            return DoAction(filesToPackIn, Action.Archive);
        }

        /// <inheritdoc cref="ICabManager.ListFiles"/>
        public IEnumerable<IFileInCab> ListFiles(string cabPath) {
            if (!File.Exists(cabPath)) {
                return Enumerable.Empty<IFileInCab>();
            }
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
            return DoAction(filesToExtract, Action.Extract);
        }

        /// <inheritdoc cref="ICabManager.DeleteFileSet"/>
        public int DeleteFileSet(IEnumerable<IFileInCabToDelete> filesToDeleteIn) {
            return DoAction(filesToDeleteIn, Action.Delete);
        }

        /// <inheritdoc cref="ICabManager.MoveFileSet"/>
        public int MoveFileSet(IEnumerable<IFileInCabToMove> filesToMove) {
            return DoAction(filesToMove, Action.Move);
        }

        /// <summary>
        /// Returns a string representation of a given cabinet file.
        /// </summary>
        /// <param name="cabPath"></param>
        /// <returns></returns>
        public string ToString(string cabPath) {
            if (!File.Exists(cabPath)) {
                return string.Empty;
            }
            using (var cfCabinet = new CfCabinet(cabPath, _cancelToken)) {
                return cfCabinet.GetStringFullRepresentation();
            }
        }
        
        private int DoAction(IEnumerable<IFileCabBase> filesIn, Action action) {
            var files = filesIn.ToList();
            files.ForEach(f => f.Processed = false);
            
            int nbFilesProcessed = 0;
            foreach (var groupedFiles in files.GroupBy(f => f.CabPath)) {
                if (action != Action.Archive && !File.Exists(groupedFiles.Key)) {
                    continue;
                }
                try {
                    if (action == Action.Extract) {
                        // create all necessary extraction folders
                        foreach (var extractDirGroupedFiles in groupedFiles.GroupBy(f => Path.GetDirectoryName(((IFileInCabToExtract) f).ExtractionPath))) {
                            if (!Directory.Exists(extractDirGroupedFiles.Key) && !string.IsNullOrWhiteSpace(extractDirGroupedFiles.Key)) {
                                Directory.CreateDirectory(extractDirGroupedFiles.Key);
                            }
                        }
                    }
                    using (var cfCabinet = new CfCabinet(groupedFiles.Key, _cancelToken)) {
                        cfCabinet.OnProgress += OnProgressionEvent;
                        try {
                            foreach (var file in groupedFiles) {
                                var fileRelativePath = file.RelativePathInCab.NormalizeRelativePath();
                                switch (action) {
                                    case Action.Archive:
                                        var fileToArchive = (IFileToAddInCab) file;
                                        if (File.Exists(fileToArchive.SourcePath)) {
                                            cfCabinet.AddExternalFile(fileToArchive.SourcePath, fileRelativePath);
                                            nbFilesProcessed++;
                                            file.Processed = true;
                                            OnProgress?.Invoke(this, CabProgressionEventArgs.NewProcessedFile(groupedFiles.Key, fileRelativePath));
                                        }
                                        break;
                                    case Action.Extract:
                                        if (cfCabinet.ExtractToFile(fileRelativePath, ((IFileInCabToExtract) file).ExtractionPath)) {
                                            nbFilesProcessed++;
                                            file.Processed = true;
                                            OnProgress?.Invoke(this, CabProgressionEventArgs.NewProcessedFile(groupedFiles.Key, fileRelativePath));
                                        }
                                        break;
                                    case Action.Delete:
                                        if (cfCabinet.DeleteFile(fileRelativePath)) {
                                            nbFilesProcessed++;
                                            file.Processed = true;
                                            OnProgress?.Invoke(this, CabProgressionEventArgs.NewProcessedFile(groupedFiles.Key, fileRelativePath));
                                        }
                                        break;
                                    case Action.Move:
                                        if (cfCabinet.MoveFile(fileRelativePath, ((IFileInCabToMove) file).NewRelativePathInCab.NormalizeRelativePath())) {
                                            nbFilesProcessed++;
                                            file.Processed = true;
                                            OnProgress?.Invoke(this, CabProgressionEventArgs.NewProcessedFile(groupedFiles.Key, fileRelativePath));
                                        }
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException(nameof(action), action, null);
                                }
                            }
                            if (action != Action.Extract) {
                                cfCabinet.Save(_compressionType);
                            }
                        } finally {
                            cfCabinet.OnProgress -= OnProgressionEvent;
                        }
                    }
                } catch (OperationCanceledException) {
                    throw;
                } catch (Exception e) {
                    throw new CabException($"Failed to {action} files in {groupedFiles.Key}.", e);
                }
                OnProgress?.Invoke(this, CabProgressionEventArgs.NewCompletedCabinet(groupedFiles.Key));
            }
            return nbFilesProcessed;
        }
        
        private enum Action {
            Archive,
            Extract,
            Delete,
            Move
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