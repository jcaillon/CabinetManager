using System;

namespace CabinetManager {
    public class CabException : Exception {
        public CabException() { }
        public CabException(string message) : base(message) { }
        public CabException(string message, Exception innerException) : base(message, innerException) { }
    }
}