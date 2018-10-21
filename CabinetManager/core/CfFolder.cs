using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using CabinetManager.core.Exceptions;
using CabinetManager.Compressor;

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

        private CfDataBlockReader _blockReader;

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
        internal List<CfData> Data { get; } = new List<CfData>();

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

        public bool RenameFile(string oldName, string newName) {
            return _blockReader.RenameFile(oldName, newName);
        }

        /// <summary>
        /// Extract a file from the cabinet using data from <see cref="Data"/>.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="fileRelativePathInCab"></param>
        /// <param name="extractionPath"></param>
        /// <param name="cancelToken"></param>
        /// <param name="progress"></param>
        public void ExtractFileFromDataBlocks(BinaryReader reader, string fileRelativePathInCab, string extractionPath, CancellationToken? cancelToken, Action<CfSaveEventArgs> progress) {

            _blockReader.InitializeToReadFile(fileRelativePathInCab);
            
            using (Stream targetStream = File.OpenWrite(extractionPath)) {
                var dataBlockBuffer = new byte[CfData.MaxUncompressedDataLength];
                int nbBytesRead;
                while ((nbBytesRead = _blockReader.ReadUncompressedData(reader, dataBlockBuffer, 0, dataBlockBuffer.Length)) > 0) {
                    targetStream.Write(dataBlockBuffer, 0, nbBytesRead);
                    progress?.Invoke(CfSaveEventArgs.New(fileRelativePathInCab, nbBytesRead));
                    cancelToken?.ThrowIfCancellationRequested();
                }
            }
        }
        
        /// <summary>
        /// Saves the data blocks of this folder into the <paramref name="writer"/> stream.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="writer"></param>
        /// <param name="cancelToken"></param>
        /// <param name="progress"></param>
        /// <exception cref="CfCabException"></exception>
        /// <exception cref="CfCabFileMissingException"></exception>
        public void WriteFolderDataBlocks(BinaryReader reader, BinaryWriter writer, CancellationToken? cancelToken, Action<CfSaveEventArgs> progress) {
           
            var dataBlockBuffer = new byte[CfData.MaxUncompressedDataLength];
            var dataBlockBufferPosition = 0;
            long uncompressedDataOffset = 0;
            Data.Clear();
            DataBlocksCount = 0;
            
            void WriteNewDataBlock() {           
                if (Data.Count + 1 > FolderMaximumDataBlockCount) {
                    throw new CfCabException($"The total number of data block for this folder {FolderIndex} exceeds the limit of {FolderMaximumDataBlockCount}.");
                }
            
                var cfData = new CfData(this) {
                    UncompressedDataOffset = uncompressedDataOffset
                };
                Data.Add(cfData);

                byte[] uncompressedData;
                if (dataBlockBuffer.Length - dataBlockBufferPosition <= 0) {
                    uncompressedData = dataBlockBuffer;
                } else {
                    uncompressedData = new byte[dataBlockBufferPosition];
                    Array.Copy(dataBlockBuffer, 0, uncompressedData, 0, dataBlockBufferPosition);
                }
            
                cfData.WriteUncompressedData(writer, uncompressedData);
                        
                // reset data block buffer
                dataBlockBuffer = new byte[CfData.MaxUncompressedDataLength];
                dataBlockBufferPosition = 0;
            }

            foreach (var file in Files) { // .OrderBy(f => f.UncompressedFileSize)
                cancelToken?.ThrowIfCancellationRequested();
                
                var bytesLeftInBuffer = dataBlockBuffer.Length - dataBlockBufferPosition;
                
                if (!string.IsNullOrEmpty(file.AbsolutePath)) {
                    if (!File.Exists(file.AbsolutePath)) {
                        throw new CfCabFileMissingException($"Missing source file : {file.AbsolutePath}.");
                    }
                    using (Stream sourceStream = File.OpenRead(file.AbsolutePath)) {
                        int nbBytesRead;
                        while ((nbBytesRead = sourceStream.Read(dataBlockBuffer, dataBlockBufferPosition, bytesLeftInBuffer)) > 0) {
                            dataBlockBufferPosition += nbBytesRead;
                            if (dataBlockBuffer.Length - dataBlockBufferPosition <= 0) {
                                // buffer full, flush it
                                WriteNewDataBlock();
                            }
                            bytesLeftInBuffer = dataBlockBuffer.Length - dataBlockBufferPosition;
                            progress?.Invoke(CfSaveEventArgs.New(file.RelativePathInCab, nbBytesRead));
                            cancelToken?.ThrowIfCancellationRequested();
                        }
                    }
                } else {                   
                    _blockReader.InitializeToReadFile(file.RelativePathInCab);
                    int nbBytesRead;
                    while ((nbBytesRead = _blockReader.ReadUncompressedData(reader, dataBlockBuffer, dataBlockBufferPosition, bytesLeftInBuffer)) > 0) {
                        dataBlockBufferPosition += nbBytesRead;
                        if (dataBlockBuffer.Length - dataBlockBufferPosition <= 0) {
                            // buffer full, flush it
                            WriteNewDataBlock();
                        }
                        bytesLeftInBuffer = dataBlockBuffer.Length - dataBlockBufferPosition;
                        progress?.Invoke(CfSaveEventArgs.New(file.RelativePathInCab, nbBytesRead));
                        cancelToken?.ThrowIfCancellationRequested();
                    }
                }

                uncompressedDataOffset += file.UncompressedFileSize;
            }

            if (dataBlockBufferPosition > 0) {
                // flush data block
                WriteNewDataBlock();
            }
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
                throw new CfCabException($"Folder info length expected {FolderHeaderLength} vs actual {reader.BaseStream.Position - HeaderStreamPosition}.");
            }
        }

        /// <summary>
        /// Read <see cref="CfData"/> info from the reader.
        /// </summary>
        /// <param name="reader"></param>
        /// <exception cref="CfCabException"></exception>
        internal void ReadDataHeaders(BinaryReader reader) {
            if (DataBlocksCount == 0) {
                return;
            }

            uint currentUncompressedDataOffset = 0;
            long currentDataOffset = FirstDataBlockOffset;
            for (int i = 0; i < DataBlocksCount; i++) {
                var cfData = new CfData(this);
                if (currentDataOffset >= reader.BaseStream.Length) {
                    throw new CfCabException($"The end of the stream has been reached, it seems the number of data blocks ({DataBlocksCount}) is incorrect.");
                }
                reader.BaseStream.Position = currentDataOffset;
                cfData.ReadHeader(reader);
                cfData.UncompressedDataOffset = currentUncompressedDataOffset;
                Data.Add(cfData);
                currentDataOffset = cfData.CompressedDataOffset + cfData.CompressedDataLength;
                currentUncompressedDataOffset += cfData.UncompressedDataLength;
            }
            
            _blockReader = new CfDataBlockReader(this);
        }

        /// <summary>
        /// Allows to rewrite <see cref="FirstDataBlockOffset"/> once the data blocks are actually written.
        /// </summary>
        /// <param name="writer"></param>
        /// <exception cref="CfCabException"></exception>
        public void UpdateDataBlockInfo(BinaryWriter writer) {
            if (HeaderStreamPosition == 0) {
                throw new CfCabException("Write or read before updating.");
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
                        throw new NotImplementedException($"Unimplemented compression type : {CompressionType}.");
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
                        throw new NotImplementedException($"Unimplemented compression type : {CompressionType}.");
                }
            }
            return _dataDecompressor.DecompressData(compressedData);
        }

        public override string ToString() {
            var returnedValue = new StringBuilder();
            
            returnedValue.AppendLine("====== FOLDER ======");
            returnedValue.AppendLine($"{nameof(FirstDataBlockOffset)} = {FirstDataBlockOffset}");
            returnedValue.AppendLine($"{nameof(DataBlocksCount)} = {DataBlocksCount}");
            returnedValue.AppendLine($"{nameof(CompressionType)} = {CompressionType}");
            if (FolderReservedArea != null) {
                returnedValue.AppendLine($"{nameof(FolderReservedArea)} = {Encoding.Default.GetString(FolderReservedArea)}");
            }
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