using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CabinetManagerTest {
    public static class TestHelper {
        private static readonly string TestFolder = Path.Combine(AppContext.BaseDirectory, "Tests");
        private static bool? _isRuntimeWindowsPlatform;

        public static string GetTestFolder(string testName) {
            var path = Path.Combine(TestFolder, testName);
            Directory.CreateDirectory(path);
            return path;
        }
        
        /// <summary>
        /// Returns true if the current execution is done on windows platform
        /// </summary>
        public static bool IsRuntimeWindowsPlatform {
            get {
                return (_isRuntimeWindowsPlatform ?? (_isRuntimeWindowsPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows))).Value;
            }
        }
    }
}