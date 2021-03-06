﻿using System;
using System.IO;
using System.Text;
using CabinetManager.core.Exceptions;
using CabinetManager.Utilities;

namespace CabinetManager.core {
    
    /// <summary>
    /// <para>
    /// Each <see cref="CfFile"/> entry contains information about one of the files stored (or at least partially stored) in this cabinet.
    /// The first <see cref="CfFile"/> entry in each cabinet is found at absolute offset <see cref="CfCabinet.FirstFileEntryOffset"/>.
    /// In a standard cabinet file the first <see cref="CfFile"/> entry immediately follows the last <see cref="CfFolder"/> entry.
    /// Subsequent <see cref="CfFile"/> records for this cabinet are contiguous.
    /// 
    /// <see cref="CfCabinet.FilesCount"/> indicates how many of these entries are in the cabinet.
    /// The <see cref="CfFile"/> entries in a standard cabinet are ordered by <see cref="FolderIndex"/> value, then by <see cref="UncompressedFileOffset"/>.
    /// Entries for files continued from the previous cabinet will be first, and entries for files continued to the next cabinet will be last.
    ///</para>
    /// </summary>
    internal class CfFile {
        
        /// <summary>
        /// The maximum uncompressed size for each individual file
        /// </summary>
        internal const uint FileMaximumUncompressedSize = 0x7FFF8000;    

        internal CfFolder Parent { get; set; }

        public CfFile(CfFolder parent) {
            Parent = parent;
        }

        /// <summary>
        /// Absolute file path outside of the .cab (if it exists)
        /// </summary>
        public string AbsolutePath { get; set; }

        /// <summary>
        /// The maximum length for the relative file path inside the cab
        /// </summary>
        internal const int CabPathMaximumLength = 256;

        public const ushort IfoldContinuedFromPrev = 0xFFFD;
        public const ushort IfoldContinuedToNext = 0xFFFE;
        public const ushort IfoldContinuedPrevAndNext = 0xFFFF;

        /// <summary>
        /// The number of byte needed for the file header (sizeof(uint) = 4, sizeof(ushort) = 2, sizeof(byte) = 1)
        /// </summary>
        internal uint FileHeaderLength => 16 + (uint) Math.Max(Encoding.UTF8.GetByteCount(RelativePathInCab ?? ""), Encoding.ASCII.GetByteCount(RelativePathInCab ?? "")) + 1; // 4+4+2+2+2+2+(1*chars+1)

        /// <summary>
        /// uncompressed size of this file in bytes
        /// </summary>
        public uint UncompressedFileSize { get; set; }

        /// <summary>
        /// Uncompressed byte offset of the start of this file's data.
        /// For the first file in each folder, this value will usually be zero.
        /// Subsequent files in the folder will have offsets that are typically the running sum of the <see cref="UncompressedFileSize"/> values.
        /// </summary>
        public uint UncompressedFileOffset { get; set; }
        
        /// <summary>
        /// Index of the folder containing this file's data. A value of zero indicates this is the first folder in this cabinet file.
        /// The special <see cref="FolderIndex"/> values <see cref="IfoldContinuedFromPrev"/> and <see cref="IfoldContinuedPrevAndNext"/> indicate that the folder index is actually zero,
        /// but that extraction of this file would have to begin with the cabinet named in <see cref="CfCabinet.PreviousCabinetFileName"/>
        /// The special <see cref="FolderIndex"/> values <see cref="IfoldContinuedPrevAndNext"/> and <see cref="IfoldContinuedToNext"/> indicate that the folder index is actually one less
        /// than <see cref="CfCabinet.FoldersCount"/>, and that extraction of this file will require continuation to the cabinet named in <see cref="CfCabinet.NextCabinetFileName"/>
        /// </summary>
        public ushort FolderIndex {
            get => Math.Max(_folderIndex, Parent?.FolderIndex ?? 0);
            set => _folderIndex = value;
        }

        private ushort _folderIndex;
        private string _relativePathInCab;

        /// <summary>
        /// File date time
        /// </summary>
        public DateTime FileDateTime { get; set; }

        /// <summary>
        /// attribute flags for this file
        /// </summary>
        public CfFileAttribs FileAttributes { get; set; } = CfFileAttribs.Arch;

