using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CabinetManager.Utilities {
    static class Extensions {
        /// <summary>
        /// Read the next <see cref="uint"/> <see cref="val"/> of the given <see cref="Stream"/>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="val"></param>
        /// <returns>Number of bytes read</returns>
        public static int ReadAsByteArray(this Stream stream, out uint val) {
            var bytes = new byte[4]; // uint -> 4 bytes
            var readBytesCount = stream.Read(bytes, 0, bytes.Length);
            val = BitConverter.ToUInt32(bytes, 0);
            return readBytesCount;
        }

        /// <summary>
        /// Read the next <see cref="ushort"/> <see cref="val"/> of the given <see cref="Stream"/>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="val"></param>
        /// <returns>Number of bytes read</returns>
        public static int ReadAsByteArray(this Stream stream, out ushort val) {
            var bytes = new byte[2]; // ushort -> 2 bytes
            var readBytesCount = stream.Read(bytes, 0, bytes.Length);
            val = BitConverter.ToUInt16(bytes, 0);
            return readBytesCount;
        }

        /// <summary>
        /// Read the next NULL terminated <see cref="string"/> <see cref="val"/> of the given <see cref="Stream"/>
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="val"></param>
        /// <param name="encoding"></param>
        /// <returns>Number of bytes read</returns>
        public static int ReadAsByteArray(this Stream stream, out string val, Encoding encoding = null) {
            encoding = encoding ?? Encoding.ASCII;
            var readBytesCount = 0;
            var bytes = new List<byte>();
            do {
                var readByte = stream.ReadByte();
                if (readByte <= 0) {
                    if (readByte == 0) {
                        readBytesCount++; // final NULL byte
                    }

                    break;
                }

                bytes.Add((byte) readByte);
                readBytesCount++;
            } while (true);

            val = encoding.GetString(bytes.ToArray());
            return readBytesCount;
        }

        /// <summary>
        /// Write the <see cref="uint"/> as byte array in the stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="val"></param>
        /// <returns>Number of bytes writted</returns>
        public static int WriteAsByteArray(this Stream stream, uint val) {
            var bytes = BitConverter.GetBytes(val);
            stream.Write(bytes, 0, bytes.Length);
            return bytes.Length;
        }

        /// <summary>
        /// Write the <see cref="ushort"/> as byte array in the stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="val"></param>
        /// <returns>Number of bytes writted</returns>
        public static int WriteAsByteArray(this Stream stream, ushort val) {
            var bytes = BitConverter.GetBytes(val);
            stream.Write(bytes, 0, bytes.Length);
            return bytes.Length;
        }

        /// <summary>
        /// Write the <see cref="string"/> as byte array in the stream using given encoding (default <see cref="Encoding.ASCII"/>) and ending the string with a NULL (0) byte
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="val"></param>
        /// <param name="encoding"></param>
        /// <returns>Number of bytes writted</returns>
        public static int WriteAsByteArray(this Stream stream, string val, Encoding encoding = null) {
            encoding = encoding ?? Encoding.ASCII;
            var bytes = encoding.GetBytes(val);
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteByte(0); // NULL ending string
            return bytes.Length + 1;
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
            writer.Write(0); // NULL ending string
            return bytes.Length + 1;
        }
    }
}