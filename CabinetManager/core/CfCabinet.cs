using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CabinetManager.Utilities;

namespace CabinetManager.core {
    /// <summary>
    /// Each file stored in a cabinet is stored completely within a single folder. A cabinet file may contain one or more folders, or portions of a folder.
    /// A folder can span across multiple cabinets. Such a series of cabinet files form a set. Each cabinet file contains name information for the logically adjacent cabinet files.
    /// Each folder contains one or more files. Throughout this discussion, cabinets are said to contain "files". This is for semantic purposes only.
    /// Cabinet files actually store streams of bytes, each with a name and some other common attributes.
    /// Whether these byte streams are actually files or some other kind of data is application-defined.
    /// 
    /// A cabinet file contains a cabinet header <see cref="CfCabinet"/>, followed by one or more cabinet folder <see cref="CfFolder"/> entries,
    /// a series of one or more cabinet file <see cref="CfFile"/> entries, and the actual compressed file data in <see cref="CfData"/> entries.
    /// The compressed file data in the <see cref="CfData"/> entry is stored in one of several compression formats,
    /// as indicated in the corresponding <see cref="CfFolder"/> structure. The compression encoding formats used are detailed in separate documents.
    /// </summary>
    class CfCabinet {
        public CfCabinet(string cabPath) {
            CabPath = cabPath;
            if (Exists) {
                ReadHeaders();
            }
        }

        // cab 1.3
        private const byte CabVersionMajor = 1;
        private const byte CabVersionMinor = 3;

        /// <summary>
        /// The maximum size for this cabinet file, size limitation of <see cref="CabinetSize"/>
        /// </summary>
        internal const uint CabinetMaximumSize = uint.MaxValue;

        /// <summary>
        /// The maximum length for the next/previous cabinet/prompt
        /// </summary>
        internal const int CabFileNameMaximumLength = 256;

        /// <summary>
        /// Maximum length of <see cref="CabinetReservedArea"/>
        /// </summary>
        private const ushort MaxCabinetReservedAreaDataLength = 60000;

        /// <summary>
        /// Necessary number of bytes to store the "fixed" portion of the header (sizeof(uint) = 4, sizeof(ushort) = 2, sizeof(byte) = 1)
        /// </summary>
        private const uint HeaderLengthWithoutOptions = 36; // (1+1+1+1)+4+4+4+4+4+1+1+2+2+2+2+2

        private readonly byte[] _signature = new[] {(byte) 0x4D, (byte) 0x53, (byte) 0x43, (byte) 0x46}; /* cab file signature : MSCF */

        private uint _reserved1; /* reserved = 0 */

        /// <summary>
        /// size of this cabinet file in bytes
        /// </summary>
        public uint CabinetSize { get; set; }

        private uint _reserved2; /* reserved = 0 */

        /// <summary>
        /// offset of the first <see cref="CfFile"/> entry
        /// </summary>
        internal uint FirstFileEntryOffset { get; private set; }

        private uint _reserved3; /* reserved = 0 */
        private byte _versionMinor = CabVersionMinor; /* cabinet file format version, minor */
        private byte _versionMajor = CabVersionMajor; /* cabinet file format version, major */

        /// <summary>
        /// number of <see cref="CfFolder"/> entries in this cabinet
        /// </summary>
        public ushort FoldersCount { get; private set; }

        /// <summary>
        /// number of <see cref="CfFile"/> entries in this cabinet
        /// </summary>
        public ushort FilesCount { get; private set; }

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
        public List<CfFolder> Folders { get; set; } = new List<CfFolder>();

        /// <summary>
        /// The absolute file path of this .cab
        /// </summary>
        public string CabPath { get; set; }

        /// <summary>
        /// The number of byte needed for the header info
        /// </summary>
        private uint HeaderLength {
            get {
                var optionalLength = 0;
                if (Flags.HasFlag(CfHeaderFlag.CfhdrReservePresent)) {
                    optionalLength += 4; // 2+1+1 : necessary bytes to write the length of each reserved area
                    optionalLength += CabinetReservedArea.Length;
                }

                if (Flags.HasFlag(CfHeaderFlag.CfhdrPrevCabinet)) {
                    optionalLength += Encoding.ASCII.GetByteCount(PreviousCabinetFileName ?? "") + 1 + Encoding.ASCII.GetByteCount(PreviousCabinetPromptName ?? "") + 1;
                }

                if (Flags.HasFlag(CfHeaderFlag.CfhdrNextCabinet)) {
                    optionalLength += Encoding.ASCII.GetByteCount(NextCabinetFileName ?? "") + 1 + Encoding.ASCII.GetByteCount(NextCabinetPromptName ?? "") + 1;
                }

                return HeaderLengthWithoutOptions + (uint) optionalLength;
            }
        }

