using System;

namespace CabinetManager.core.Exceptions {
    internal class CfCabFileMissingException : Exception {
        public CfCabFileMissingException(string message) : base(message) { }
    }
}