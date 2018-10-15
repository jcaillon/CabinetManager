using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CabinetManager.Utilities;

namespace CabinetManager.core {
    
    /// <summary>
    /// <para>
    /// Each <see cref="CfFolder"/> contains information about one of the folders or partial folders stored in this cabinet file. 
    /// The first <see cref="CfFolder"/> entry immediately follows the <see cref="CfCabinet"/> entry and subsequent <see cref="CfFolder"/> records for this cabinet are contiguous. 
    /// <see cref="CfCabinet.FoldersCount"/> indicates how many <see cref="CfFolder"/> entries are present.
    /// 
    /// Folders may start in one cabinet, and continue on to one or more succeeding cabinets. 
    /// When the cabinet file creator detects that a folder has been continued into another cabinet, 
    /// it will complete that folder as soon as the current file has been completely compressed. 
    /// Any additional files will be placed in the next folder. Generally, this means that a folder would span at most two cabinets, 
    /// but if the file is large enough, it could span more than two cabinets.
    /// See <see cref="CfFile.FolderIndex"/> for more information about contiguous folders.
    /// 
    /// <see cref="CfFolder"/> entries actually refer to folder fragments, not necessarily complete folders. 
    /// A <see cref="CfFolder"/> structure is the beginning of a folder if the <see cref="FolderIndex"/> value in the first file referencing the folder does not indicate 
    /// the folder is continued from the previous cabinet file.
    /// 
    /// The <see cref="CompressionType"/> field may vary from one folder to the next, unless the folder is continued from a previous cabinet file.
    /// </para>
    /// </summary>
    internal class CfFolder {
        
        /// <summary>
        /// The maximum uncompressed size for each individual folder
        /// </summary>
        internal const uint FolderMaximumUncompressedSize = 0x7FFF8000;    
        
        /// <summary>
        /// The maximum number of files to store in a single folder.
        /// </summary>
        internal const uint FolderMaximumFileCount = ushort.MaxValue; // 0xFFFF
                
        /// <summary>
        /// The maximum number of data blocks for a single folder.
        /// </summary>
        internal const uint FolderMaximumDataBlockCount = ushort.MaxValue;

        private readonly CfCabinet _parent;

        public CfFolder(CfCabinet parent) {
            _parent = parent;
        }

        /// <summary>
        /// This folder index, starting at 0
        /// </summary>
        public ushort FolderIndex { get; set; }

        /// <summary>
        /// The number of byte needed for the folder header (sizeof(uint) = 4, sizeof(ushort) = 2, sizeof(byte) = 1)
        /// </summary>
        internal uint FolderHeaderLength => 8 + (uint) (FolderReservedArea?.Length ?? 0); // 4+2+2+(1*Length+1)

        /// <summary>
        /// offset of the first <see cref="CfData"/> block in this folder
        /// </summary>
        public uint FirstDataBlockOffset { get; set; }

        /// <summary>
        /// number of <see cref="CfData"/> blocks in this folder
        /// </summary>
        private ushort DataBlocksCount {
            get => Math.Max(_dataBlocksCount, (ushort) Data.Count);
            set => _dataBlocksCount = value;
        }

        private ushort _dataBlocksCount;

        /// <summary>
        /// Stream position at which we can write this <see cref="CfFolder"/> header
        /// </summary>
        private long HeaderStreamPosition { get; set; }

        /// <summary>
        /// compression type
        /// </summary>
        public CfFolderTypeCompress CompressionType {
            get => _compressionType;
            set {
                _dataCompressor = null;
                _dataDecompressor = null;
                _compressionType = value;
            }
        }

        /// <summary>
        /// if <see cref="CfHeaderFlag.CfhdrReservePresent"/> is set in <see cref="CfCabinet.Flags"/> and <see cref="CfCabinet.FolderReservedAreaSize"/> is non-zero,
        /// then this field contains per-datablock application information. This field is defined by the application and used for application-defined purposes.
        /// </summary>
        public byte[] FolderReservedArea { get; set; }

