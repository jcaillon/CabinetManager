using System;
using System.IO;
using System.Text;
using CabinetManager.core.Exceptions;

namespace CabinetManager.core {
    
    /// <summary>
    /// <para>
    /// Each <see cref="CfData"/> record describes some amount of compressed data.
    /// The first <see cref="CfData"/> entry for each folder is located using <see cref="CfFolder.FirstDataBlockOffset"/>.
    /// Subsequent <see cref="CfData"/> records for this folder are contiguous.
    /// In a standard cabinet all the <see cref="CfData"/> entries are contiguous and in the same order as the <see cref="CfFolder"/> entries that refer them.
    /// Blocks are compressed individually using the <see cref="CfFolder.CompressionType"/>.
    ///</para>
    /// </summary>
    internal class CfData {
        
        /// <summary>
        /// CAB data blocks max size in bytes in uncompressed form.
        /// Uncompressed blocks have zero growth. MSZIP guarantees that it won't grow above
        /// uncompressed size by more than 12 bytes. LZX guarantees it won't grow
        /// more than 6144 bytes.
        /// </summary>
        public const ushort MaxUncompressedDataLength = 32768;

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
        /// Offset in cabinet stream at which to read the <see cref="CompressedData"/>.
        /// </summary>
        internal long CompressedDataOffset { get; private set; }

        /// <summary>
        /// This data block contains the bytes that are positioned at <see cref="UncompressedDataOffset"/> in the uncompressed stream.
        /// </summary>
        internal long UncompressedDataOffset { get; set; }

        /// <summary>
        /// Stream position at which we can write this <see cref="CfData"/> header
        /// </summary>
        private long HeaderStreamPosition { get; set; }

        /// <summary>
        /// Returns the uncompressed bytes for this <see cref="CfData"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public byte[] ReadUncompressedData(BinaryReader reader) {
            reader.BaseStream.Position = CompressedDataOffset;

            byte[] completeCompressedData;
            ushort completeUncompressedDataLength;
            
            if (UncompressedDataLength == 0) {
                // the compressed data in this data block is only partial and should be continued in the next cabinet file
                var nextCompressedData = _parent.GetNextCabinetFirstDataBlockCompressedData(out completeUncompressedDataLength);
                
                completeCompressedData = new byte[CompressedDataLength + nextCompressedData.Length];
                Array.Copy(nextCompressedData, 0, completeCompressedData, CompressedDataLength, nextCompressedData.Length);

                var thisBlockCompressedData = ReadCompressedData(reader);
                Array.Copy(thisBlockCompressedData, completeCompressedData, thisBlockCompressedData.Length);
            } else {
                completeUncompressedDataLength = UncompressedDataLength;
                completeCompressedData = ReadCompressedData(reader);
            }
            
            var uncompressedData = _parent.UncompressData(completeCompressedData);
            if (completeUncompressedDataLength != 0 && completeUncompressedDataLength != uncompressedData.Length) {
                throw new CfDataCorruptedException($"Corrupted data block, the expected uncompressed data length is {completeUncompressedDataLength} but the actual is {uncompressedData.Length}.");
            }
            
            return uncompressedData;
        }
        
        /// <summary>
        /// Returns the compressed bytes for this <see cref="CfData"/>
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public byte[] ReadCompressedData(BinaryReader reader) {
            reader.BaseStream.Position = CompressedDataOffset;
            var compressedData = new byte[CompressedDataLength];
            reader.Read(compressedData, 0, compressedData.Length);
            return compressedData;
        }
        
        /// <summary>
        /// Write this data block with some uncompressed data
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="uncompressedData"></param>
        public void WriteUncompressedData(BinaryWriter writer, byte[] uncompressedData) {
            CompressedData = _parent.CompressData(uncompressedData);
            
            // TODO : implement checksum?
            CheckSum = ComputeCheckSum(CompressedData);
            CompressedDataLength = (ushort) CompressedData.Length;
            UncompressedDataLength = (ushort) uncompressedData.Length;
            
            WriteDataHeader(writer);

            CompressedDataOffset = writer.BaseStream.Position;
            
            writer.Write(CompressedData);

            CompressedData = null;
        }

        /// <summary>
        /// Write this instance of <see cref="CfData"/> to a stream
        /// </summary>
        public void WriteDataHeader(BinaryWriter writer) {
            HeaderStreamPosition = writer.BaseStream.Position;

            writer.Write(CheckSum);
            writer.Write(CompressedDataLength);
            writer.Write(UncompressedDataLength);
            if (DataReservedArea != null && DataReservedArea.Length > 0) {
                writer.Write(DataReservedArea, 0, DataReservedArea.Length);
            }
        }

        /// <summary>
        /// Computes the checksum for some <paramref name="compressedData"/>.
        /// </summary>
        /// <param name="compressedData"></param>
        /// <returns></returns>
        private uint ComputeCheckSum(byte[] compressedData) {
            // CFDATA.cbData = cbCompressed;
            // CFDATA.cbUncomp = cbUncompressed;
            // csumPartial = CSUMCompute(&CFDATA.ab[0],CFDATA.cbData,0);
            // CFDATA.csum = CSUMCompute(&CFDATA.cbData,sizeof(CFDATA) –
            // sizeof(CFDATA.csum),csumPartial);
            return CheckSum;
        }

        /// <summary>
        /// Read data from a stream to fill this <see cref="CfData"/>
        /// </summary>
        /// <param name="reader"></param>
        public void ReadHeader(BinaryReader reader) {
            HeaderStreamPosition = reader.BaseStream.Position;
            // u4 csum
            CheckSum = reader.ReadUInt32();
            // u2 cbData
            CompressedDataLength = reader.ReadUInt16();
            // u2 cbUncomp
            UncompressedDataLength = reader.ReadUInt16();
            // u1[CFHEADER.cbCFData] abReserve(optional)
            if (_parent.DataReservedAreaSize > 0) {
                DataReservedArea = new byte[_parent.DataReservedAreaSize];
                reader.Read(DataReservedArea, 0, DataReservedArea.Length);
            }

            // we can't overflow because the max size of a cab archive is uint.MaxValue!
            CompressedDataOffset = reader.BaseStream.Position;

            if (reader.BaseStream.Position - HeaderStreamPosition != DataHeaderLength) {
                throw new CfCabException($"Data info length expected {DataHeaderLength} vs actual {reader.BaseStream.Position - HeaderStreamPosition}");
            }
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