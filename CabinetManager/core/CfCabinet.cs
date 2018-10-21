using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using CabinetManager.core.Exceptions;
using CabinetManager.Utilities;

namespace CabinetManager.core {
    
    /// <summary>
    /// <para>
    /// Each file stored in a cabinet is stored completely within a single folder. A cabinet file may contain one or more folders, or portions of a folder.
    /// A folder can span across multiple cabinets. Such a series of cabinet files form a set. Each cabinet file contains name information for the logically adjacent cabinet files.
    /// Each folder contains one or more files.
    /// 
    /// Cabinet files actually store streams of bytes, each with a name and some other common attributes.
    /// Whether these byte streams are actually files or some other kind of data is application-defined.
    /// 
    /// A cabinet file contains a cabinet header <see cref="CfCabinet"/>, followed by one or more cabinet folder <see cref="CfFolder"/> entries,
    /// a series of one or more cabinet file <see cref="CfFile"/> entries, and the actual compressed file data in <see cref="CfData"/> entries.
    /// The compressed file data in the <see cref="CfData"/> entry is stored in one of several compression formats,
    /// as indicated in the corresponding <see cref="CfFolder"/> structure. The compression encoding formats used are detailed in separate documents.
    /// </para>
    /// </summary>
    internal class CfCabinet : IDisposable {
        
        public CfCabinet(string cabPath, CancellationToken? cancelToken) {
            CabPath = cabPath;
            _cancelToken = cancelToken;
            OpenCab();
        }

        public void Dispose() {
            _reader?.Dispose();
        }

        private BinaryReader _reader;
        
        private readonly CancellationToken? _cancelToken;
        
        /// <summary>
        /// Event published when saving this cabinet, allows to follow the progression of the process.
        /// </summary>
        public event EventHandler<CfSaveEventArgs> OnProgress;

        /// <summary>
        /// The maximum size for this cabinet file, size limitation of <see cref="CabinetSize"/>
        /// </summary>
        internal const uint CabinetMaximumSize = int.MaxValue; // 0x7FFFFFFF
        
        /// <summary>
        /// The maximum number of files to store in a single cabinet.
        /// </summary>
        internal const uint CabinetMaximumFileCount = ushort.MaxValue; // 0xFFFF
        
        // cab 1.3
        private const byte CabVersionMajor = 1;
        private const byte CabVersionMinor = 3;

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

        private readonly byte[] _signature = {0x4D, 0x53, 0x43, 0x46}; /* cab file signature : MSCF */

        private uint _reserved1; /* reserved = 0 */

        /// <summary>
        /// size of this cabinet file in bytes
        /// </summary>
        public uint CabinetSize { get; set; }

        private uint _reserved2; /* reserved = 0 */

        /// <summary>
        /// offset of the first <see cref="CfFile"/> entry
        /// </summary>
        internal uint FirstFileEntryOffset {
            get => Math.Max(_firstFileEntryOffset, HeaderLength + (uint) Folders.Sum(f => f.FolderHeaderLength));
            set => _firstFileEntryOffset = value;
        }

        private uint _firstFileEntryOffset;

        private uint _reserved3; /* reserved = 0 */
        private byte _versionMinor = CabVersionMinor; /* cabinet file format version, minor */
        private byte _versionMajor = CabVersionMajor; /* cabinet file format version, major */

        /// <summary>
        /// number of <see cref="CfFolder"/> entries in this cabinet
        /// </summary>
        public ushort FoldersCount {
            get => Math.Max(_foldersCount, (ushort) Folders.Count);
            set => _foldersCount = value;
        }

        private ushort _foldersCount;

        /// <summary>
        /// number of <see cref="CfFile"/> entries in this cabinet
        /// </summary>
        public ushort FilesCount {
            get => Math.Max(_filesCount, (ushort) Folders.SelectMany(f => f.Files).Count());
            set => _filesCount = value;
        }

        private ushort _filesCount;

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
        public byte FolderReservedAreaSize { get; set; }

        /// <summary>
        /// (optional) size of per-datablock reserved area, need the CfhdrReservePresent flag
        /// </summary>
        public byte DataReservedAreaSize { get; set; }

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
        private List<CfFolder> Folders { get; } = new List<CfFolder>();

        /// <summary>
        /// The absolute file path of this .cab
        /// </summary>
        public string CabPath { get; }

        /// <summary>
        /// Temporary cab path to write to
        /// </summary>
        private string CabPathToWrite => _cabPathToWrite ?? (_cabPathToWrite = Path.Combine(Path.GetDirectoryName(CabPath) ?? "", $"~{Path.GetRandomFileName()}"));