        /// <summary>
        /// offset of the first <see cref="CfFile"/> entry
        /// </summary>
        /// <returns></returns>
        internal uint GetFirstFileEntryOffset() {
            return HeaderLength + (uint) Folders.Sum(f => f.FolderHeaderLength);
        }

        /// <summary>
        /// number of <see cref="CfFolder"/> entries in this cabinet
        /// </summary>
        /// <returns></returns>
        internal ushort GetFoldersCount() {
            return (ushort) Folders.Count;
        }

        /// <summary>
        /// number of <see cref="CfFile"/> entries in this cabinet
        /// </summary>
        /// <returns></returns>
        internal ushort GetFilesCount() {
            return (ushort) Folders.SelectMany(f => f.Files).Count();
        }

        /// <summary>
        /// Returns true if this cabinet exists
        /// </summary>
        public bool Exists => File.Exists(CabPath);

        /// <summary>
        /// The next cabinet instance
        /// </summary>
        internal CfCabinet NextCabinet { get; private set; }

        /// <summary>
        /// Write this instance of <see cref="CfCabinet"/> to a stream
        /// </summary>
        /// <param name="stream"></param>
        private void WriteToStream(Stream stream) {
            WriteHeaderToStream(stream);
            ReadHeaderFromStream(stream);
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
            //foreach (var folder in Folders) {
            //    folder.DataBlocksCount = 0;
            //    folder.FirstDataBlockOffset = GetFirstFileEntryOffset() + (uint) Folders.SelectMany(f => f.Files).Sum(f => f.FileHeaderLength) + 0;
            //    folder.UpdateDataBlockInfo(stream);
            //}
        }

        /// <summary>
        /// Read data from <see cref="CabPath"/> to fill this <see cref="CfCabinet"/>
        /// </summary>
        private void ReadHeaders() {
            using (Stream stream = File.OpenRead(CabPath)) {
                ReadHeaderFromStream(stream);
                ReadFileAndFolderHeadersFromStream(stream);

                // load next cabinet (if any) headers
                while (!string.IsNullOrEmpty(NextCabinetFileName)) {
                    var cabDirectory = Path.GetDirectoryName(CabPath);
                    if (string.IsNullOrEmpty(cabDirectory)) {
                        throw new CfCabException($"Invalid directory name for {CabPath}");
                    }

                    var nextCabinetFilePath = Path.Combine(cabDirectory, NextCabinetFileName);
                    if (!File.Exists(nextCabinetFilePath)) {
                        throw new CfCabException($"Could not find the next cabinet file {NextCabinetFileName} in {cabDirectory}");
                    }

                    NextCabinet = new CfCabinet(nextCabinetFilePath);
                }
            }
        }

        private void ReadFileAndFolderHeadersFromStream(Stream stream) {
            for (int i = 0; i < FoldersCount; i++) {
                var cfFolder = new CfFolder(this);
                cfFolder.ReadHeaderFromStream(stream);
                Folders.Add(cfFolder);
            }

            for (int i = 0; i < FilesCount; i++) {
                var cfFile = new CfFile(null);
                cfFile.ReadHeaderFromStream(stream);
                if (cfFile.FolderIndex >= Folders.Count) {
                    throw new CfCabException($"Invalid folder index ({cfFile.FolderIndex}) for file {cfFile.RelativePathInCab}");
                }

                cfFile.Parent = Folders[cfFile.FolderIndex];
                Folders[cfFile.FolderIndex].Files.Add(cfFile);
            }
        }

        private void WriteHeaderToStream(Stream stream) {
            stream.Write(_signature, 0, _signature.Length);
            stream.WriteAsByteArray(_reserved1);
            stream.WriteAsByteArray(CabinetSize);
            stream.WriteAsByteArray(_reserved2);
            FirstFileEntryOffset = GetFirstFileEntryOffset();
            stream.WriteAsByteArray(FirstFileEntryOffset);
            stream.WriteAsByteArray(_reserved3);
            stream.WriteByte(_versionMinor);
            stream.WriteByte(_versionMajor);
            FoldersCount = GetFoldersCount();
            stream.WriteAsByteArray(FoldersCount);
            FilesCount = GetFilesCount();
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
                    throw new CfCabException($"Maximum cabinet reserved data length is {MaxCabinetReservedAreaDataLength}, you try to use {CabinetReservedArea.Length}");
                }

