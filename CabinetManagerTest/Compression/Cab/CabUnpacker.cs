#region header

// ========================================================================
// Copyright (c) 2017 - Julien Caillon (julien.caillon@gmail.com)
// This file (CabUnpacker.cs) is part of csdeployer.
// 
// csdeployer is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// csdeployer is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with csdeployer. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;

namespace CabinetManagerTest.Compression.Cab {
    internal class CabUnpacker : CabWorker {
        private NativeMethods.FDI.Handle fdiHandle;

        // These delegates need to be saved as member variables
        // so that they don't get GC'd.
        private NativeMethods.FDI.PFNALLOC fdiAllocMemHandler;

        private NativeMethods.FDI.PFNFREE fdiFreeMemHandler;
        private NativeMethods.FDI.PFNOPEN fdiOpenStreamHandler;
        private NativeMethods.FDI.PFNREAD fdiReadStreamHandler;
        private NativeMethods.FDI.PFNWRITE fdiWriteStreamHandler;
        private NativeMethods.FDI.PFNCLOSE fdiCloseStreamHandler;
        private NativeMethods.FDI.PFNSEEK fdiSeekStreamHandler;

        private IUnpackStreamContext context;

        private List<ArchiveFileInfo> fileList;

        private int folderId;

        private Predicate<string> filter;

