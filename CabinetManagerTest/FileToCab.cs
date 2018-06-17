using CabinetManager;

namespace CabinetManagerTest {
    public class FileToCab : IFileToCab {
        public string SourcePath { get; set; }
        public string CabFilePath { get; set; }
        public string RelativePathInCab { get; set; }
    }
}