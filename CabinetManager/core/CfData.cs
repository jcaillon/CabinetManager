using System.IO;
using System.Text;
using CabinetManager.Utilities;

namespace CabinetManager.core {
    /// <summary>
    /// Each <see cref="CfData"/> record describes some amount of compressed data.
    /// The first <see cref="CfData"/> entry for each folder is located using <see cref="CfFolder.FirstDataBlockOffset"/>.
    /// Subsequent <see cref="CfData"/> records for this folder are contiguous.
    /// In a standard cabinet all the <see cref="CfData"/> entries are contiguous and in the same order as the <see cref="CfFolder"/> entries that refer them.
    /// </summary>
    class CfData {
        public const ushort MaxCompressedDataLength = ushort.MaxValue;

        private readonly CfFolder _parent;

        public CfData(CfFolder parent) {
            _parent = parent;
        }

        /// <summary>
        /// The number of byte needed for the data header (sizeof(uint) = 4, sizeof(ushort) = 2, sizeof(byte) = 1) (does not count the actual data size!)
        /// </summary>
        internal uint DataHeaderLength => 8 + (uint) (DataReservedArea?.Length ?? 0); // 4+2+2

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
        private byte[] CompressedData { get; set; }

        /// <summary>
        /// Offset at which to read this <see cref="CompressedData"/>
        /// </summary>
        public uint CompressedDataOffset { get; private set; }

        /// <summary>
        /// This data contains the bytes that are positionned at <see cref="UncompressedDataOffset"/> in the uncompressed stream
        /// </summary>
        public long UncompressedDataOffset { get; set; }

        /// <summary>
        /// Stream position at which we can write this <see cref="CfData"/> header
        /// </summary>
        private long HeaderStreamPosition { get; set; }

        /// <summary>
        /// Returns the uncompressed bytes for this <see cref="CfData"/>
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public byte[] GetUncompressedData(Stream stream) {
            stream.Position = CompressedDataOffset;
            CompressedData = new byte[CompressedDataLength];
            stream.Read(CompressedData, 0, CompressedData.Length);
            byte[] uncompressedData = UncompressData(CompressedData);
            CompressedData = null;
            return uncompressedData;
        }

        /// <summary>
        /// Write this instance of <see cref="CfData"/> to a stream
        /// </summary>
        public void WriteHeaderToStream(Stream stream) {
            HeaderStreamPosition = stream.Position;

            // TODO : implement checksum!
            stream.WriteAsByteArray(CheckSum);
            stream.WriteAsByteArray(CompressedDataLength);
            stream.WriteAsByteArray(UncompressedDataLength);
            if (DataReservedArea.Length > 0) {
                stream.Write(DataReservedArea, 0, DataReservedArea.Length);
            }
        }

        /// <summary>
        /// Read data from a stream to fill this <see cref="CfData"/>
        /// </summary>
        /// <param name="stream"></param>
        public int ReadHeaderFromStream(Stream stream) {
            HeaderStreamPosition = stream.Position;
            int nbBytesRead = 0;
            // u4 csum
            nbBytesRead += stream.ReadAsByteArray(out uint checkSum);
            CheckSum = checkSum;
            // u2 cbData
            nbBytesRead += stream.ReadAsByteArray(out ushort compressedDataLength);
            CompressedDataLength = compressedDataLength;
            // u2 cbUncomp
            nbBytesRead += stream.ReadAsByteArray(out ushort uncompressedDataLength);
            UncompressedDataLength = uncompressedDataLength;
            // u1[CFHEADER.cbCFData] abReserve(optional)
            if (_parent.DataReservedAreaSize > 0) {
                DataReservedArea = new byte[_parent.DataReservedAreaSize];
                nbBytesRead += stream.Read(DataReservedArea, 0, DataReservedArea.Length);
            }

            // we can't overflow because the max size of a cab archive is uint.MaxValue!
            CompressedDataOffset = (uint) stream.Position;

            if (nbBytesRead != DataHeaderLength) {
                throw new CfCabException($"Data info length expected {DataHeaderLength} vs actual {nbBytesRead}");
            }

            return nbBytesRead;
        }

        private byte[] CompressData(byte[] uncompressedData) {
            // TODO : compression algo
            return uncompressedData;
        }

        private byte[] UncompressData(byte[] compressedData) {
            // TODO : compression algo
            return compressedData;
        }

        public override string ToString() {
            var returnedValue = new StringBuilder();
            returnedValue.AppendLine("====== DATA ======");
            returnedValue.AppendLine($"{nameof(CheckSum)} = {CheckSum}");
            returnedValue.AppendLine($"{nameof(CompressedDataLength)} = {CompressedDataLength}");
            returnedValue.AppendLine($"{nameof(UncompressedDataLength)} = {UncompressedDataLength}");
            returnedValue.AppendLine($"{nameof(DataReservedArea)} = {(DataReservedArea == null ? "null" : Encoding.Default.GetString(DataReservedArea))}");
            return returnedValue.ToString();
        }
    }
}