        [SuppressMessage("Microsoft.Security", "CA2106:SecureAsserts")]
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        public CabUnpacker(CabEngine cabEngine)
            : base(cabEngine) {
            fdiAllocMemHandler = CabAllocMem;
            fdiFreeMemHandler = CabFreeMem;
            fdiOpenStreamHandler = CabOpenStream;
            fdiReadStreamHandler = CabReadStream;
            fdiWriteStreamHandler = CabWriteStream;
            fdiCloseStreamHandler = CabCloseStream;
            fdiSeekStreamHandler = CabSeekStream;

            fdiHandle = NativeMethods.FDI.Create(
                fdiAllocMemHandler,
                fdiFreeMemHandler,
                fdiOpenStreamHandler,
                fdiReadStreamHandler,
                fdiWriteStreamHandler,
                fdiCloseStreamHandler,
                fdiSeekStreamHandler,
                NativeMethods.FDI.CPU_80386,
                ErfHandle.AddrOfPinnedObject());
            if (Erf.Error) {
                int error = Erf.Oper;
                int errorCode = Erf.Type;
                ErfHandle.Free();
                throw new CabException(
                    error,
                    errorCode,
                    CabException.GetErrorMessage(error, errorCode, true));
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2106:SecureAsserts")]
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        public bool IsArchive(Stream stream) {
            if (stream == null) {
                throw new ArgumentNullException("stream");
            }

            lock (this) {
                short id;
                int folderCount, fileCount;
                return IsCabinet(stream, out id, out folderCount, out fileCount);
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2106:SecureAsserts")]
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        public IList<ArchiveFileInfo> GetFileInfo(
            IUnpackStreamContext streamContext,
            Predicate<string> fileFilter) {
            if (streamContext == null) {
                throw new ArgumentNullException("streamContext");
            }

            lock (this) {
                context = streamContext;
                filter = fileFilter;
                NextCabinetName = String.Empty;
                fileList = new List<ArchiveFileInfo>();
                bool tmpSuppress = SuppressProgressEvents;
                SuppressProgressEvents = true;
                try {
                    for (short cabNumber = 0;
                        NextCabinetName != null;
                        cabNumber++) {
                        Erf.Clear();
                        CabNumbers[NextCabinetName] = cabNumber;

                        NativeMethods.FDI.Copy(
                            fdiHandle,
                            NextCabinetName,
                            String.Empty,
                            0,
                            CabListNotify,
                            IntPtr.Zero,
                            IntPtr.Zero);
                        CheckError(true);
                    }

                    List<ArchiveFileInfo> tmpFileList = fileList;
                    fileList = null;
                    return tmpFileList.AsReadOnly();
                } finally {
                    SuppressProgressEvents = tmpSuppress;

                    if (CabStream != null) {
                        context.CloseArchiveReadStream(
                            currentArchiveNumber,
                            currentArchiveName,
                            CabStream);
                        CabStream = null;
                    }

                    context = null;
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2106:SecureAsserts")]
        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        public void Unpack(
            IUnpackStreamContext streamContext,
            Predicate<string> fileFilter) {
            lock (this) {
                IList<ArchiveFileInfo> files =
                    GetFileInfo(streamContext, fileFilter);

                ResetProgressData();

                if (files != null) {
                    totalFiles = files.Count;

                    for (int i = 0; i < files.Count; i++) {
                        totalFileBytes += files[i].Length;
                        if (files[i].ArchiveNumber >= this.totalArchives) {
                            int totalArchives = files[i].ArchiveNumber + 1;
                            this.totalArchives = (short) totalArchives;
                        }
                    }
                }

                context = streamContext;
                fileList = null;
                NextCabinetName = String.Empty;
                folderId = -1;
                currentFileNumber = -1;

                try {
                    for (short cabNumber = 0;
                        NextCabinetName != null;
                        cabNumber++) {
                        Erf.Clear();
                        CabNumbers[NextCabinetName] = cabNumber;

                        NativeMethods.FDI.Copy(
                            fdiHandle,
                            NextCabinetName,
                            String.Empty,
                            0,
                            CabExtractNotify,
                            IntPtr.Zero,
                            IntPtr.Zero);
                        CheckError(true);
                    }
                } finally {
                    if (CabStream != null) {
                        context.CloseArchiveReadStream(
                            currentArchiveNumber,
                            currentArchiveName,
                            CabStream);
                        CabStream = null;
                    }

                    if (FileStream != null) {
                        context.CloseFileWriteStream(currentFileName, FileStream, FileAttributes.Normal, DateTime.Now);
                        FileStream = null;
                    }

                    context = null;
                }
            }
        }

        internal override int CabOpenStreamEx(string path, int openFlags, int shareMode, out int err, IntPtr pv) {
            if (CabNumbers.ContainsKey(path)) {
                Stream stream = CabStream;
                if (stream == null) {
                    short cabNumber = CabNumbers[path];

                    stream = context.OpenArchiveReadStream(cabNumber, path, CabEngine);
                    if (stream == null) {
                        throw new FileNotFoundException(String.Format(CultureInfo.InvariantCulture, "Cabinet {0} not provided.", cabNumber));
                    }

                    currentArchiveName = path;
                    currentArchiveNumber = cabNumber;
                    if (this.totalArchives <= currentArchiveNumber) {
                        int totalArchives = currentArchiveNumber + 1;
                        this.totalArchives = (short) totalArchives;
                    }

                    currentArchiveTotalBytes = stream.Length;
                    currentArchiveBytesProcessed = 0;

                    if (folderId != -3) // -3 is a special folderId that requires re-opening the same cab
                    {
                        OnProgress(ArchiveProgressType.StartArchive);
                    }

                    CabStream = stream;
                }

                path = CabStreamName;
            }

            return base.CabOpenStreamEx(path, openFlags, shareMode, out err, pv);
        }

        internal override int CabReadStreamEx(int streamHandle, IntPtr memory, int cb, out int err, IntPtr pv) {
            int count = base.CabReadStreamEx(streamHandle, memory, cb, out err, pv);
            if (err == 0 && CabStream != null) {
                if (fileList == null) {
                    Stream stream = StreamHandles[streamHandle];
                    if (DuplicateStream.OriginalStream(stream) ==
                        DuplicateStream.OriginalStream(CabStream)) {
                        currentArchiveBytesProcessed += cb;
                        if (currentArchiveBytesProcessed > currentArchiveTotalBytes) {
                            currentArchiveBytesProcessed = currentArchiveTotalBytes;
                        }
                    }
                }
            }

            return count;
        }

        internal override int CabWriteStreamEx(int streamHandle, IntPtr memory, int cb, out int err, IntPtr pv) {
            int count = base.CabWriteStreamEx(streamHandle, memory, cb, out err, pv);
            if (count > 0 && err == 0) {
                currentFileBytesProcessed += cb;
                fileBytesProcessed += cb;
                OnProgress(ArchiveProgressType.PartialFile);
            }

            return count;
        }

        internal override int CabCloseStreamEx(int streamHandle, out int err, IntPtr pv) {
            Stream stream = DuplicateStream.OriginalStream(StreamHandles[streamHandle]);

            if (stream == DuplicateStream.OriginalStream(CabStream)) {
                if (folderId != -3) // -3 is a special folderId that requires re-opening the same cab
                {
                    OnProgress(ArchiveProgressType.FinishArchive);
                }

                context.CloseArchiveReadStream(currentArchiveNumber, currentArchiveName, stream);

                currentArchiveName = NextCabinetName;
                currentArchiveBytesProcessed = currentArchiveTotalBytes = 0;

                CabStream = null;
            }

            return base.CabCloseStreamEx(streamHandle, out err, pv);
        }

        /// <summary>
        /// Disposes of resources allocated by the cabinet engine.
        /// </summary>
        /// <param name="disposing">If true, the method has been called directly or indirectly by a user's code,
        /// so managed and unmanaged resources will be disposed. If false, the method has been called by the 
        /// runtime from inside the finalizer, and only unmanaged resources will be disposed.</param>
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        protected override void Dispose(bool disposing) {
            try {
                if (disposing) {
                    if (fdiHandle != null) {
                        fdiHandle.Dispose();
                        fdiHandle = null;
                    }
                }
            } finally {
                base.Dispose(disposing);
            }
        }

        private static string GetFileName(NativeMethods.FDI.NOTIFICATION notification) {
            bool utf8Name = (notification.attribs & (ushort) FileAttributes.Normal) != 0; // _A_NAME_IS_UTF

            // Non-utf8 names should be completely ASCII. But for compatibility with
            // legacy tools, interpret them using the current (Default) ANSI codepage.
            Encoding nameEncoding = utf8Name ? Encoding.UTF8 : Encoding.Default;

            // Find how many bytes are in the string.
            // Unfortunately there is no faster way.
            int nameBytesCount = 0;
            while (Marshal.ReadByte(notification.psz1, nameBytesCount) != 0) {
                nameBytesCount++;
            }

            byte[] nameBytes = new byte[nameBytesCount];
            Marshal.Copy(notification.psz1, nameBytes, 0, nameBytesCount);
            string name = nameEncoding.GetString(nameBytes);
            if (Path.IsPathRooted(name)) {
                name = name.Replace("" + Path.VolumeSeparatorChar, "");
            }

            return name;
        }

        private bool IsCabinet(Stream cabStream, out short id, out int cabFolderCount, out int fileCount) {
            int streamHandle = StreamHandles.AllocHandle(cabStream);
            try {
                Erf.Clear();
                NativeMethods.FDI.CABINFO fdici;
                bool isCabinet = 0 != NativeMethods.FDI.IsCabinet(fdiHandle, streamHandle, out fdici);

                if (Erf.Error) {
                    if (((NativeMethods.FDI.ERROR) Erf.Oper) == NativeMethods.FDI.ERROR.UNKNOWN_CABINET_VERSION) {
                        isCabinet = false;
                    } else {
                        throw new CabException(
                            Erf.Oper,
                            Erf.Type,
                            CabException.GetErrorMessage(Erf.Oper, Erf.Type, true));
                    }
                }

                id = fdici.setID;
                cabFolderCount = fdici.cFolders;
                fileCount = fdici.cFiles;
                return isCabinet;
            } finally {
                StreamHandles.FreeHandle(streamHandle);
            }
        }

        private int CabListNotify(NativeMethods.FDI.NOTIFICATIONTYPE notificationType, NativeMethods.FDI.NOTIFICATION notification) {
            switch (notificationType) {
                case NativeMethods.FDI.NOTIFICATIONTYPE.CABINET_INFO: {
                    string nextCab = Marshal.PtrToStringAnsi(notification.psz1);
                    NextCabinetName = (nextCab.Length != 0 ? nextCab : null);
                    return 0; // Continue
                }
                case NativeMethods.FDI.NOTIFICATIONTYPE.PARTIAL_FILE: {
                    // This notification can occur when examining the contents of a non-first cab file.
                    return 0; // Continue
                }
                case NativeMethods.FDI.NOTIFICATIONTYPE.COPY_FILE: {
                    //bool execute = (notification.attribs & (ushort) FileAttributes.Device) != 0;  // _A_EXEC

                    string name = GetFileName(notification);

                    if (filter == null || filter(name)) {
                        if (fileList != null) {
                            FileAttributes attributes = (FileAttributes) notification.attribs &
                                                        (FileAttributes.Archive | FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System);
                            if (attributes == 0) {
                                attributes = FileAttributes.Normal;
                            }

                            DateTime lastWriteTime;
                            CompressionEngine.DosDateAndTimeToDateTime(notification.date, notification.time, out lastWriteTime);
                            long length = notification.cb;

                            CabFileInfo fileInfo = new CabFileInfo(
                                name,
                                notification.iFolder,
                                notification.iCabinet,
                                attributes,
                                lastWriteTime,
                                length);
                            fileList.Add(fileInfo);
                            currentFileNumber = fileList.Count - 1;
                            fileBytesProcessed += notification.cb;
                        }
                    }

                    totalFiles++;
                    totalFileBytes += notification.cb;
                    return 0; // Continue
                }
            }

            return 0;
        }

        private int CabExtractNotify(NativeMethods.FDI.NOTIFICATIONTYPE notificationType, NativeMethods.FDI.NOTIFICATION notification) {
            switch (notificationType) {
                case NativeMethods.FDI.NOTIFICATIONTYPE.CABINET_INFO: {
                    if (NextCabinetName != null && NextCabinetName.StartsWith("?", StringComparison.Ordinal)) {
                        // We are just continuing the copy of a file that spanned cabinets.
                        // The next cabinet name needs to be preserved.
                        NextCabinetName = NextCabinetName.Substring(1);
                    } else {
                        string nextCab = Marshal.PtrToStringAnsi(notification.psz1);
                        NextCabinetName = (nextCab.Length != 0 ? nextCab : null);
                    }

                    return 0; // Continue
                }
                case NativeMethods.FDI.NOTIFICATIONTYPE.NEXT_CABINET: {
                    string nextCab = Marshal.PtrToStringAnsi(notification.psz1);
                    CabNumbers[nextCab] = notification.iCabinet;
                    NextCabinetName = "?" + NextCabinetName;
                    return 0; // Continue
                }
                case NativeMethods.FDI.NOTIFICATIONTYPE.COPY_FILE: {
                    return CabExtractCopyFile(notification);
                }
                case NativeMethods.FDI.NOTIFICATIONTYPE.CLOSE_FILE_INFO: {
                    return CabExtractCloseFile(notification);
                }
            }

            return 0;
        }

        private int CabExtractCopyFile(NativeMethods.FDI.NOTIFICATION notification) {
            if (notification.iFolder != folderId) {
                if (notification.iFolder != -3) // -3 is a special folderId used when continuing a folder from a previous cab
                {
                    if (folderId != -1) // -1 means we just started the extraction sequence
                    {
                        currentFolderNumber++;
                    }
                }

                folderId = notification.iFolder;
            }

            //bool execute = (notification.attribs & (ushort) FileAttributes.Device) != 0;  // _A_EXEC

            string name = GetFileName(notification);

            if (filter == null || filter(name)) {
                currentFileNumber++;
                currentFileName = name;

                currentFileBytesProcessed = 0;
                currentFileTotalBytes = notification.cb;
                OnProgress(ArchiveProgressType.StartFile);

                DateTime lastWriteTime;
                CompressionEngine.DosDateAndTimeToDateTime(notification.date, notification.time, out lastWriteTime);

                Stream stream = context.OpenFileWriteStream(name, notification.cb, lastWriteTime);
                if (stream != null) {
                    FileStream = stream;
                    int streamHandle = StreamHandles.AllocHandle(stream);
                    return streamHandle;
                }

                fileBytesProcessed += notification.cb;
                OnProgress(ArchiveProgressType.FinishFile);
                currentFileName = null;
            }

            return 0; // Continue
        }

        private int CabExtractCloseFile(NativeMethods.FDI.NOTIFICATION notification) {
            Stream stream = StreamHandles[notification.hf];
            StreamHandles.FreeHandle(notification.hf);

            //bool execute = (notification.attribs & (ushort) FileAttributes.Device) != 0;  // _A_EXEC

            string name = GetFileName(notification);

            FileAttributes attributes = (FileAttributes) notification.attribs &
                                        (FileAttributes.Archive | FileAttributes.Hidden | FileAttributes.ReadOnly | FileAttributes.System);
            if (attributes == 0) {
                attributes = FileAttributes.Normal;
            }

            DateTime lastWriteTime;
            CompressionEngine.DosDateAndTimeToDateTime(notification.date, notification.time, out lastWriteTime);

            stream.Flush();
            context.CloseFileWriteStream(name, stream, attributes, lastWriteTime);
            FileStream = null;

            long remainder = currentFileTotalBytes - currentFileBytesProcessed;
            currentFileBytesProcessed += remainder;
            fileBytesProcessed += remainder;
            OnProgress(ArchiveProgressType.FinishFile);
            currentFileName = null;

            return 1; // Continue
        }
    }
}