        /// <summary>
        /// (optional) size of per-datablock reserved area, need the CfhdrReservePresent flag
        /// </summary>
        public byte DataReservedAreaSize => _parent.DataReservedAreaSize;

        /// <summary>
        /// List of files in this folder
        /// </summary>
        public List<CfFile> Files { get; } = new List<CfFile>();

        /// <summary>
        /// list of data for this folder
        /// </summary>
        private List<CfData> Data { get; } = new List<CfData>();

        public long FolderUncompressedSize {
            get {
                long total = 0;
                foreach (var file in Files) {
                    total += file.UncompressedFileSize;
                }
                return total;
            }
        }

        private IDataCompressor _dataCompressor;
        
        private IDataDecompressor _dataDecompressor;
        
        private CfFolderTypeCompress _compressionType;
        
        /// <summary>
        /// Reads data from <param name="reader"></param> and read them into <param name="targetStream"></param>
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="targetStream"></param>
        /// <param name="uncompressedFileOffset"></param>
        /// <param name="uncompressedFileSize"></param>
        public void ExtractData(BinaryReader reader, Stream targetStream, uint uncompressedFileOffset, uint uncompressedFileSize) {
            if (Data.Count == 0) {
                // read data headers if needed
                ReadDataHeader(reader);
            }

            var uncompressedFileOffsetToRead = uncompressedFileOffset;
            var uncompressedFileLengthLeftToRead = uncompressedFileSize;
            var iDataBlock = 0;
            do {
                var dataBlock = Data[iDataBlock];

                // find the data block in which the data we want to read starts
                if (dataBlock.UncompressedDataOffset <= uncompressedFileOffsetToRead &&
                    (uncompressedFileOffsetToRead <= dataBlock.UncompressedDataOffset + dataBlock.UncompressedDataLength || dataBlock.UncompressedDataLength == 0)) {
                    // the first byte of the uncompressed data will be found in this dataBlock

                    byte[] uncompressedData = dataBlock.ReadUncompressedData(reader);

                    var fileDataOffsetInThisBlockData = (int) uncompressedFileOffsetToRead - (int) dataBlock.UncompressedDataOffset;
                    var fileDataLengthReadInThisBlockData = Math.Min(uncompressedData.Length - fileDataOffsetInThisBlockData, (int) uncompressedFileLengthLeftToRead);
                    targetStream.Write(uncompressedData, fileDataOffsetInThisBlockData, fileDataLengthReadInThisBlockData);

                    uncompressedFileOffsetToRead += (uint) fileDataLengthReadInThisBlockData;
                    uncompressedFileLengthLeftToRead -= (uint) fileDataLengthReadInThisBlockData;
                }
            } while (iDataBlock++ < Data.Count && uncompressedFileLengthLeftToRead > 0);

            if (uncompressedFileLengthLeftToRead > 0) {
                throw new CfCabException($"Failed to read the entire data starting at {uncompressedFileOffset} with length {uncompressedFileSize}.");
            }
        }
        
