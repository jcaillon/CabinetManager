# Cabinet manager

Handles microsoft cabinet format (.cab files) in pure C#.

[![logo](logo.png)](https://github.com/jcaillon/CabinetManager)

## About

### Microsoft cabinet file specifications

This library implements the following specifications:

[https://msdn.microsoft.com/en-us/library/bb417343.aspx#cabinet_format](https://msdn.microsoft.com/en-us/library/bb417343.aspx#cabinet_format)

### Limitations of this lib

This library is not a complete implementation of the specifications (mostly because I didn't need all the features).

The limitations are listed below:

- Does not handle multi-cabinet file (a single cabinet can hold up to ~2Gb of data)
- Does not handle compressed data, you can only store/retrieve uncompressed data
- Does not compute nor verify checksums

### Usage example

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CabinetManager;

namespace CabinetManagerTest {
    
    public class Program {
        
        static void Main(string[] args) {

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

            // Delete files in a cabinet
            Console.WriteLine("Delete file in cabinet.");
            nbProcessed = cabManager.DeleteFileSet(filesInCab.Select(f => CabFile.NewToDelete(f.CabPath, f.RelativePathInCab)));

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

        private class CabFile : IFileInCabToDelete, IFileInCabToExtract, IFileToAddInCab {
            
            public string CabPath { get; private set; }
            public string RelativePathInCab { get; private set; }
            public string ExtractionPath { get; private set; }
            public string SourcePath { get; private set; }

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
            
        }
    }
}
```