        /// <summary>
        /// Path inside the cab, including path separator characters and the file name
        /// </summary>
        public string RelativePathInCab {
            get => _relativePathInCab;
            set => _relativePathInCab = value?.NormalizeRelativePath();
        }

        /// <summary>
        /// Stream position at which we can write this <see cref="CfFile"/> header
        /// </summary>
        private long HeaderStreamPosition { get; set; }

        /// <summary>
        /// Write this instance of <see cref="CfFile"/> to a stream
        /// </summary>
        public void WriteFileHeader(BinaryWriter writer) {
            HeaderStreamPosition = writer.BaseStream.Position;

            // use either ascii of UTF encoding
            Encoding nameEncoding = Encoding.ASCII;
            if (Encoding.UTF8.GetByteCount(RelativePathInCab) > nameEncoding.GetByteCount(RelativePathInCab)) {
                nameEncoding = Encoding.UTF8;
                // When this flag is set, this string can be converted directly to Unicode, avoiding locale-specific dependencies
                // otherwise this string is subject to interpretation depending on locale
                FileAttributes |= CfFileAttribs.NameIsUtf;
            }

            writer.Write(UncompressedFileSize);
            writer.Write(UncompressedFileOffset);
            writer.Write(FolderIndex);
            DosDateTime.DateTimeToDosDateTimeUtc(FileDateTime, out ushort fatDate, out ushort fatTime);
            writer.Write(fatDate);
            writer.Write(fatTime);
            writer.Write((ushort) FileAttributes);
            var lght = writer.WriteNullTerminatedString(RelativePathInCab, nameEncoding);
            if (lght >= CabPathMaximumLength) {
                throw new CfCabException($"The file path ({RelativePathInCab}) exceeds the maximum authorised length of {CabPathMaximumLength} with {lght}.");
            }
            
            if (writer.BaseStream.Position - HeaderStreamPosition != FileHeaderLength) {
                throw new CfCabException($"File info length expected {FileHeaderLength} vs actual {writer.BaseStream.Position - HeaderStreamPosition}.");
            }
        }
        
        /// <summary>
        /// Read data from a stream to fill this <see cref="CfFile"/>
        /// </summary>
        /// <param name="reader"></param>
        public void ReadFileHeader(BinaryReader reader) {
            HeaderStreamPosition = reader.BaseStream.Position;

            // u4 cbFile
            UncompressedFileSize = reader.ReadUInt32();
            // u4 uoffFolderStart
            UncompressedFileOffset = reader.ReadUInt32();
            // u2 iFolder
            FolderIndex = reader.ReadUInt16();
            // u2 date
            var fatDate = reader.ReadUInt16();
            // u2 time
            var fatTime = reader.ReadUInt16();
            FileDateTime = DosDateTime.DosDateTimeToDateTimeUtc(fatDate, fatTime);
            // u2 attribs
            FileAttributes = (CfFileAttribs) reader.ReadUInt16();
            // char[] szName
            var nameEncoding = FileAttributes.HasFlag(CfFileAttribs.NameIsUtf) ? Encoding.UTF8 : Encoding.ASCII;
            RelativePathInCab = reader.ReadNullTerminatedString(nameEncoding);

            if (reader.BaseStream.Position - HeaderStreamPosition != FileHeaderLength) {
                throw new CfCabException($"File info length expected {FileHeaderLength} vs actual {reader.BaseStream.Position - HeaderStreamPosition}.");
            }
        }

        public override string ToString() {
            var returnedValue = new StringBuilder();
            returnedValue.AppendLine("====== FILE ======");
            returnedValue.AppendLine($"{nameof(UncompressedFileSize)} = {UncompressedFileSize}");
            returnedValue.AppendLine($"{nameof(UncompressedFileOffset)} = {UncompressedFileOffset}");
            returnedValue.AppendLine($"{nameof(FolderIndex)} = {FolderIndex}");
            returnedValue.AppendLine($"{nameof(FileDateTime)}.Date = {FileDateTime.Date}");
            returnedValue.AppendLine($"{nameof(FileDateTime)}.Time = {FileDateTime}");
            returnedValue.AppendLine($"{nameof(FileAttributes)} = {FileAttributes}");
            returnedValue.AppendLine($"{nameof(RelativePathInCab)} = {RelativePathInCab}");
            return returnedValue.ToString();
        }
    }
}