        public void SaveFolder(BinaryReader reader, BinaryWriter writer) {
            
            var dataBlockBuffer = new byte[CfData.MaxUncompressedDataLength];
            var dataBlockBufferPosition = 0;

            long uncompressedDataOffset = 0;
            foreach (var file in Files) { // .OrderBy(f => f.UncompressedFileSize)
                var bytesLeftInBuffer = dataBlockBuffer.Length - dataBlockBufferPosition;
                
                if (!string.IsNullOrEmpty(file.AbsolutePath)) {
                    // read data from file
                    using (Stream sourceStream = File.OpenRead(file.AbsolutePath)) {
                        byte[] fileBuffer = new byte[bytesLeftInBuffer];
                        int nbBytesRead;
                        while ((nbBytesRead = sourceStream.Read(fileBuffer, 0, fileBuffer.Length)) > 0) {
                            Array.Copy(fileBuffer, 0, dataBlockBuffer, dataBlockBufferPosition, nbBytesRead);
                            dataBlockBufferPosition += nbBytesRead;
                            if (dataBlockBuffer.Length - dataBlockBufferPosition <= 0) { // < 0 should never happen
                                // buffer full, flush it
                                PushToNewDataBlock(writer, ref dataBlockBuffer, ref dataBlockBufferPosition, uncompressedDataOffset);
                            }
                        }
                    }
                } else {
                    // read data from the existing cabinet
                    if (Data.Count == 0) {
                        // read data headers if needed
                        ReadDataHeader(reader);
                    }
        
                    var uncompressedFileOffsetToRead = file.UncompressedFileOffset;
                    var uncompressedFileLengthLeftToRead = file.UncompressedFileSize;
                    var iDataBlock = 0;
                    do {
                        var dataBlock = Data[iDataBlock];
        
                        // find the data block in which the data we want to read starts
                        if (dataBlock.UncompressedDataOffset <= uncompressedFileOffsetToRead &&
                            (uncompressedFileOffsetToRead <= dataBlock.UncompressedDataOffset + dataBlock.UncompressedDataLength || dataBlock.UncompressedDataLength == 0)) {
                            // the first byte of the uncompressed data will be found in this dataBlock
        
                            byte[] uncompressedData = dataBlock.ReadUncompressedData(reader);
        
                            var fileDataOffsetInThisBlockData = (int) uncompressedFileOffsetToRead - (int) dataBlock.UncompressedDataOffset;
                            var fileDataLengthReadInThisBlockData = Math.Min(uncompressedData.Length - fileDataOffsetInThisBlockData, (int) uncompressedFileLengthLeftToRead);
                            
                            var dataChunkOffset = 0;
                            var dataChunkLengthLeft = fileDataLengthReadInThisBlockData - fileDataOffsetInThisBlockData;

                            do {
                                var dataChunkLengthToRead = Math.Min(dataChunkLengthLeft, bytesLeftInBuffer);
                                //var dataChunk = new byte[dataChunkLengthToRead];
                                //Array.Copy(uncompressedData, fileDataOffsetInThisBlockData + dataChunkOffset, dataChunk, 0, dataChunkLengthToRead);
                                
                                Array.Copy(uncompressedData, fileDataOffsetInThisBlockData + dataChunkOffset, dataBlockBuffer, dataBlockBufferPosition, dataChunkLengthToRead);
                                dataBlockBufferPosition += dataChunkLengthToRead;
                                
                                if (dataBlockBuffer.Length - dataBlockBufferPosition <= 0) { // < 0 should never happen
                                    // buffer full, flush it
                                    PushToNewDataBlock(writer, ref dataBlockBuffer, ref dataBlockBufferPosition, uncompressedDataOffset);
                                }

                                dataChunkOffset += dataChunkLengthToRead;
                                dataChunkLengthLeft -= dataChunkLengthToRead;
                            } while (dataChunkLengthLeft > 0);
                            
                            uncompressedFileOffsetToRead += (uint) fileDataLengthReadInThisBlockData;
                            uncompressedFileLengthLeftToRead -= (uint) fileDataLengthReadInThisBlockData;
                        }
                    } while (iDataBlock++ < Data.Count && uncompressedFileLengthLeftToRead > 0);
        
                    if (uncompressedFileLengthLeftToRead > 0) {
                        throw new CfCabException($"Failed to read the entire data starting at {file.UncompressedFileOffset} with length {file.UncompressedFileSize}.");
                    }
                }

                uncompressedDataOffset += file.UncompressedFileSize;
            }

            if (dataBlockBufferPosition > 0) {
                // flush data block
                PushToNewDataBlock(writer, ref dataBlockBuffer, ref dataBlockBufferPosition, uncompressedDataOffset);
            }
        }