                stream.WriteAsByteArray((ushort) CabinetReservedArea.Length);
                stream.WriteByte(FolderReservedAreaSize);
                stream.WriteByte(DataReservedAreaSize);

                if (CabinetReservedArea.Length > 0) {
                    stream.Write(CabinetReservedArea, 0, CabinetReservedArea.Length);
                }
            }

            if (Flags.HasFlag(CfHeaderFlag.CfhdrPrevCabinet)) {
                var lght = stream.WriteAsByteArray(PreviousCabinetFileName);
                if (lght >= CabFileNameMaximumLength) {
                    throw new CfCabException($"PreviousCabinetFileName ({PreviousCabinetFileName}) exceeds the maximum autorised length of {CabFileNameMaximumLength} with {lght}");
                }

                lght = stream.WriteAsByteArray(PreviousCabinetPromptName);
                if (lght >= CabFileNameMaximumLength) {
                    throw new CfCabException($"PreviousCabinetPromptName ({PreviousCabinetPromptName}) exceeds the maximum autorised length of {CabFileNameMaximumLength} with {lght}");
                }
            }

            if (Flags.HasFlag(CfHeaderFlag.CfhdrNextCabinet)) {
                var lght = stream.WriteAsByteArray(NextCabinetFileName);
                if (lght >= CabFileNameMaximumLength) {
                    throw new CfCabException($"NextCabinetFileName ({NextCabinetFileName}) exceeds the maximum autorised length of {CabFileNameMaximumLength} with {lght}");
                }

                lght = stream.WriteAsByteArray(NextCabinetPromptName);
                if (lght >= CabFileNameMaximumLength) {
                    throw new CfCabException($"NextCabinetPromptName ({NextCabinetPromptName}) exceeds the maximum autorised length of {CabFileNameMaximumLength} with {lght}");
                }
            }
        }

        private void ReadHeaderFromStream(Stream stream) {
            int nbBytesRead = 0;
            // u1[4] signature
            nbBytesRead += stream.Read(_signature, 0, _signature.Length);
            // u4 reserved1
            nbBytesRead += stream.ReadAsByteArray(out _reserved1);
            // u4 cbCabinet
            nbBytesRead += stream.ReadAsByteArray(out uint cabinetSize);
            CabinetSize = cabinetSize;
            // u4 reserved2
            nbBytesRead += stream.ReadAsByteArray(out _reserved2);
            // u4 coffFiles
            nbBytesRead += stream.ReadAsByteArray(out uint firstFileEntryOffset);
            FirstFileEntryOffset = firstFileEntryOffset;
            // u4 reserved3
            nbBytesRead += stream.ReadAsByteArray(out _reserved3);
            // u1 versionMinor
            nbBytesRead += 1;
            _versionMinor = (byte) stream.ReadByte();
            // u1 versionMajor
            nbBytesRead += 1;
            _versionMajor = (byte) stream.ReadByte();
            if (_versionMinor != CabVersionMinor || _versionMajor != CabVersionMajor) {
                throw new CfCabException($"Cab version expected {CabVersionMajor}.{CabVersionMinor}, actual {_versionMajor}.{_versionMinor}");
            }

            // u2 cFolders
            nbBytesRead += stream.ReadAsByteArray(out ushort foldersCount);
            FoldersCount = foldersCount;
            // u2 cFiles
            nbBytesRead += stream.ReadAsByteArray(out ushort filesCount);
            FilesCount = filesCount;
            // u2 flags
            nbBytesRead += stream.ReadAsByteArray(out ushort flags);
            Flags = (CfHeaderFlag) flags;
            // u2 setID
            nbBytesRead += stream.ReadAsByteArray(out ushort setId);
            SetId = setId;
            // u2 iCabinet
            nbBytesRead += stream.ReadAsByteArray(out ushort cabinetNumber);
            CabinetNumber = cabinetNumber;

            // optional from there

            if (Flags.HasFlag(CfHeaderFlag.CfhdrReservePresent)) {
                // u2 cbCFHeader(optional)
                nbBytesRead += stream.ReadAsByteArray(out ushort cabinetReservedAreaLength);
                CabinetReservedArea = new byte[cabinetReservedAreaLength];
                if (CabinetReservedArea.Length > MaxCabinetReservedAreaDataLength) {
                    throw new CfCabException($"Maximum cabinet reserved data length is {MaxCabinetReservedAreaDataLength}, used {CabinetReservedArea.Length}");
                }

                // u1 cbCFFolder(optional)
                nbBytesRead += 1;
                FolderReservedAreaSize = (byte) stream.ReadByte();
                // u1 cbCFData(optional)
                nbBytesRead += 1;
                DataReservedAreaSize = (byte) stream.ReadByte();

                // u1[cbCFHeader] abReserve(optional)
                if (CabinetReservedArea.Length > 0) {
                    nbBytesRead += stream.Read(CabinetReservedArea, 0, CabinetReservedArea.Length);
                }
            }

            if (Flags.HasFlag(CfHeaderFlag.CfhdrPrevCabinet)) {
                // u1[]NULL szCabinetPrev(optional)
                nbBytesRead += stream.ReadAsByteArray(out string previousCabinetFileName);
                PreviousCabinetFileName = previousCabinetFileName;
                // u1[]NULL szDiskPrev(optional)
                nbBytesRead += stream.ReadAsByteArray(out string previousCabinetPromptName);
                PreviousCabinetPromptName = previousCabinetPromptName;
            }

            if (Flags.HasFlag(CfHeaderFlag.CfhdrNextCabinet)) {
                // u1[]NULL szCabinetNext(optional)
                nbBytesRead += stream.ReadAsByteArray(out string nextCabinetFileName);
                NextCabinetFileName = nextCabinetFileName;
                // u1[]NULL szDiskNext(optional)
                nbBytesRead += stream.ReadAsByteArray(out string nextCabinetPromptName);
                NextCabinetPromptName = nextCabinetPromptName;
            }

            if (nbBytesRead != HeaderLength) {
                throw new CfCabException($"Header length expected {HeaderLength} vs actual {nbBytesRead}");
            }
        }

        public override string ToString() {
            var returnedValue = new StringBuilder();
            returnedValue.AppendLine("====== HEADER ======");
            returnedValue.AppendLine($"{nameof(_signature)} = {Encoding.Default.GetString(_signature)}");
            returnedValue.AppendLine($"{nameof(_reserved1)} = {_reserved1}");
            returnedValue.AppendLine($"{nameof(CabinetSize)} = {CabinetSize}");
            returnedValue.AppendLine($"{nameof(_reserved2)} = {_reserved2}");
            returnedValue.AppendLine($"{nameof(FirstFileEntryOffset)} = {FirstFileEntryOffset}");
            returnedValue.AppendLine($"{nameof(_reserved3)} = {_reserved3}");
            returnedValue.AppendLine($"{nameof(_versionMinor)} = {_versionMinor}");
            returnedValue.AppendLine($"{nameof(_versionMajor)} = {_versionMajor}");
            returnedValue.AppendLine($"{nameof(FoldersCount)} = {FoldersCount}");
            returnedValue.AppendLine($"{nameof(FilesCount)} = {FilesCount}");
            returnedValue.AppendLine($"{nameof(Flags)} = {Flags}");
            returnedValue.AppendLine($"{nameof(SetId)} = {SetId}");
            returnedValue.AppendLine($"{nameof(CabinetNumber)} = {CabinetNumber}");

            if (Flags.HasFlag(CfHeaderFlag.CfhdrReservePresent)) {
                returnedValue.AppendLine($"{nameof(CabinetReservedArea)}.Length = {CabinetReservedArea.Length}");
                returnedValue.AppendLine($"{nameof(FolderReservedAreaSize)} = {FolderReservedAreaSize}");
                returnedValue.AppendLine($"{nameof(DataReservedAreaSize)} = {DataReservedAreaSize}");
                returnedValue.AppendLine($"{nameof(CabinetReservedArea)} = {Encoding.Default.GetString(CabinetReservedArea)}");
            }

            if (Flags.HasFlag(CfHeaderFlag.CfhdrPrevCabinet)) {
                returnedValue.AppendLine($"{nameof(PreviousCabinetFileName)} = {PreviousCabinetFileName}");
                returnedValue.AppendLine($"{nameof(PreviousCabinetPromptName)} = {PreviousCabinetPromptName}");
            }

            if (Flags.HasFlag(CfHeaderFlag.CfhdrNextCabinet)) {
                returnedValue.AppendLine($"{nameof(NextCabinetFileName)} = {NextCabinetFileName}");
                returnedValue.AppendLine($"{nameof(NextCabinetPromptName)} = {NextCabinetPromptName}");
            }

            returnedValue.AppendLine();

            foreach (var cfFolder in Folders) {
                returnedValue.AppendLine(cfFolder.ToString());
            }

            return returnedValue.ToString();
        }
    }
}