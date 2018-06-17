using CabinetManager;

namespace CabinetManagerTest {
    class FileToExtractFromCab : IFileToExtractFromCab {
        public string CabPath { get; set; }
        public string RelativePathInCab { get; set; }
        public string ToPath { get; set; }
    }
}