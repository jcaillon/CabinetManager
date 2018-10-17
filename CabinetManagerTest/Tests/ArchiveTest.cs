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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using CabinetManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetManagerTest.Tests {
    
    public class ArchiveTest {

        private int _nbFileFinished;
        private int _nbArchiveFinished;
        
        protected void CreateArchive(ICabManager cabManager, List<FileInCab> listFiles) {
            cabManager.OnProgress += ArchiverOnOnProgress;
            
            _nbFileFinished = 0;
            _nbArchiveFinished = 0;

            var modifiedList = listFiles.GetRange(1, listFiles.Count - 1);
            
            // try to add a non existing file
            modifiedList.Add(new FileInCab {
                CabPath = listFiles.First().CabPath,
                ExtractionPath = listFiles.First().ExtractionPath,
                RelativePathInCab = "random.name"
            });
            Assert.AreEqual(modifiedList.Count - 1, cabManager.PackFileSet(modifiedList));
            
            // test the update of archives
            modifiedList = listFiles.GetRange(0, 1);
            Assert.AreEqual(modifiedList.Count, cabManager.PackFileSet(modifiedList));
 
            foreach (var archive in listFiles.GroupBy(f => f.CabPath)) {
                if (Directory.Exists(Path.GetDirectoryName(archive.Key))) {
                    Assert.IsTrue(File.Exists(archive.Key), $"The archive does not exist : {archive}");
                }
            }
            
            cabManager.OnProgress -= ArchiverOnOnProgress;
            Assert.AreEqual(listFiles.Count, _nbFileFinished, "Problem in the progress event");
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

        protected void Extract(ICabManager cabManager, List<FileInCab> listFiles) {
            cabManager.OnProgress += ArchiverOnOnProgress;
            _nbFileFinished = 0;
            _nbArchiveFinished = 0;
            
            // try to extract a non existing file
            var modifiedList = listFiles.ToList();
            modifiedList.Add(new FileInCab {
                CabPath = listFiles.First().CabPath,
                ExtractionPath = listFiles.First().ExtractionPath,
                RelativePathInCab = "random.name"
            });
            Assert.AreEqual(modifiedList.Count - 1, cabManager.ExtractFileSet(modifiedList));
            
            foreach (var fileToExtract in listFiles) {
                Assert.IsTrue(File.Exists(fileToExtract.ExtractionPath), $"Extracted file does not exist : {fileToExtract.ExtractionPath}");
                Assert.AreEqual(File.ReadAllText(fileToExtract.SourcePath), File.ReadAllText(fileToExtract.ExtractionPath), "Incoherent extracted file content");
            }
            
            cabManager.OnProgress -= ArchiverOnOnProgress;
            Assert.AreEqual(listFiles.Count, _nbFileFinished, "Problem in the progress event");
            Assert.AreEqual(listFiles.GroupBy(f => f.CabPath).Count(), _nbArchiveFinished, "Problem in the progress event, number of archives");
        }
        
        protected void DeleteFilesInArchive(ICabManager cabManager, List<FileInCab> listFiles) {
            cabManager.OnProgress += ArchiverOnOnProgress;
            _nbFileFinished = 0;
            _nbArchiveFinished = 0;

            // try to delete a non existing file
            var modifiedList = listFiles.ToList();
            modifiedList.Add(new FileInCab {
                CabPath = listFiles.First().CabPath,
                ExtractionPath = listFiles.First().ExtractionPath,
                RelativePathInCab = "random.name"
            });
            Assert.AreEqual(modifiedList.Count - 1, cabManager.DeleteFileSet(modifiedList));
            
            foreach (var groupedFiles in listFiles.GroupBy(f => f.CabPath)) {
                var files = cabManager.ListFiles(groupedFiles.Key);
                Assert.AreEqual(0, files.Count(), $"The archive is not empty : {groupedFiles.Key}");
            }
            
            cabManager.OnProgress -= ArchiverOnOnProgress;
            Assert.AreEqual(listFiles.Count, _nbFileFinished, "Problem in the progress event");
            Assert.AreEqual(listFiles.GroupBy(f => f.CabPath).Count(), _nbArchiveFinished, "Problem in the progress event, number of archives");
        }

        private void ArchiverOnOnProgress(object sender, ICabProgressionEventArgs e) {
            if (e.EventType == CabEventType.FileProcessed) {
                _nbFileFinished++;
            } else if (e.EventType == CabEventType.CabinetCompleted) {
                _nbArchiveFinished++;
            }
        }
        
        protected List<FileInCab> GetPackageTestFilesList(string testFolder, string cabPath) {
            var list = new List<FileInCab> {
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
            foreach (var file in list) {
                File.WriteAllText(file.SourcePath, $"\"{Path.GetFileName(file.SourcePath)}\"");
                if (File.Exists(file.ExtractionPath)) {
                    File.Delete(file.ExtractionPath);
                }
            }
            foreach (var cabGrouped in list.GroupBy(f => f.CabPath)) {
                if (File.Exists(cabGrouped.Key)) {
                    File.Delete(cabGrouped.Key);
                }
            }
            return list;
        }
        
    }
}