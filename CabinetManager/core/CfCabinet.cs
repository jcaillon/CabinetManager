using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CabinetManager.core {

    /// <summary>
    /// The <see cref="CfCabinet"/> provides information about a cabinet file
    /// </summary>
    class CfCabinet {

        private const ushort MaxCabinetReservedAreaDataLength = 60000;
        private const uint HeaderLengthWithoutOptions = 0x23;

        private readonly byte[] _signature = new[] {(byte) 0x4D, (byte) 0x53, (byte) 0x43, (byte) 0x46}; /* cab file signature : MSCF */
        private readonly uint _reserved1 = 0; /* reserved = 0 */

        /// <summary>
        /// size of this cabinet file in bytes
        /// </summary>
        public uint CabinetSize { get; set; }

        private readonly uint _reserved2 = 0; /* reserved = 0 */

        /// <summary>
        /// offset of the first <see cref="CfFile"/> entry
        /// </summary>
        internal uint FirstFileEntryOffset => HeaderLength + (uint) Folders.Sum(f => f.FolderInfoLength);

        private readonly uint _reserved3 = 0; /* reserved = 0 */
        private readonly byte _versionMinor = 3; /* cabinet file format version, minor */
        private readonly byte _versionMajor = 1; /* cabinet file format version, major */

        /// <summary>
        /// number of <see cref="CfFolder"/> entries in this cabinet
        /// </summary>
        public ushort FoldersCount => (ushort) Folders.Count;

        /// <summary>
        /// number of <see cref="CfFile"/> entries in this cabinet
        /// </summary>
        public ushort FilesCount => (ushort) Folders.SelectMany(f => f.Files).Count();

        /// <summary>
        /// Bit-mapped values that indicate the presence of optional data
        /// </summary>
        public CfHeaderFlag Flags { get; set; }

        /// <summary>
        /// An arbitrarily derived (random) value that binds a collection of linked cabinet files together.
        /// All cabinet files in a set will contain the same <see cref="SetId"/>.
        /// This field is used by cabinet file extractors to assure that cabinet files are not inadvertently mixed.
        /// This value has no meaning in a cabinet file that is not in a set.
        /// </summary>
        public ushort SetId { get; set; }

        /// <summary>
        /// Sequential number of this cabinet in a multi-cabinet set. The first cabinet has <see cref="CabinetNumber"/>=0.
        /// This field, along with <see cref="SetId"/>, is used by cabinet file extractors to assure that this cabinet is the correct continuation cabinet when spanning cabinet files.
        /// </summary>
        public ushort CabinetNumber { get; set; } /* number of this cabinet file in a set */

        /// <summary>
        /// (optional) size of per-folder reserved area, need the CfhdrReservePresent flag
        /// </summary>
        public byte FolderReservedAreaSize { get; set; } = 0;

        /// <summary>
        /// (optional) size of per-datablock reserved area, need the CfhdrReservePresent flag
        /// </summary>
        public byte DataReservedAreaSize { get; set; } = 0;
        
        /// <summary>
        /// (optional)
        /// If <see cref="CfHeaderFlag.CfhdrReservePresent"/> is set, then this field contains per-cabinet-file application information.
        /// This field is defined by the application and used for application-defined purposes.
        /// Max 60 000 length
        /// </summary>
        public byte[] CabinetReservedArea { get; set; } /* (optional) per-cabinet reserved area */

        /// <summary>
        /// (optional)
        /// If <see cref="CfHeaderFlag.CfhdrPrevCabinet"/> is not set, then this field is not present.
        /// NUL-terminated ASCII string containing the file name of the logically previous cabinet file. May contain up to 255 bytes plus the NUL byte.
        /// Note that this gives the name of the most-recently-preceding cabinet file that contains the initial instance of a file entry.
        /// This might not be the immediately previous cabinet file, when the most recent file spans multiple cabinet files.
        /// If searching in reverse for a specific file entry, or trying to extract a file that is reported to begin in the "previous cabinet",
        /// szCabinetPrev would give the name of the cabinet to examine.
        /// </summary>
        public string PreviousCabinetFileName { get; set; }

        /// <summary>
        /// (optional)
        /// If flags.cfhdrNEXT_CABINET is not set, then this field is not present. NUL-terminated ASCII string containing the file name of the next cabinet file in a set. May contain up to 255 bytes plus the NUL byte. Files extending beyond the end of the current cabinet file are continued in the named cabinet file.
        /// </summary>
        public string NextCabinetFileName { get; set; }

        /// <summary>
        /// (optional)
        /// If <see cref="CfHeaderFlag.CfhdrPrevCabinet"/> is not set, then this field is not present.
        /// NUL-terminated ASCII string containing a descriptive name for the media containing the file named in szCabinetPrev, such as the text on the diskette label.
        /// This string can be used when prompting the user to insert a diskette. May contain up to 255 bytes plus the NUL byte.
        /// </summary>
        public string PreviousCabinetPromptName { get; set; }

        /// <summary>
        /// (optional)
        /// If flags.cfhdrNEXT_CABINET is not set, then this field is not present. NUL-terminated ASCII string containing a descriptive name for the media containing the file named in szCabinetNext, such as the text on the diskette label. May contain up to 255 bytes plus the NUL byte. This string can be used when prompting the user to insert a diskette.
        /// </summary>
        public string NextCabinetPromptName { get; set; }

        /// <summary>
        /// List of folders in this cabinet
        /// </summary>
        public List<CfFolder> Folders { get; set; }

        /// <summary>
        /// The number of byte needed for the header info
        /// </summary>
        private uint HeaderLength {
            get {
                var optionalLength = 0;
                if (Flags.HasFlag(CfHeaderFlag.CfhdrReservePresent)) {
                    optionalLength += 2 + 1 + 1; // necessary bytes to write the length of each reserved area
                    optionalLength += CabinetReservedArea.Length;
                }
                if (Flags.HasFlag(CfHeaderFlag.CfhdrPrevCabinet)) {
                    optionalLength += Encoding.ASCII.GetByteCount(PreviousCabinetFileName) + 1 + Encoding.ASCII.GetByteCount(PreviousCabinetPromptName) + 1;
                }
                if (Flags.HasFlag(CfHeaderFlag.CfhdrNextCabinet)) {
                    optionalLength += Encoding.ASCII.GetByteCount(NextCabinetFileName) + 1 + Encoding.ASCII.GetByteCount(NextCabinetPromptName) + 1;
                }
                return HeaderLengthWithoutOptions + (uint) optionalLength;
            }
        }


        public void WriteToStream(Stream stream) {
            WriteHeaderToStream(stream);

            // write data
            //// write compressed data
            //// TODO : compression algo
            //using(Stream source = File.OpenRead(path)) {
            //    byte[] buffer = new byte[2048];
            //    int bytesRead;
            //    while((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0) {
            //        stream.Write(buffer, 0, bytesRead);
            //    }
            //}

            // update DATA BLOCK info for each CfFolder and Update the stream
            foreach (var folder in Folders) {
                folder.DataBlocksCount = 0;
                folder.FirstDataBlockOffset = FirstFileEntryOffset + (uint) Folders.SelectMany(f => f.Files).Sum(f => f.FileInfoLength) + 0;
                folder.UpdateDataBlockInfo(stream);
            }
            
        }

        private void WriteHeaderToStream(Stream stream) {
            stream.Write(_signature, 0, _signature.Length);
            stream.WriteAsByteArray(_reserved1);
            stream.WriteAsByteArray(CabinetSize);
            stream.WriteAsByteArray(_reserved2);
            stream.WriteAsByteArray(FirstFileEntryOffset);
            stream.WriteAsByteArray(_reserved3);
            stream.WriteByte(_versionMinor);
            stream.WriteByte(_versionMajor);
            stream.WriteAsByteArray(FoldersCount);
            stream.WriteAsByteArray(FilesCount);
            stream.WriteAsByteArray((ushort) Flags);
            if (SetId == 0) {
                SetId = (ushort) new Random().Next(ushort.MaxValue);
            }
            stream.WriteAsByteArray(SetId);
            stream.WriteAsByteArray(CabinetNumber);

            // optional from there

            if (Flags.HasFlag(CfHeaderFlag.CfhdrReservePresent)) {
                if (CabinetReservedArea.Length > MaxCabinetReservedAreaDataLength) {
                    throw new ArgumentException($"Maximum cabinet reserved data length is {MaxCabinetReservedAreaDataLength}, you try to use {CabinetReservedArea.Length}");
                }

                stream.WriteAsByteArray((ushort) CabinetReservedArea.Length);
                stream.WriteAsByteArray(FolderReservedAreaSize);
                stream.WriteAsByteArray(DataReservedAreaSize);

                if (CabinetReservedArea.Length > 0) {
                    stream.Write(CabinetReservedArea, 0, CabinetReservedArea.Length);
                }
            }

            if (Flags.HasFlag(CfHeaderFlag.CfhdrPrevCabinet)) {
                stream.WriteAsByteArray(PreviousCabinetFileName);
                stream.WriteAsByteArray(PreviousCabinetPromptName);
            }

            if (Flags.HasFlag(CfHeaderFlag.CfhdrNextCabinet)) {
                stream.WriteAsByteArray(NextCabinetFileName);
                stream.WriteAsByteArray(NextCabinetPromptName);
            }
        }


    }
}