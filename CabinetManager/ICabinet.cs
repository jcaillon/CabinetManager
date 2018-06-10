using System;
using System.Collections.Generic;
using System.Text;

namespace CabinetManager {
    public interface ICabinet {

        void AddFile(string relativePathInCab, string absoluteFilePath);

        void Save(string cabFilePath);
    }
}