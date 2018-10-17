using System.Collections.Generic;
using System.IO;
using System.Linq;
using CabinetManager;
using CabinetManagerTest.Compression;
using CabinetManagerTest.Compression.Cab;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetManagerTest.Tests {
    
    [TestClass]
    public class CabTest : ArchiveTest {
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
        public void ClassicTest() {
            var archiver = CabManager.New();
            var listFiles = GetPackageTestFilesList(TestFolder, Path.Combine(TestFolder, "archives", "test1.cab"));
            listFiles.AddRange(GetPackageTestFilesList(TestFolder, Path.Combine(TestFolder, "archives", "test2.cab")));
            
            CreateArchive(archiver, listFiles);

            // verify
            ListArchive(archiver, listFiles);
            
            // extract
            Extract(archiver, listFiles);
            
            // delete files
            DeleteFilesInArchive(archiver, listFiles);
            
            // now with bigger files
            listFiles = GetPackageTestFilesList(TestFolder, Path.Combine(TestFolder, "archives", "test1.cab"));
            listFiles.AddRange(GetPackageTestFilesList(TestFolder, Path.Combine(TestFolder, "archives", "test2.cab")));
            
            byte[] fileBuffer = Enumerable.Repeat((byte) 42, 1000).ToArray();
            foreach (var file in listFiles) {
                using (Stream sourceStream = File.OpenWrite(file.SourcePath)) {
                    for (int i = 0; i < 2000; i++) {
                        sourceStream.Write(fileBuffer, 0, fileBuffer.Length);
                    }
                }
            }
            
            CreateArchive(archiver, listFiles);

            // verify
            ListArchive(archiver, listFiles);
            
            // extract
            Extract(archiver, listFiles);
            
            // delete files
            DeleteFilesInArchive(archiver, listFiles);

            CompareWithCabinetsProducedByWinApi();
        }

        private void CompareWithCabinetsProducedByWinApi() {
            if (!TestHelper.IsRuntimeWindowsPlatform) {
                return;
            }

            var windowsLibListFiles = GetPackageTestFilesList(TestFolder, Path.Combine(TestFolder, "archives", "win_test1.cab"));
            InitWithWindowsLib(windowsLibListFiles, CompressionLevel.None);
            
            var cabManager = CabManager.New();

            // verify
            ListArchive(cabManager, windowsLibListFiles);
            
            // extract
            Extract(cabManager, windowsLibListFiles);
            
            // delete files
            DeleteFilesInArchive(cabManager, windowsLibListFiles);
            
            var listFiles = GetPackageTestFilesList(TestFolder, Path.Combine(TestFolder, "archives", "test1.cab"));
            listFiles.AddRange(GetPackageTestFilesList(TestFolder, Path.Combine(TestFolder, "archives", "test2.cab")));

            CreateArchive(cabManager, listFiles);
            
            VerifyCabFilesWithWindowsLib(listFiles);
        }

        private void InitWithWindowsLib(List<FileInCab> windowsLibListFiles, CompressionLevel level) {

            foreach (var cabGroupedFiles in windowsLibListFiles.GroupBy(f => f.CabPath)) {
                if (!Directory.Exists(cabGroupedFiles.Key)) {
                    Directory.CreateDirectory(Path.GetDirectoryName(cabGroupedFiles.Key));
                }
            }
            
            // create .cab
            foreach (var cabGroupedFiles in windowsLibListFiles.GroupBy(f => f.CabPath)) {
                var cabInfo = new CabInfo(cabGroupedFiles.Key);
                var filesDic = cabGroupedFiles.ToDictionary(file => file.RelativePathInCab, file => file.SourcePath);
                cabInfo.PackFileSet(filesDic, level, null);
            }

            VerifyCabFilesWithWindowsLib(windowsLibListFiles);
        }

        private void VerifyCabFilesWithWindowsLib(List<FileInCab> windowsLibListFiles) {
            var actualFilesList = new List<FileInCab>();
            foreach (var cabGroupedFiles in windowsLibListFiles.GroupBy(f => f.CabPath)) {
                actualFilesList.AddRange(
                    new CabInfo(cabGroupedFiles.Key)
                        .GetFiles()
                        .Select(info => new FileInCab {
                            RelativePathInCab = Path.Combine(info.Path, info.Name),
                            SizeInBytes = (ulong) info.Length,
                            LastWriteTime = info.LastWriteTime,
                            CabPath = cabGroupedFiles.Key
                        })
                );
            }

            // verify
            foreach (var groupedTheoreticalFiles in windowsLibListFiles.GroupBy(f => f.CabPath)) {
                var actualFiles = actualFilesList.Where(f => f.CabPath.Equals(groupedTheoreticalFiles.Key)).ToList();
                foreach (var theoreticalFile in groupedTheoreticalFiles) {
                    Assert.IsTrue(actualFiles.ToList().Exists(f => f.RelativePathInCab.Replace("/", "\\").Equals(theoreticalFile.RelativePathInCab)), $"Can't find file in list : {theoreticalFile.RelativePathInCab}");
                }
                Assert.AreEqual(groupedTheoreticalFiles.Count(), actualFiles.Count, $"Wrong number of files listed : {groupedTheoreticalFiles.Count()}!={actualFiles.Count}");
            }
        }

    }
}