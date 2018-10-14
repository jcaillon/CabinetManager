using System;

namespace CabinetManager.core {
    internal class CfCabException : Exception {
        public CfCabException() { }
        public CfCabException(string message) : base(message) { }
        public CfCabException(string message, Exception innerException) : base(message, innerException) { }
    }
}