        public void PushToNewDataBlock(BinaryWriter writer, ref byte[] dataBlockBuffer, ref int dataBlockBufferPosition, long uncompressedDataOffset) {           
            if (Data.Count + 1 > FolderMaximumDataBlockCount) {
                throw new CfCabException($"The total number of data block for this folder {FolderIndex} exceeds the limit of {FolderMaximumDataBlockCount}.");
            }
            
            var cfData = new CfData(this) {
                UncompressedDataOffset = uncompressedDataOffset
            };

            byte[] uncompressedData;
            if (dataBlockBuffer.Length - dataBlockBufferPosition <= 0) {
                uncompressedData = dataBlockBuffer;
            } else {
                uncompressedData = new byte[dataBlockBufferPosition];
                Array.Copy(dataBlockBuffer, 0, uncompressedData, 0, dataBlockBufferPosition);
            }
            
            cfData.WriteUncompressedData(writer, uncompressedData);
            Data.Add(cfData);
                        
            // reset data block buffer
            dataBlockBuffer = new byte[CfData.MaxUncompressedDataLength];
            dataBlockBufferPosition = 0;
        }

        /// <summary>
        /// Write this instance of <see cref="CfFolder"/> to a stream
        /// </summary>
        public void WriteFolderHeader(BinaryWriter writer) {
            HeaderStreamPosition = writer.BaseStream.Position;

            writer.Write(FirstDataBlockOffset);
            writer.Write(DataBlocksCount);
            writer.Write((ushort) CompressionType);
            if (FolderReservedArea != null && FolderReservedArea.Length > 0) {
                writer.Write(FolderReservedArea, 0, FolderReservedArea.Length);
            }
        }

        /// <summary>
        /// Read data from a stream to fill this <see cref="CfFolder"/>
        /// </summary>
        /// <param name="reader"></param>
        public void ReadFolderHeader(BinaryReader reader) {
            HeaderStreamPosition = reader.BaseStream.Position;

            // u4 coffCabStart
            FirstDataBlockOffset = reader.ReadUInt32();
            // u2 cCFData
            DataBlocksCount = reader.ReadUInt16();
            // u2 typeCompress
            CompressionType = (CfFolderTypeCompress) reader.ReadUInt16();

            // u1[CFHEADER.cbCFFolder] abReserve(optional)
            if (_parent.FolderReservedAreaSize > 0) {
                FolderReservedArea = new byte[_parent.FolderReservedAreaSize];
            }

            if (reader.BaseStream.Position - HeaderStreamPosition != FolderHeaderLength) {
                throw new CfCabException($"Folder info length expected {FolderHeaderLength} vs actual {reader.BaseStream.Position - HeaderStreamPosition}");
            }
        }

        /// <summary>
        /// Read <see cref="CfData"/> info from the reader.
        /// </summary>
        /// <param name="reader"></param>
        /// <exception cref="CfCabException"></exception>
        public void ReadDataHeader(BinaryReader reader) {
            if (DataBlocksCount == 0) {
                throw new CfCabException($"The data block count is {DataBlocksCount}, read the folder header first or correct the data");
            }

            uint currentUncompressedDataOffset = 0;
            long currentDataOffset = FirstDataBlockOffset;
            for (int i = 0; i < DataBlocksCount; i++) {
                var cfData = new CfData(this);
                reader.BaseStream.Position = currentDataOffset;
                cfData.ReadHeader(reader);
                cfData.UncompressedDataOffset = currentUncompressedDataOffset;
                Data.Add(cfData);
                currentDataOffset = cfData.CompressedDataOffset + cfData.CompressedDataLength;
                currentUncompressedDataOffset += cfData.UncompressedDataLength;
            }

            DataBlocksCount = 0;
        }

