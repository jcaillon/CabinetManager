using System;
using System.Collections.Generic;
using System.IO;
using CabinetManager;

namespace CabinetManagerTest {
    public static class TestHelper {
        private static readonly string TestFolder = Path.Combine(AppContext.BaseDirectory, "Tests");

        public static string GetTestFolder(string testName) {
            var path = Path.Combine(TestFolder, testName);
            Directory.CreateDirectory(path);
            return path;
        }

        public static void CreateSourceFiles(List<IFileToCab> listFiles) {
            foreach (var file in listFiles) {
                File.WriteAllText(file.SourcePath, Path.GetFileName(file.SourcePath));
            }
        }

        public static List<IFileToCab> GetPackageTestFilesList(string testFolder, string outCab) {
            return new List<IFileToCab> {
                new FileToCab {
                    SourcePath = Path.Combine(testFolder, "file1.txt"),
                    CabFilePath = Path.Combine(testFolder, outCab),
                    RelativePathInCab = "file1.txt"
                },
                new FileToCab {
                    SourcePath = Path.Combine(testFolder, "file2.txt"),
                    CabFilePath = Path.Combine(testFolder, outCab),
                    RelativePathInCab = Path.Combine("subfolder1", "file2.txt")
                },
                new FileToCab {
                    SourcePath = Path.Combine(testFolder, "file1.txt"),
                    CabFilePath = Path.Combine(testFolder, $"_{outCab}"),
                    RelativePathInCab = Path.Combine("subfolder1", "bla", "file3.txt")
                }
            };
        }
    }
}