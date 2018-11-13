#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (IArchiveTest.cs) is part of Oetools.Utilities.Test.
// 
// Oetools.Utilities.Test is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Oetools.Utilities.Test is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Oetools.Utilities.Test. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CabinetManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetManagerTest.Tests {
    
    public class ArchiveTest {

        private int _nbFileProcessed;
        private int _nbArchiveFinished;
        private bool _hasReceivedGlobalProgression;
        private CancellationTokenSource _cancelSource;
        
        protected void CreateArchive(ICabManager archiver, List<FileInCab> listFiles) {
            archiver.OnProgress += ArchiverOnOnProgress;
            
            _nbFileProcessed = 0;
            _nbArchiveFinished = 0;

            var modifiedList = listFiles.GetRange(1, listFiles.Count - 1);
            
            // Test the cancellation.
            _cancelSource = new CancellationTokenSource();
            archiver.SetCancellationToken(_cancelSource.Token);
            var list = modifiedList;
            Assert.ThrowsException<OperationCanceledException>(() => archiver.PackFileSet(list));
            Assert.IsTrue(_nbArchiveFinished == 0, "Nothing was done again.");
            _nbFileProcessed = 0;
            _cancelSource = null;
            archiver.SetCancellationToken(null);

            _nbFileProcessed = 0;
            CleanupArchives(listFiles);
            
            // try to add a non existing file
            modifiedList.Add(new FileInCab {
                CabPath = listFiles.First().CabPath,
                ExtractionPath = listFiles.First().ExtractionPath,
                RelativePathInCab = "random.name"
            });
            Assert.AreEqual(modifiedList.Count - 1, archiver.PackFileSet(modifiedList));
            Assert.AreEqual(modifiedList.Count - 1, modifiedList.Count(f => f.Processed));
            
            // test the update of archives
            modifiedList = listFiles.GetRange(0, 1);
            Assert.AreEqual(modifiedList.Count, archiver.PackFileSet(modifiedList));
            Assert.AreEqual(modifiedList.Count, modifiedList.Count(f => f.Processed));

            foreach (var archive in listFiles.GroupBy(f => f.CabPath)) {
                if (Directory.Exists(Path.GetDirectoryName(archive.Key))) {
                    Assert.IsTrue(File.Exists(archive.Key), $"The archive does not exist : {archive}");
                }
            }

            archiver.OnProgress -= ArchiverOnOnProgress;
            
            // check progress
            Assert.IsTrue(_hasReceivedGlobalProgression, "Should have received a progress event.");
            Assert.AreEqual(listFiles.Count, _nbFileProcessed, "Problem in the progress event");
            Assert.AreEqual(listFiles.GroupBy(f => f.CabPath).Count() + 1, _nbArchiveFinished, "Problem in the progress event, number of archives");
        }

        protected void ListArchive(ICabManager cabManager, List<FileInCab> listFiles) {
            foreach (var groupedTheoreticalFiles in listFiles.GroupBy(f => f.CabPath)) {
                var actualFiles = cabManager.ListFiles(groupedTheoreticalFiles.Key).ToList();
                foreach (var theoreticalFile in groupedTheoreticalFiles) {
                    Assert.IsTrue(actualFiles.ToList().Exists(f => f.RelativePathInCab.Replace("/", "\\").Equals(theoreticalFile.RelativePathInCab)), $"Can't find file in list : {theoreticalFile.RelativePathInCab}");
                }
                Assert.AreEqual(groupedTheoreticalFiles.Count(), actualFiles.Count, $"Wrong number of files listed : {groupedTheoreticalFiles.Count()}!={actualFiles.Count}");
            }
        }

        protected void Extract(ICabManager archiver, List<FileInCab> listFiles) {
            archiver.OnProgress += ArchiverOnOnProgress;
            _nbFileProcessed = 0;
            _nbArchiveFinished = 0;

            var modifiedList = listFiles.ToList();
            
            // Test the cancellation.
            _cancelSource = new CancellationTokenSource();
            archiver.SetCancellationToken(_cancelSource.Token);
            var list = modifiedList;
            Assert.ThrowsException<OperationCanceledException>(() => archiver.ExtractFileSet(list));
            Assert.IsTrue(_nbArchiveFinished == 0, "Nothing was done again.");
            _nbFileProcessed = 0;
            _cancelSource = null;
            archiver.SetCancellationToken(null);
            
            // try to extract a non existing file
            modifiedList.Add(new FileInCab {
                CabPath = listFiles.First().CabPath,
                ExtractionPath = listFiles.First().ExtractionPath,
                RelativePathInCab = "random.name"
            });
            Assert.AreEqual(modifiedList.Count - 1, archiver.ExtractFileSet(modifiedList));
            Assert.AreEqual(modifiedList.Count - 1, modifiedList.Count(f => f.Processed));
            
            foreach (var fileToExtract in listFiles) {
                Assert.IsTrue(File.Exists(fileToExtract.ExtractionPath), $"Extracted file does not exist : {fileToExtract.ExtractionPath}");
                Assert.AreEqual(File.ReadAllText(fileToExtract.SourcePath), File.ReadAllText(fileToExtract.ExtractionPath), "Incoherent extracted file content");
            }
            
            archiver.OnProgress -= ArchiverOnOnProgress;
            
            // check progress
            Assert.IsTrue(_hasReceivedGlobalProgression, "Should have received a progress event.");
            Assert.AreEqual(listFiles.Count, _nbFileProcessed, "Problem in the progress event");
            Assert.AreEqual(listFiles.GroupBy(f => f.CabPath).Count(), _nbArchiveFinished, "Problem in the progress event, number of archives");
        }
        
        protected void DeleteFilesInArchive(ICabManager archiver, List<FileInCab> listFiles) {
            archiver.OnProgress += ArchiverOnOnProgress;
            _nbFileProcessed = 0;
            _nbArchiveFinished = 0;
            
            var modifiedList = listFiles.ToList();

            // Test the cancellation.
            _cancelSource = new CancellationTokenSource();
            archiver.SetCancellationToken(_cancelSource.Token);
            var list = modifiedList;
            Assert.ThrowsException<OperationCanceledException>(() => archiver.DeleteFileSet(list));
            Assert.IsTrue(_nbArchiveFinished == 0, "Nothing was done again.");
            _nbFileProcessed = 0;
            _cancelSource = null;
            archiver.SetCancellationToken(null);
            
            // try to delete a non existing file
            modifiedList.Add(new FileInCab {
                CabPath = listFiles.First().CabPath,
                ExtractionPath = listFiles.First().ExtractionPath,
                RelativePathInCab = "random.name"
            });
            Assert.AreEqual(modifiedList.Count - 1, archiver.DeleteFileSet(modifiedList));
            Assert.AreEqual(modifiedList.Count - 1, modifiedList.Count(f => f.Processed));
            
            foreach (var groupedFiles in listFiles.GroupBy(f => f.CabPath)) {
                var files = archiver.ListFiles(groupedFiles.Key);
                Assert.AreEqual(0, files.Count(), $"The archive is not empty : {groupedFiles.Key}");
            }
            
            archiver.OnProgress -= ArchiverOnOnProgress;
            
            // check progress
            Assert.IsTrue(_hasReceivedGlobalProgression, "Should have received a progress event.");
            Assert.AreEqual(listFiles.Count, _nbFileProcessed, "Problem in the progress event");
            Assert.AreEqual(listFiles.GroupBy(f => f.CabPath).Count(), _nbArchiveFinished, "Problem in the progress event, number of archives");
        }
        
        protected void MoveInArchives(ICabManager archiver, List<FileInCab> listFiles) {
            archiver.OnProgress += ArchiverOnOnProgress;
            _nbFileProcessed = 0;
            _nbArchiveFinished = 0;
            
            
            var modifiedList = listFiles.ToList();
            modifiedList.Add(new FileInCab {
                CabPath = listFiles.First().CabPath,
                ExtractionPath = listFiles.First().ExtractionPath,
                RelativePathInCab = "random.name"
            });
            modifiedList.ForEach(f => f.NewRelativePathInCab = $"{f.RelativePathInCab}_move");
            
            // Test the cancellation.
            _cancelSource = new CancellationTokenSource();
            archiver.SetCancellationToken(_cancelSource.Token);
            var list = modifiedList;
            Assert.ThrowsException<OperationCanceledException>(() => archiver.MoveFileSet(list));
            Assert.IsTrue(_nbArchiveFinished == 0, "Nothing was done again.");
            _nbFileProcessed = 0;
            _cancelSource = null;
            archiver.SetCancellationToken(null);

            Assert.AreEqual(modifiedList.Count - 1, archiver.MoveFileSet(modifiedList));
            Assert.AreEqual(modifiedList.Count - 1, modifiedList.Count(f => f.Processed));

            archiver.OnProgress -= ArchiverOnOnProgress;
            
            // check progress
            Assert.IsTrue(_hasReceivedGlobalProgression, "Should have received a progress event.");
            Assert.AreEqual(listFiles.Count, _nbFileProcessed, "Problem in the progress event");
            Assert.AreEqual(listFiles.GroupBy(f => f.CabPath).Count(), _nbArchiveFinished, "Problem in the progress event, number of archives");
            
            // move them back
            modifiedList.ForEach(f => {
                f.RelativePathInCab = f.NewRelativePathInCab;
                f.NewRelativePathInCab = f.NewRelativePathInCab.Substring(0, f.NewRelativePathInCab.Length - 5);
            });
            
            Assert.AreEqual(modifiedList.Count - 1, archiver.MoveFileSet(modifiedList));
            Assert.AreEqual(modifiedList.Count - 1, modifiedList.Count(f => f.Processed));
            
            modifiedList.ForEach(f => {
                f.RelativePathInCab = f.NewRelativePathInCab;
            });
        }

        private void ArchiverOnOnProgress(object sender, ICabProgressionEventArgs e) {
            switch (e.EventType) {
                case CabEventType.GlobalProgression:
                    if (e.PercentageDone < 0 || e.PercentageDone > 100) {
                        throw new Exception($"Wrong value for percentage done : {e.PercentageDone}%.");
                    }
                    _hasReceivedGlobalProgression = true;
                    _cancelSource?.Cancel();
                    break;
                case CabEventType.FileProcessed:
                    _nbFileProcessed++;
                    break;
                case CabEventType.CabinetCompleted:
                    _nbArchiveFinished++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        protected List<FileInCab> GetPackageTestFilesList(string testFolder, string cabPath) {
            var outputList = new List<FileInCab> {
                new FileInCab {
                    SourcePath = Path.Combine(testFolder, "file 0.txt"),
                    CabPath = cabPath,
                    RelativePathInCab = "file 0.txt",
                    ExtractionPath = Path.Combine(testFolder, "extract", Path.GetFileName(cabPath) ?? "", "file 0.txt")
                },
                new FileInCab {
                    SourcePath = Path.Combine(testFolder, "file1.txt"),
                    CabPath = cabPath,
                    RelativePathInCab = "file1.txt",
                    ExtractionPath = Path.Combine(testFolder, "extract", Path.GetFileName(cabPath) ?? "", "file1.txt")
                },
                new FileInCab {
                    SourcePath = Path.Combine(testFolder, "file2.txt"),
                    CabPath = cabPath,
                    RelativePathInCab = Path.Combine("subfolder1", "file2.txt"),
                    ExtractionPath = Path.Combine(testFolder, "extract", Path.GetFileName(cabPath) ?? "", "subfolder1", "file2.txt")
                },
                new FileInCab {
                    SourcePath = Path.Combine(testFolder, "file3.txt"),
                    CabPath = cabPath,
                    RelativePathInCab = Path.Combine("subfolder1", "bla bla", "file3.txt"),
                    ExtractionPath = Path.Combine(testFolder, "extract", Path.GetFileName(cabPath) ?? "", "subfolder1", "bla bla", "file3.txt")
                }
            };
            
            byte[] fileBuffer = Enumerable.Repeat((byte) 42, 1000).ToArray();
            foreach (var file in outputList) {
                using (Stream sourceStream = File.OpenWrite(file.SourcePath)) {
                    for (int i = 0; i < 2000; i++) {
                        sourceStream.Write(fileBuffer, 0, fileBuffer.Length);
                    }
                }
            }

            CleanupExtractedFiles(outputList);
            CleanupArchives(outputList);
            
            return outputList;
        }
        
        protected void CleanupExtractedFiles(List<FileInCab> fileList) {
            foreach (var file in fileList) {
                if (File.Exists(file.ExtractionPath)) {
                    File.Delete(file.ExtractionPath);
                }
            }
        }
        
        protected void CleanupArchives(List<FileInCab> fileList) {
            foreach (var cabGrouped in fileList.GroupBy(f => f.CabPath)) {
                if (File.Exists(cabGrouped.Key)) {
                    File.Delete(cabGrouped.Key);
                }
            }
        }
        
    }
}