        /// <summary>
        /// Allows to rewrite <see cref="FirstDataBlockOffset"/> once the data blocks are actually written.
        /// </summary>
        /// <param name="writer"></param>
        /// <exception cref="CfCabException"></exception>
        public void UpdateDataBlockInfo(BinaryWriter writer) {
            if (HeaderStreamPosition == 0) {
                throw new CfCabException("Write or read before updating");
            }

            var previousStreamPos = writer.BaseStream.Position;
            writer.BaseStream.Position = HeaderStreamPosition;
            writer.Write(FirstDataBlockOffset);
            writer.Write(DataBlocksCount);
            writer.BaseStream.Position = previousStreamPos;
        }
        
        /// <summary>
        /// Compress some data using <see cref="CompressionType"/>.
        /// </summary>
        /// <param name="uncompressedData"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public byte[] CompressData(byte[] uncompressedData) {
            if (_dataCompressor == null) {
                switch (CompressionType) {
                    case CfFolderTypeCompress.None:
                        _dataCompressor = new NoCompressionDataCompressor();
                        break;
                    default:
                        throw new NotImplementedException($"Unimplemented compression type : {CompressionType}");
                }
            }
            return _dataCompressor.CompressData(uncompressedData);
        }

        /// <summary>
        /// Decompress some data using <see cref="CompressionType"/>.
        /// </summary>
        /// <param name="compressedData"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public byte[] UncompressData(byte[] compressedData) {
            if (_dataDecompressor == null) {
                switch (CompressionType) {
                    case CfFolderTypeCompress.None:
                        _dataDecompressor = new NoCompressionDataDecompressor();
                        break;
                    default:
                        throw new NotImplementedException($"Unimplemented compression type : {CompressionType}");
                }
            }
            return _dataDecompressor.DecompressData(compressedData);
        }

        /// <summary>
        /// Returns the compressed data of the first data block of the next cabinet.
        /// </summary>
        /// <remarks>This is used to read a data block that continues on a next cabinet file.</remarks>
        /// <param name="uncompressedDataLength"></param>
        /// <returns></returns>
        internal byte[] GetNextCabinetFirstDataBlockCompressedData(out ushort uncompressedDataLength) {
            return _parent.NextCabinet.GetFirstDataBlockCompressedData(out uncompressedDataLength);
        }

        /// <summary>
        /// Returns the compressed data of the first data block of the folder.
        /// </summary>
        /// <remarks>This is used to read a data block that continues on a next cabinet file.</remarks>
        /// <param name="reader"></param>
        /// <param name="uncompressedDataLength"></param>
        /// <returns></returns>
        /// <exception cref="CfCabException"></exception>
        internal byte[] GetFirstDataBlockCompressedData(BinaryReader reader, out ushort uncompressedDataLength) {
            if (Data.Count == 0) {
                // read data headers if needed
                ReadDataHeader(reader);
                if (Data.Count == 0) {
                    throw new CfCabException($"Could not get the first data block compressed data because there are no data blocks in folder {FolderIndex} and cabinet {_parent.CabPath}.");
                }
            }

            uncompressedDataLength = Data[0].UncompressedDataLength;
            return Data[0].ReadCompressedData(reader);
        }

        public override string ToString() {
            var returnedValue = new StringBuilder();
            
            returnedValue.AppendLine("====== FOLDER ======");
            returnedValue.AppendLine($"{nameof(FirstDataBlockOffset)} = {FirstDataBlockOffset}");
            returnedValue.AppendLine($"{nameof(DataBlocksCount)} = {DataBlocksCount}");
            returnedValue.AppendLine($"{nameof(CompressionType)} = {CompressionType}");
            returnedValue.AppendLine($"{nameof(FolderReservedArea)} = {(FolderReservedArea == null ? "null" : Encoding.Default.GetString(FolderReservedArea))}");
            returnedValue.AppendLine();

            foreach (var cfFile in Files) {
                returnedValue.AppendLine(cfFile.ToString());
            }

            foreach (var cfData in Data) {
                returnedValue.AppendLine(cfData.ToString());
            }

            return returnedValue.ToString();
        }
    }
}