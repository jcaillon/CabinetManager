﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CabinetManager.Utilities;

namespace CabinetManager.core {
    /// <summary>
    /// Each <see cref="CfFolder"/> contains information about one of the folders or partial folders stored in this cabinet file. 
    /// The first <see cref="CfFolder"/> entry immediately follows the <see cref="CfCabinet"/> entry and subsequent <see cref="CfFolder"/> records for this cabinet are contiguous. 
    /// <see cref="CfCabinet.FoldersCount"/> indicates how many <see cref="CfFolder"/> entries are present.
    /// 
    /// Folders may start in one cabinet, and continue on to one or more succeeding cabinets. 
    /// When the cabinet file creator detects that a folder has been continued into another cabinet, 
    /// it will complete that folder as soon as the current file has been completely compressed. 
    /// Any additional files will be placed in the next folder. Generally, this means that a folder would span at most two cabinets, 
    /// but if the file is large enough, it could span more than two cabinets.
    /// 
    /// <see cref="CfFolder"/> entries actually refer to folder fragments, not necessarily complete folders. 
    /// A <see cref="CfFolder"/> structure is the beginning of a folder if the <see cref="FolderIndex"/> value in the first file referencing the folder does not indicate 
    /// the folder is continued from the previous cabinet file.
    /// 
    /// The <see cref="CompressionType"/> field may vary from one folder to the next, unless the folder is continued from a previous cabinet file.
    /// </summary>
    class CfFolder {
        /// <summary>
        /// The maximum uncompressed size for each individual folder
        /// </summary>
        internal const int FolderMaximumUncompressedSize = 0x7FFF8000;

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
        private uint FirstDataBlockOffset { get; set; }

        /// <summary>
        /// number of <see cref="CfData"/> blocks in this folder
        /// </summary>
        private ushort DataBlocksCount { get; set; }

        /// <summary>
        /// Stream position at which we can write this <see cref="CfFolder"/> header
        /// </summary>
        private long HeaderStreamPosition { get; set; }

        /// <summary>
        /// compression type
        /// </summary>
        public CfFolderTypeCompress CompressionType { get; set; }

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
        public List<CfFile> Files { get; set; } = new List<CfFile>();

        /// <summary>
        /// list of data for this folder
        /// </summary>
        private List<CfData> Data { get; set; } = new List<CfData>();

        /// <summary>
        /// Reads data from <param name="sourceStream"></param> and read them into <param name="targetStream"></param>
        /// </summary>
        /// <param name="sourceStream"></param>
        /// <param name="targetStream"></param>
        /// <param name="uncompressedFileOffset"></param>
        /// <param name="uncompressedFileSize"></param>
        public void ExtractDataToStream(Stream sourceStream, Stream targetStream, uint uncompressedFileOffset, uint uncompressedFileSize) {
            if (Data.Count == 0) {
                // read data headers if needed
                ReadDataHeaderFromStream(sourceStream);
            }

            var uncompressedFileOffsetToRead = uncompressedFileOffset;
            var uncompressedFileLengthLeftToRead = uncompressedFileSize;
            var iDataBlock = 0;
            do {
                var dataBlock = Data[iDataBlock];

                // find the data block in which the data we want to read starts
                if (dataBlock.UncompressedDataOffset <= uncompressedFileOffsetToRead &&
                    uncompressedFileOffsetToRead <= dataBlock.UncompressedDataOffset + dataBlock.UncompressedDataLength) {
                    // the first byte of the uncompressed data will be found in this dataBlock

                    byte[] uncompressedData = dataBlock.GetUncompressedData(sourceStream);

                    var fileDataOffsetInThisBlockData = (int) uncompressedFileOffsetToRead - (int) dataBlock.UncompressedDataOffset;
                    var fileDataLengthReadInThisBlockData = Math.Min(dataBlock.UncompressedDataLength - fileDataOffsetInThisBlockData, (int) uncompressedFileLengthLeftToRead);
                    targetStream.Write(uncompressedData, fileDataOffsetInThisBlockData, fileDataLengthReadInThisBlockData);

                    uncompressedFileOffsetToRead += (uint) fileDataLengthReadInThisBlockData;
                    uncompressedFileLengthLeftToRead -= (uint) fileDataLengthReadInThisBlockData;
                }
            } while (iDataBlock++ < Data.Count && uncompressedFileLengthLeftToRead > 0);

            if (uncompressedFileLengthLeftToRead > 0) {
                throw new CfCabException($"Failed to read the entire data starting at {uncompressedFileOffset} with length {uncompressedFileSize}");
            }
        }

        /// <summary>
        /// Write this instance of <see cref="CfFolder"/> to a stream
        /// </summary>
        public void WriteHeaderToStream(Stream stream) {
            HeaderStreamPosition = stream.Position;

            stream.WriteAsByteArray(FirstDataBlockOffset);
            stream.WriteAsByteArray(DataBlocksCount);
            stream.WriteAsByteArray((ushort) CompressionType);
            if (FolderReservedArea.Length > 0) {
                stream.Write(FolderReservedArea, 0, FolderReservedArea.Length);
            }
        }

        /// <summary>
        /// Read data from a stream to fill this <see cref="CfFolder"/>
        /// </summary>
        /// <param name="stream"></param>
        public int ReadHeaderFromStream(Stream stream) {
            HeaderStreamPosition = stream.Position;

            int nbBytesRead = 0;
            // u4 coffCabStart
            nbBytesRead += stream.ReadAsByteArray(out uint firstDataBlockOffset);
            FirstDataBlockOffset = firstDataBlockOffset;
            // u2 cCFData
            nbBytesRead += stream.ReadAsByteArray(out ushort dataBlocksCount);
            DataBlocksCount = dataBlocksCount;
            // u2 typeCompress
            nbBytesRead += stream.ReadAsByteArray(out ushort compressionType);
            CompressionType = (CfFolderTypeCompress) compressionType;

            // u1[CFHEADER.cbCFFolder] abReserve(optional)
            if (_parent.FolderReservedAreaSize > 0) {
                FolderReservedArea = new byte[_parent.FolderReservedAreaSize];
                nbBytesRead += stream.Read(FolderReservedArea, 0, FolderReservedArea.Length);
            }

            if (nbBytesRead != FolderHeaderLength) {
                throw new CfCabException($"Folder info length expected {FolderHeaderLength} vs actual {nbBytesRead}");
            }

            return nbBytesRead;
        }

        public void ReadDataHeaderFromStream(Stream stream) {
            if (DataBlocksCount == 0) {
                throw new CfCabException($"The data block count is {DataBlocksCount}, read the folder header first or correct the data");
            }

            uint currentUncompressedDataOffset = 0;
            var currentDataOffset = FirstDataBlockOffset;
            for (int i = 0; i < DataBlocksCount; i++) {
                var cfData = new CfData(this);
                stream.Position = currentDataOffset;
                cfData.ReadHeaderFromStream(stream);
                cfData.UncompressedDataOffset = currentUncompressedDataOffset;
                Data.Add(cfData);
                currentDataOffset = cfData.CompressedDataOffset + cfData.CompressedDataLength;
                currentUncompressedDataOffset += cfData.UncompressedDataLength;
            }
        }

        public void UpdateDataBlockInfo(Stream stream) {
            if (HeaderStreamPosition == 0) {
                throw new CfCabException($"Write or read before updating");
            }

            stream.Position = HeaderStreamPosition;
            stream.WriteAsByteArray(FirstDataBlockOffset);
            stream.WriteAsByteArray(DataBlocksCount);
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