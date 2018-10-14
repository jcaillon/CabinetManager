﻿using System;

namespace CabinetManager {
    class FileInCab : IFileInCab {
        public string CabPath { get; set; }
        public string RelativePathInCab { get; set; }
        public ulong SizeInBytes { get; set; }
        public DateTime LastWriteTime { get; set; }
    }
}