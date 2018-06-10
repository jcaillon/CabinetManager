using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CabinetManager.core {

    /// <summary>
    /// Each <see cref="CfFile"/> entry contains information about one of the files stored (or at least partially stored) in this cabinet.
    /// The first <see cref="CfFile"/> entry in each cabinet is found at absolute offset <see cref="CfCabinet.FirstFileEntryOffset"/>.
    /// In a standard cabinet file the first <see cref="CfFile"/> entry immediately follows the last <see cref="CfFolder"/> entry.
    /// Subsequent <see cref="CfFile"/> records for this cabinet are contiguous.
    /// 
    /// <see cref="CfCabinet.FilesCount"/> indicates how many of these entries are in the cabinet.
    /// The <see cref="CfFile"/> entries in a standard cabinet are ordered by <see cref="FolderIndex"/> value, then by <see cref="UncompressedFileOffset"/>.
    /// Entries for files continued from the previous cabinet will be first, and entries for files continued to the next cabinet will be last.
    /// </summary>
    class CfFile {

        private readonly CfFolder _parent;

        public CfFile(CfFolder parent) {
            _parent = parent;
        }

        /// <summary>
        /// Absolute file path outside of the .cab (if it exists)
        /// </summary>
        public string AbsolutePath { get; set; }

        public const ushort IfoldContinuedFromPrev = 0xFFFD;
        public const ushort IfoldContinuedToNext = 0xFFFE;
        public const ushort IfoldContinuedPrevAndNext = 0xFFFF;
        
        /// <summary>
        /// The number of byte needed for the file info (uint = 4, ushort = 2, byte = 1)
        /// </summary>
        internal uint FileInfoLength => 4 + 4 + 2 + 2 + 2 + 2 + (uint) Math.Max(Encoding.UTF8.GetByteCount(CabPath), Encoding.ASCII.GetByteCount(CabPath));

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
        public ushort FolderIndex => _parent.FolderIndex;

        /// <summary>
        /// File date time
        /// </summary>
        public DateTime FileDateTime { get; set; }

        /// <summary>
        /// attribute flags for this file
        /// </summary>
        public CfFileAttribs FileAttributes { get; set; }

        /// <summary>
        /// Path inside the cab, including path separator characters and the file name
        /// </summary>
        public string CabPath { get; set; }
        
        public void WriteToStream(Stream stream) {

            stream.WriteAsByteArray(UncompressedFileSize);
            stream.WriteAsByteArray(UncompressedFileOffset);
            stream.WriteAsByteArray(FolderIndex);
            DosDateTime.DateTimeToDosDateTime(FileDateTime, out ushort fatDate, out ushort fatTime);
            stream.WriteAsByteArray(fatDate);
            stream.WriteAsByteArray(fatTime);
            stream.WriteAsByteArray((ushort) FileAttributes);

            // use either ascii of UTF encoding
            Encoding nameEncoding = Encoding.ASCII;
            if (Encoding.UTF8.GetByteCount(CabPath) > nameEncoding.GetByteCount(CabPath)) {
                nameEncoding = Encoding.UTF8;
                // When this flag is set, this string can be converted directly to Unicode, avoiding locale-specific dependencies
                // otherwise this string is subject to interpretation depending on locale
                FileAttributes |= CfFileAttribs.NameIsUtf;
            }

            // last byte must be NULL
            var bytes = nameEncoding.GetBytes(CabPath);
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteByte(0); // NULL ending string
        }

    }
}