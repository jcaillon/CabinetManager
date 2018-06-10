using System;
using System.IO;
using System.Text;
using CabinetManager;
using CabinetManager.core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CabinetManagerTest {

    [TestClass]
    public class CabinetTest {

        private readonly string _tempFolder = Path.Combine(AppContext.BaseDirectory, "Temp");

        [TestMethod]
        public void TestMethod1() {

            var file1Path = Path.Combine(_tempFolder, "file1.txt");
            var file2Path = Path.Combine(_tempFolder, "file2.txt");

            File.WriteAllText(file1Path, "one", Encoding.Default);
            File.WriteAllText(file2Path, "two", Encoding.Default);

            ICabinet cab = new Cabinet();
            cab.AddFile("file1.txt", file1Path);
            cab.AddFile("folder1\\file1.txt", file1Path);
            cab.AddFile("folder1\\file2.txt", file2Path);
            cab.AddFile("folder1\\subfolder1\\file1.txt", file1Path);
            cab.Save(Path.Combine(_tempFolder, "out.cab"));

        }
    }
}