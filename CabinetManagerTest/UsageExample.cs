#region header

// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (Program.cs) is part of CabinetManagerTest.
// 
// CabinetManagerTest is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// CabinetManagerTest is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with CabinetManagerTest. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CabinetManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetManagerTest {
    
    [TestClass]
    public class UsageExample {
        
        [TestMethod]
        public void Main() {

            Console.WriteLine("Creating dumb file.");
            File.WriteAllText(@"my_source_file.txt", @"my content");
            
            var cabManager = CabManager.New();
            
            cabManager.SetCompressionLevel(CabCompressionLevel.None);
            cabManager.SetCancellationToken(null);
            cabManager.OnProgress += CabManagerOnProgress;

            // Add files to a new or existing cabinet
            Console.WriteLine("Adding file to cabinet.");
            var nbProcessed = cabManager.PackFileSet(new List<IFileToAddInCab> {
                CabFile.NewToPack(@"archive.cab", @"folder\file.txt", @"my_source_file.txt")
            });

            Console.WriteLine($" -> {nbProcessed} files were added to a cabinet.");

            // List all the files in a cabinet
            var filesInCab = cabManager.ListFiles(@"archive.cab").ToList();

            Console.WriteLine("Listing files:");
            foreach (var fileInCab in filesInCab) {
                Console.WriteLine($" * {fileInCab.RelativePathInCab}: {fileInCab.LastWriteTime}, {fileInCab.SizeInBytes}, {fileInCab.FileAttributes}");
            }

            // Extract files to external paths
            Console.WriteLine("Extract file from cabinet.");
            nbProcessed = cabManager.ExtractFileSet(new List<IFileInCabToExtract> {
                CabFile.NewToExtract(@"archive.cab", @"folder\file.txt", @"extraction_path.txt")
            });

            Console.WriteLine($" -> {nbProcessed} files were extracted from a cabinet.");
            
            // Move files within a cabinet
            var filesToMove = filesInCab.Select(f => CabFile.NewToMove(f.CabPath, f.RelativePathInCab, $"subfolder/{f.RelativePathInCab}")).ToList();
            Console.WriteLine("Moving files within a cabinet.");
            nbProcessed = cabManager.MoveFileSet(filesToMove);

            Console.WriteLine($" -> {nbProcessed} files were moved within the cabinet.");

            Console.WriteLine("Listing files that were processed.");
            foreach (var file in filesToMove.Where(f => f.Processed)) {
                Console.WriteLine($" * {file.RelativePathInCab}");
            }

            // Delete files in a cabinet
            Console.WriteLine("Delete file in cabinet.");
            nbProcessed = cabManager.DeleteFileSet(filesToMove.Select(f => CabFile.NewToDelete(f.CabPath, f.NewRelativePathInCab)));

            Console.WriteLine($" -> {nbProcessed} files were deleted from a cabinet.");

        }

        private static void CabManagerOnProgress(object sender, ICabProgressionEventArgs e) {
            switch (e.EventType) {
                case CabEventType.GlobalProgression:
                    Console.WriteLine($"Global progression : {e.PercentageDone}%, current file is {e.RelativePathInCab}");
                    break;
                case CabEventType.FileProcessed:
                    Console.WriteLine($"New file processed : {e.RelativePathInCab}");
                    break;
                case CabEventType.CabinetCompleted:
                    Console.WriteLine($"New cabinet completed : {e.CabPath}");
                    break;
            }
        }

        private class CabFile : IFileInCabToDelete, IFileInCabToExtract, IFileToAddInCab, IFileInCabToMove {
            
            public string CabPath { get; private set; }
            public string RelativePathInCab { get; private set; }
            public bool Processed { get; set; }
            public string ExtractionPath { get; private set; }
            public string SourcePath { get; private set; }
            public string NewRelativePathInCab { get; private set; }

            public static CabFile NewToPack(string cabPath, string relativePathInCab, string sourcePath) {
                return new CabFile {
                    CabPath = cabPath,
                    RelativePathInCab = relativePathInCab,
                    SourcePath = sourcePath
                };
            }

            public static CabFile NewToExtract(string cabPath, string relativePathInCab, string extractionPath) {
                return new CabFile {
                    CabPath = cabPath,
                    RelativePathInCab = relativePathInCab,
                    ExtractionPath = extractionPath
                };
            }

            public static CabFile NewToDelete(string cabPath, string relativePathInCab) {
                return new CabFile {
                    CabPath = cabPath,
                    RelativePathInCab = relativePathInCab
                };
            }

            public static CabFile NewToMove(string cabPath, string relativePathInCab, string newRelativePathInCab) {
                return new CabFile {
                    CabPath = cabPath,
                    RelativePathInCab = relativePathInCab,
                    NewRelativePathInCab = newRelativePathInCab
                };
            }
        }
    }
}