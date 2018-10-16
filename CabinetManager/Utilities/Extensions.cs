using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CabinetManager.Utilities {
    internal static class Extensions {

        public static string NormalizeRelativePath(this string path) {
            return path.Trim().Replace('/', '\\');
        }
        
        /// <summary>
        /// Read the next NULL terminated <see cref="string"/> of the given <paramref name="reader"/>.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="encoding"></param>
        /// <returns>Number of bytes read.</returns>
        public static string ReadNullTerminatedString(this BinaryReader reader, Encoding encoding = null) {
            encoding = encoding ?? Encoding.ASCII;
            var bytes = new List<byte>();
            do {
                var readByte = reader.ReadByte();
                if (readByte <= 0) {
                    break;
                }
                bytes.Add(readByte);
            } while (true);

            return encoding.GetString(bytes.ToArray());
        }
        
        /// <summary>
        /// Write the <see cref="string"/> as byte array in the stream using given encoding (default <see cref="Encoding.ASCII"/>) and ending the string with a NULL (0) byte.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="val"></param>
        /// <param name="encoding"></param>
        /// <returns>Number of bytes written.</returns>
        public static int WriteNullTerminatedString(this BinaryWriter writer, string val, Encoding encoding = null) {
            encoding = encoding ?? Encoding.ASCII;
            var bytes = encoding.GetBytes(val);
            writer.Write(bytes, 0, bytes.Length);
            writer.Write((byte) 0); // NULL ending string
            return bytes.Length + 1;
        }
    }
}