using System;
using System.IO;
using System.Text;

namespace CabinetManager.core {
    static class Extensions {

        /// <summary>
        /// Write the <see cref="uint"/> as byte array in the stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="val"></param>
        public static void WriteAsByteArray(this Stream stream, uint val) {
            var bytes = BitConverter.GetBytes(val);
            stream.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Write the <see cref="ushort"/> as byte array in the stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="val"></param>
        public static void WriteAsByteArray(this Stream stream, ushort val) {
            stream.WriteAsByteArray((uint) val);
        }

        /// <summary>
        /// Write the <see cref="string"/> as byte array in the stream using given encoding (default <see cref="Encoding.ASCII"/>) and ending the string with a NULL (0) byte
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="val"></param>
        /// <param name="encoding"></param>
        public static void WriteAsByteArray(this Stream stream, string val, Encoding encoding = null) {
            encoding = encoding ?? Encoding.ASCII;
            var bytes = encoding.GetBytes(val);
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteByte(0); // NULL ending string
        }

    }
}