        private string _cabPathToWrite;

        private bool _dataHeadersRead;
        
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
        /// Returns true if this cabinet exists
        /// </summary>
        public bool Exists => File.Exists(CabPath);

        /// <summary>
        /// Returns the complete list of files in this cabinet (and contiguous cabinet if they exist
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CfFile> GetFiles() => Folders.SelectMany(f => f.Files);
        
        /// <summary>
        /// Add a new external file to this cabinet file.
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="relativePathInCab"></param>
        /// <exception cref="CfCabException"></exception>
        public void AddExternalFile(string sourcePath, string relativePathInCab) {
            
            if (!_dataHeadersRead) {
                ReadDataHeaders(_reader);
            }
            
            if (FilesCount + 1 > CabinetMaximumFileCount) {
                throw new CfCabException($"The cabinet would exceed the maximum number of files in a single cabinet: {CabinetMaximumFileCount}.");
            }
            
            // remove existing files with the same name
            DeleteFile(relativePathInCab);

            var fileInfo = new FileInfo(sourcePath);
            var fileInfoLength = fileInfo.Length;
            if (fileInfo.Length > CfFile.FileMaximumUncompressedSize) {
                throw new CfCabException($"The file exceeds the maximum size of {CfFile.FileMaximumUncompressedSize} with a length of {fileInfoLength} bytes.");
            }

            ushort idx = 0;
            while (true) {
                if (idx >= Folders.Count) {
                    Folders.Add(new CfFolder(this));
                    Folders[idx].FolderIndex = idx;
                }
                if (Folders[idx].FolderUncompressedSize + fileInfoLength <= CfFolder.FolderMaximumUncompressedSize &&
                    Folders[idx].Files.Count + 1 <= CfFolder.FolderMaximumFileCount) {
                    break;
                }
                idx++;
            }

            var addedFile = new CfFile(Folders[idx]) {
                RelativePathInCab = relativePathInCab,
                AbsolutePath = sourcePath,
                UncompressedFileSize = (uint) fileInfoLength,
                FileDateTime = fileInfo.LastWriteTime
            };
            
            addedFile.FileDateTime = File.GetLastWriteTime(sourcePath);
            var sourceFileAttributes = File.GetAttributes(sourcePath);
            if (sourceFileAttributes.HasFlag(FileAttributes.ReadOnly)) {
                addedFile.FileAttributes |= CfFileAttribs.Rdonly;
            }
            if (sourceFileAttributes.HasFlag(FileAttributes.Hidden)) {
                addedFile.FileAttributes |= CfFileAttribs.Hiddden;
            }
            
            Folders[idx].Files.Add(addedFile);
        }

        /// <summary>
        /// Extracts a file to an external path.
        /// </summary>
        /// <param name="relativePathInCab"></param>
        /// <param name="extractionPath"></param>
        /// <returns>true if the file was actually extracted, false if it does not exist</returns>
        public bool ExtractToFile(string relativePathInCab, string extractionPath) {
            if (!_dataHeadersRead) {
                ReadDataHeaders(_reader);
            }      
            
            var fileToExtract = Folders.SelectMany(folder => folder.Files).FirstOrDefault(file => file.RelativePathInCab.Equals(relativePathInCab, StringComparison.OrdinalIgnoreCase));
            if (fileToExtract == null) {
                return false;
            }
            
            long totalNumberOfBytes = fileToExtract.UncompressedFileSize;
            long totalNumberOfBytesDone = 0;
            
            void Progress(CfSaveEventArgs args) {
                totalNumberOfBytesDone += args.BytesDone;
                args.TotalBytesToProcess = totalNumberOfBytes;
                args.TotalBytesDone = totalNumberOfBytesDone;
                OnProgress?.Invoke(this, args);
            }
           
            fileToExtract.Parent.ExtractFileFromDataBlocks(_reader, relativePathInCab, extractionPath, _cancelToken, Progress);
            
            File.SetCreationTime(extractionPath, fileToExtract.FileDateTime);
            File.SetLastWriteTime(extractionPath, fileToExtract.FileDateTime);
            if (fileToExtract.FileAttributes.HasFlag(CfFileAttribs.Rdonly)) {
                File.SetAttributes(extractionPath, FileAttributes.ReadOnly);
            }

            if (fileToExtract.FileAttributes.HasFlag(CfFileAttribs.Hiddden)) {
                File.SetAttributes(extractionPath, FileAttributes.Hidden);
            }
            
            return true;
        }

