using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CabinetManager.core {

    /// <summary>
    /// Each <see cref="CfFolder"/> contains information about one of the folders or partial folders stored in this cabinet file. 
    /// The first <see cref="CfFolder"/> entry immediately follows the <see cref="CfFolder"/> entry and subsequent <see cref="CfCabinet.FolderReservedAreaSize"/> records for this cabinet are contiguous. 
    /// <see cref="CfCabinet"/> indicates how many <see cref="CfFolder"/> entries are present.
    /// 
    /// Folders may start in one cabinet, and continue on to one or more succeeding cabinets. 
    /// When the cabinet file creator detects that a folder has been continued into another cabinet, 
    /// it will complete that folder as soon as the current file has been completely compressed. 
    /// Any additional files will be placed in the next folder. Generally, this means that a folder would span at most two cabinets, 
    /// but if the file is large enough, it could span more than two cabinets.
    /// 
    /// <see cref="CfFolder"/> entries actually refer to folder fragments, not necessarily complete folders. 
    /// A <see cref="CfFile.FolderIndex"/> structure is the beginning of a folder if the <see cref="CfFile"/> value in the first file referencing the folder does not indicate 
    /// the folder is continued from the previous cabinet file.
    /// 
    /// The <see cref="CfCabinet"/> field may vary from one folder to the next, unless the folder is continued from a previous cabinet file.
    /// </summary>
    class CfFolder {

        private CfCabinet _parent;

        public CfFolder(CfCabinet parent) {
            _parent = parent;
        }

        /// <summary>
        /// This folder index, starting at 0
        /// </summary>
        public ushort FolderIndex { get; set; }

        /// <summary>
        /// Path of the folder inside the cab
        /// </summary>
        /// <example>folder\subfolder\myfolder</example>
        public string FolderPath { get; set; }

        /// <summary>
        /// The number of byte needed for the folder info (uint = 4, ushort = 2, byte = 1)
        /// </summary>
        internal uint FolderInfoLength => 4 + 2 + 2 + (uint) FolderReservedArea.Length;

        /// <summary>
        /// offset of the first <see cref="CfData"/> block in this folder
        /// </summary>
        public uint FirstDataBlockOffset  { get; set; }

        /// <summary>
        /// number of <see cref="CfData"/> blocks in this folder
        /// </summary>
        public ushort DataBlocksCount { get; set; }

        /// <summary>
        /// Stream position at which we can write <see cref="FirstDataBlockOffset"/> and <see cref="DataBlocksCount"/>
        /// </summary>
        private long StreamPositionToWriteDataBlockInfo { get; set; }

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
        /// List of files in this folder
        /// </summary>
        public List<CfFile> Files { get; set; }

        /// <summary>
        /// list of data for this folder
        /// </summary>
        public List<CfData> Data { get; set; }
        
        public void WriteToStream(Stream stream) {

            StreamPositionToWriteDataBlockInfo = stream.Position;
            stream.WriteAsByteArray(FirstDataBlockOffset);
            stream.WriteAsByteArray(DataBlocksCount);
            stream.WriteAsByteArray((ushort) CompressionType);
            if (FolderReservedArea.Length > 0) {
                stream.Write(FolderReservedArea, 0, FolderReservedArea.Length);
            }
        }

        public void UpdateDataBlockInfo(Stream stream) {
            stream.Position = StreamPositionToWriteDataBlockInfo;
            stream.WriteAsByteArray(FirstDataBlockOffset);
            stream.WriteAsByteArray(DataBlocksCount);
        }
    }
}