using System.IO;

namespace CabinetManager.core {

    /// <summary>
    /// Each <see cref="CfData"/> record describes some amount of compressed data.
    /// The first <see cref="CfData"/> entry for each folder is located using <see cref="CfFolder.FirstDataBlockOffset"/>.
    /// Subsequent <see cref="CfData"/> records for this folder are contiguous.
    /// In a standard cabinet all the <see cref="CfData"/> entries are contiguous and in the same order as the <see cref="CfFolder"/> entries that refer them.
    /// </summary>
    class CfData {

        public ushort MaxCompressedDataLength = ushort.MaxValue;

        private readonly CfFolder _parent;

        public CfData(CfFolder parent) {
            _parent = parent;
        }

        /// <summary>
        /// The number of byte needed for the data info (uint = 4, ushort = 2, byte = 1) (does not count the actual data size!)
        /// </summary>
        internal uint DataInfoLength => 4 + 2 + 2 + (uint) DataReservedArea.Length;

        /// <summary>
        /// Checksum of this <see cref="CfData"/> structure, from 0 to <see cref="CompressedDataLength"/>.
        /// May be set to zero if the checksum is not supplied.
        /// </summary>
        public uint CheckSum { get; set; }

        /// <summary>
        /// Number of bytes of compressed data in this <see cref="CfData"/> record. When <see cref="UncompressedDataLength"/> is zero,
        /// this field indicates only the number of bytes that fit into this cabinet file
        /// </summary>
        public ushort CompressedDataLength { get; set; }

        /// <summary>
        /// The uncompressed size of the data in this <see cref="CfData"/> entry. When this <see cref="CfData"/> entry is continued in the next cabinet file,
        /// <see cref="UncompressedDataLength"/> will be zero, and <see cref="UncompressedDataLength"/> in the first <see cref="CfData"/> entry in the next cabinet file will report
        /// the total uncompressed size of the data from both <see cref="CfData"/> blocks.
        /// </summary>
        public ushort UncompressedDataLength { get; set; }

        /// <summary>
        /// if <see cref="CfHeaderFlag.CfhdrReservePresent"/> is set in <see cref="CfCabinet.Flags"/> and <see cref="CfCabinet.DataReservedAreaSize"/> is non-zero,
        /// then this field contains per-datablock application information. This field is defined by the application and used for application-defined purposes.
        /// </summary>
        public byte[] DataReservedArea { get; set; }

        /// <summary>
        /// The compressed data bytes, compressed using the <see cref="CfFolder.CompressionType"/> method. When <see cref="UncompressedDataLength"/> is zero,
        /// these data bytes must be combined with the data bytes from the next cabinet's first <see cref="CfData"/> entry before decompression.
        /// When <see cref="CfFolder.CompressionType"/> indicates that the data is not compressed, this field contains the uncompressed data bytes.
        /// In this case, <see cref="CompressedDataLength"/> and <see cref="UncompressedDataLength"/> will be equal unless this <see cref="CfData"/> entry crosses a cabinet file boundary.
        /// </summary>
        public byte[] CompressedData { get; set; }
        
        public void WriteToStream(Stream stream) {
            stream.WriteAsByteArray(CheckSum);
            stream.WriteAsByteArray(CompressedDataLength);
            stream.WriteAsByteArray(UncompressedDataLength);
            if (DataReservedArea.Length > 0) {
                stream.Write(DataReservedArea, 0, DataReservedArea.Length);
            }
            //stream.Write(CompressedData, 0, CompressedData.Length);
        }
    }
}