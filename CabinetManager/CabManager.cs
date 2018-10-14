﻿using System;
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
                    using (BinaryReader reader = new BinaryReader(File.OpenRead(cabGroupedFiles.Key))) {
                        var cfCabinet = new CfCabinet(cabGroupedFiles.Key, reader);
                        foreach (var fileToExtract in cabGroupedFiles) {
                            _cancelToken?.ThrowIfCancellationRequested();
                            try {
                                cfCabinet.ExtractToFile(reader, fileToExtract.RelativePathInCab, fileToExtract.ExtractionPath);
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

        public void DeleteFileSet(IEnumerable<IFileInCabToDelete> filesToDelete) {
            OnProgress?.Invoke(this, null);
            throw new NotImplementedException();
        }
        
        public string GetCabDetails(string archivePath) {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(archivePath))) {
                var cfCabinet = new CfCabinet(archivePath, reader);
                foreach (var cfFolder in cfCabinet.Folders) {
                    cfFolder.ReadDataHeader(reader);
                }
                return cfCabinet.ToString();
            }
        }

        
    }
}