        /// <summary>
        /// Delete a file within this cabinet.
        /// </summary>
        /// <param name="relativePathInCab"></param>
        /// <returns></returns>
        public bool DeleteFile(string relativePathInCab) {
            if (!_dataHeadersRead) {
                ReadDataHeaders(_reader);
            }
            
            int nbFileDeleted = 0;
            // Remove existing files with the same name (there could be many if the file is spread over several folders/cabinets).
            foreach (var folder in Folders) {
                nbFileDeleted += folder.Files.RemoveAll(f => f.RelativePathInCab.Equals(relativePathInCab, StringComparison.OrdinalIgnoreCase));
            }
            return nbFileDeleted > 0;
        }

        /// <summary>
        /// Move (i.e. change the relative path) a file within this cabinet.
        /// </summary>
        /// <param name="relativePathInCab"></param>
        /// <param name="newRelativePathInCab"></param>
        /// <returns></returns>
        public bool MoveFile(string relativePathInCab, string newRelativePathInCab) {
            if (!_dataHeadersRead) {
                ReadDataHeaders(_reader);
            }
            
            var fileToMove = Folders.SelectMany(folder => folder.Files).FirstOrDefault(file => file.RelativePathInCab.Equals(relativePathInCab, StringComparison.OrdinalIgnoreCase));
            if (fileToMove == null) {
                return false;
            }

            if (!fileToMove.Parent.RenameFile(relativePathInCab, newRelativePathInCab)) {
                return false;
            }

            fileToMove.RelativePathInCab = newRelativePathInCab;
            
            return true;
        }
        
        /// <summary>
        /// Save this instance of <see cref="CfCabinet"/> to <see cref="CabPath"/>.
        /// </summary>
        public void Save(CfFolderTypeCompress compressionType) {
            if (!_dataHeadersRead) {
                ReadDataHeaders(_reader);
            }
            
            var cabDirectory = Path.GetDirectoryName(CabPathToWrite);
            if (!string.IsNullOrWhiteSpace(cabDirectory) && !Directory.Exists(cabDirectory)) {
                Directory.CreateDirectory(cabDirectory);
            }
            try {
                using (var writer = new BinaryWriter(File.OpenWrite(CabPathToWrite))) {
                    writer.BaseStream.Position = 0;
                    WriteHeaderToStream(writer);
                    WriteFileAndFolderHeaders(writer, compressionType);
                    WriteDataBlocks(writer);
                    UpdateCabinetSize(writer);
                }
                _reader?.Dispose();
                File.Delete(CabPath);
                File.Move(CabPathToWrite, CabPath);
            } finally {
                // get rid of the temp file
                if (File.Exists(CabPathToWrite)) {
                    File.Delete(CabPathToWrite);
                }
            }
        }
        
        private void OpenCab() {
            Folders.Clear();
            _dataHeadersRead = false;
            if (Exists) {
                _reader = new BinaryReader(File.OpenRead(CabPath));
                ReadCabinetInfo(_reader);
            }
        }
        
