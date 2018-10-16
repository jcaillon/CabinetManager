using System;

namespace CabinetManager.core.Exceptions {
    internal class CfCabException : Exception {
        public CfCabException(string message) : base(message) { }
        public CfCabException(string message, Exception innerException) : base(message, innerException) { }
    }
}