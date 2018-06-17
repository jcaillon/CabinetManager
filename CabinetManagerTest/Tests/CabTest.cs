using System.Collections.Generic;
using System.IO;
using System.Linq;
using CabinetManager;
using CabinetManagerTest.Compression;
using CabinetManagerTest.Compression.Cab;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetManagerTest.Tests {
    [TestClass]
    public class CabTest {
        private static string _testFolder;
        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(CabTest)));

        [ClassInitialize]
        public static void Init(TestContext context) {
            Cleanup();
            Directory.CreateDirectory(TestFolder);
        }

        [ClassCleanup]
        public static void Cleanup() {
            // TODO : uncomment
            //if (Directory.Exists(TestFolder)) {
            //    Directory.Delete(TestFolder, true);
            //}
        }

        [TestMethod]
        public void CreateCab() {
            CreateCabWithWindowsLib("out.cab");
        }

        [TestMethod]
        public void ReadExistingCab() {
            Cab cab = new Cab();
            var details = cab.GetCabDetails(Path.Combine(TestFolder, "out.cab"));
            var list = cab.ListFiles(Path.Combine(TestFolder, "out.cab"));
            cab.ExtractFileSet(list
                .Select(f => new FileToExtractFromCab {
                    CabPath = f.CabPath,
                    RelativePathInCab = f.RelativePathInCab,
                    ToPath = Path.Combine(TestFolder, $"__{Path.GetFileName(f.RelativePathInCab)}")
                } as IFileToExtractFromCab)
                .ToList());
        }

        #region Windows Library

        private List<IFileToCab> CreateCabWithWindowsLib(string cabName) {
            List<IFileToCab> listFiles = TestHelper.GetPackageTestFilesList(TestFolder, cabName);
            TestHelper.CreateSourceFiles(listFiles);
            PackFileSet(listFiles, CompressionLevel.None);

            // verify
            foreach (var groupedFiles in listFiles.GroupBy(f => f.CabFilePath)) {
                var files = ListFiles(groupedFiles.Key);
                foreach (var file in files) {
                    Assert.IsTrue(groupedFiles.ToList().Exists(f => f.RelativePathInCab.Equals(file.RelativePathInCab)));
                }

                Assert.AreEqual(groupedFiles.ToList().Count, files.Count);
            }

            return listFiles;
        }

        private void PackFileSet(List<IFileToCab> files, CompressionLevel compressionLevel) {
            foreach (var cabGroupedFiles in files.GroupBy(f => f.CabFilePath)) {
                var cabInfo = new CabInfo(cabGroupedFiles.Key);
                var filesDic = cabGroupedFiles.ToDictionary(file => file.RelativePathInCab, file => file.SourcePath);
                cabInfo.PackFileSet(filesDic, compressionLevel, null);
            }
        }

        private List<IFileInCab> ListFiles(string archivePath) {
            return new CabInfo(archivePath)
                .GetFiles()
                .Select(info => new FileInCab {
                    RelativePathInCab = Path.Combine(info.Path, info.Name),
                    SizeInBytes = (ulong) info.Length,
                    LastWriteTime = info.LastWriteTime,
                    CabPath = archivePath
                } as IFileInCab)
                .ToList();
        }

        #endregion
    }
}