        /// <summary>
        /// Read data from <see cref="CabPath"/> to fill this <see cref="CfCabinet"/>
        /// </summary>
        /// <param name="reader"></param>
        private void ReadCabinetInfo(BinaryReader reader) {
            ReadCabinetHeader(reader);
            ReadFileAndFolderHeaders(reader);

            // load next cabinet (if any) headers
            while (!string.IsNullOrEmpty(NextCabinetFileName)) {
                var cabDirectory = Path.GetDirectoryName(CabPath);
                if (string.IsNullOrEmpty(cabDirectory)) {
                    throw new CfCabException($"Invalid directory name for {CabPath}.");
                }

                var nextCabinetFilePath = Path.Combine(cabDirectory, NextCabinetFileName);
                if (!File.Exists(nextCabinetFilePath)) {
                    throw new CfCabException($"Could not find the next cabinet file {NextCabinetFileName} in {cabDirectory}.");
                }

                // crash now because we won't be able to correctly read data later anyway...
                throw new NotImplementedException("The management of several consecutive cabinet files is not implemented yet.");
            }
        }

        
        private void WriteHeaderToStream(BinaryWriter writer) {
            writer.Write(_signature, 0, _signature.Length);
            writer.Write(_reserved1);
            writer.Write(CabinetSize);
            writer.Write(_reserved2);
            writer.Write(FirstFileEntryOffset);
            writer.Write(_reserved3);
            writer.Write(_versionMinor);
            writer.Write(_versionMajor);
            writer.Write(FoldersCount);
            writer.Write(FilesCount);
            writer.Write((ushort) Flags);
            writer.Write(SetId);
            writer.Write(CabinetNumber);

            // optional from there

            if (Flags.HasFlag(CfHeaderFlag.CfhdrReservePresent)) {
                if (CabinetReservedArea.Length > MaxCabinetReservedAreaDataLength) {
                    throw new CfCabException($"Maximum cabinet reserved data length is {MaxCabinetReservedAreaDataLength}, you try to use {CabinetReservedArea.Length}.");
                }

                writer.Write((ushort) CabinetReservedArea.Length);
                writer.Write(FolderReservedAreaSize);
                writer.Write(DataReservedAreaSize);

                if (CabinetReservedArea.Length > 0) {
                    writer.Write(CabinetReservedArea, 0, CabinetReservedArea.Length);
                }
            }

            if (Flags.HasFlag(CfHeaderFlag.CfhdrPrevCabinet)) {
                var lght = writer.WriteNullTerminatedString(PreviousCabinetFileName);
                if (lght >= CabFileNameMaximumLength) {
                    throw new CfCabException($"PreviousCabinetFileName ({PreviousCabinetFileName}) exceeds the maximum authorised length of {CabFileNameMaximumLength} with {lght}.");
                }

                lght = writer.WriteNullTerminatedString(PreviousCabinetPromptName);
                if (lght >= CabFileNameMaximumLength) {
                    throw new CfCabException($"PreviousCabinetPromptName ({PreviousCabinetPromptName}) exceeds the maximum authorised length of {CabFileNameMaximumLength} with {lght}.");
                }
            }

            if (Flags.HasFlag(CfHeaderFlag.CfhdrNextCabinet)) {
                var lght = writer.WriteNullTerminatedString(NextCabinetFileName);
                if (lght >= CabFileNameMaximumLength) {
                    throw new CfCabException($"NextCabinetFileName ({NextCabinetFileName}) exceeds the maximum authorised length of {CabFileNameMaximumLength} with {lght}.");
                }

                lght = writer.WriteNullTerminatedString(NextCabinetPromptName);
                if (lght >= CabFileNameMaximumLength) {
                    throw new CfCabException($"NextCabinetPromptName ({NextCabinetPromptName}) exceeds the maximum authorised length of {CabFileNameMaximumLength} with {lght}.");
                }
            }
        }

        private void WriteFileAndFolderHeaders(BinaryWriter writer, CfFolderTypeCompress compressionType) {
            foreach (var folder in Folders) { // .OrderBy(f => f.FolderIndex)
                folder.CompressionType = compressionType;
                folder.WriteFolderHeader(writer);
            }
            
            uint uncompressedFileOffset = 0;
            foreach (var folder in Folders) { // .OrderBy(f => f.FolderIndex)
                
                // files are supposed to be sorted by folder index then by uncompressed size
                foreach (var file in folder.Files) { // .OrderBy(f => f.UncompressedFileSize)
                    file.UncompressedFileOffset = uncompressedFileOffset;
                    file.WriteFileHeader(writer);
                    uncompressedFileOffset += file.UncompressedFileSize;
                }
            }
        }

        private void WriteDataBlocks(BinaryWriter writer) {

            long totalNumberOfBytes = 0;
            foreach (var folder in Folders) {
                totalNumberOfBytes += folder.FolderUncompressedSize;
            }

            long totalNumberOfBytesDone = 0;
            
            void WriteDataProgress(CfSaveEventArgs args) {
                totalNumberOfBytesDone += args.BytesDone;
                args.TotalBytesToProcess = totalNumberOfBytes;
                args.TotalBytesDone = totalNumberOfBytesDone;
                OnProgress?.Invoke(this, args);
            }
            
            foreach (var folder in Folders) { // .OrderBy(f => f.FolderIndex)
                folder.FirstDataBlockOffset = (uint) writer.BaseStream.Position;
                folder.WriteFolderDataBlocks(_reader, writer, _cancelToken, WriteDataProgress);
                folder.UpdateDataBlockInfo(writer);
            }
        }

        private void UpdateCabinetSize(BinaryWriter writer) {
            if (writer.BaseStream.Length > CabinetMaximumSize) {
                throw new CfCabException($"The cabinet size exceeds the maximum of ({CabinetMaximumSize}) bytes with {writer.BaseStream.Length}.");
            }
            CabinetSize = (uint) writer.BaseStream.Length;

            var previousStreamPos = writer.BaseStream.Position;
            writer.BaseStream.Position = 8;
            writer.Write(CabinetSize);
            writer.BaseStream.Position = previousStreamPos;
        }

        private void ReadCabinetHeader(BinaryReader reader) {
            // u1[4] signature
            reader.Read(_signature, 0, _signature.Length);
            // u4 reserved1
            _reserved1 = reader.ReadUInt32();
            // u4 cbCabinet
            CabinetSize = reader.ReadUInt32();
            // u4 reserved2
            _reserved2 = reader.ReadUInt32();
            // u4 coffFiles
            FirstFileEntryOffset = reader.ReadUInt32();
            // u4 reserved3
            _reserved3 = reader.ReadUInt32();
            // u1 versionMinor
            _versionMinor = reader.ReadByte();
            // u1 versionMajor
            _versionMajor = reader.ReadByte();
            if (_versionMinor != CabVersionMinor || _versionMajor != CabVersionMajor) {
                throw new CfCabException($"Cab version expected {CabVersionMajor}.{CabVersionMinor}, actual {_versionMajor}.{_versionMinor}.");
            }

            // u2 cFolders
            FoldersCount = reader.ReadUInt16();
            // u2 cFiles
            FilesCount = reader.ReadUInt16();
            // u2 flags
            Flags = (CfHeaderFlag) reader.ReadUInt16();
            // u2 setID
            SetId = reader.ReadUInt16();
            // u2 iCabinet
            CabinetNumber = reader.ReadUInt16();

            // optional from there

            if (Flags.HasFlag(CfHeaderFlag.CfhdrReservePresent)) {
                // u2 cbCFHeader(optional)
                var cabinetReservedAreaLength = reader.ReadUInt16();
                CabinetReservedArea = new byte[cabinetReservedAreaLength];
                if (CabinetReservedArea.Length > MaxCabinetReservedAreaDataLength) {
                    throw new CfCabException($"Maximum cabinet reserved data length is {MaxCabinetReservedAreaDataLength}, used {CabinetReservedArea.Length}.");
                }

                // u1 cbCFFolder(optional)
                FolderReservedAreaSize = reader.ReadByte();
                // u1 cbCFData(optional)
                DataReservedAreaSize = reader.ReadByte();

                // u1[cbCFHeader] abReserve(optional)
                if (CabinetReservedArea.Length > 0) {
                }
            }

            if (Flags.HasFlag(CfHeaderFlag.CfhdrPrevCabinet)) {
                // u1[]NULL szCabinetPrev(optional)
                PreviousCabinetFileName =  reader.ReadNullTerminatedString();
                // u1[]NULL szDiskPrev(optional)
                PreviousCabinetPromptName = reader.ReadNullTerminatedString();
            }

            if (Flags.HasFlag(CfHeaderFlag.CfhdrNextCabinet)) {
                // u1[]NULL szCabinetNext(optional)
                NextCabinetFileName = reader.ReadNullTerminatedString();
                // u1[]NULL szDiskNext(optional)
                NextCabinetPromptName = reader.ReadNullTerminatedString();
            }

            if (reader.BaseStream.Position != HeaderLength) {
                throw new CfCabException($"Header length expected {HeaderLength} vs actual {reader.BaseStream.Position}.");
            }
        }

        private void ReadFileAndFolderHeaders(BinaryReader reader) {
            for (int i = 0; i < FoldersCount; i++) {
                var cfFolder = new CfFolder(this);
                cfFolder.ReadFolderHeader(reader);
                Folders.Add(cfFolder);
            }

            FoldersCount = 0;

            for (int i = 0; i < FilesCount; i++) {
                var cfFile = new CfFile(null);
                cfFile.ReadFileHeader(reader);
                if (cfFile.FolderIndex >= Folders.Count) {
                    throw new CfCabException($"Invalid folder index ({cfFile.FolderIndex}) for file {cfFile.RelativePathInCab}.");
                }
                cfFile.Parent = Folders[cfFile.FolderIndex];
                cfFile.FolderIndex = 0;
                Folders[cfFile.FolderIndex].Files.Add(cfFile);
            }

            FilesCount = 0;
            FirstFileEntryOffset = 0;
        }

        private void ReadDataHeaders(BinaryReader reader) {
            foreach (var folder in Folders) {
                folder.ReadDataHeaders(reader);
            }
            _dataHeadersRead = true;
        }

        /// <summary>
        /// Returns a text representation of this cabinet file
        /// </summary>
        /// <returns></returns>
        public string GetStringFullRepresentation() {
            if (!_dataHeadersRead) {
                ReadDataHeaders(_reader);
            }
            return ToString();
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