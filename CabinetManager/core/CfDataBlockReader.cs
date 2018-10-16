#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (CfDataReader.cs) is part of CabinetManager.
// 
// CabinetManager is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// CabinetManager is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with CabinetManager. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CabinetManager.core.Exceptions;

namespace CabinetManager.core {
    
    internal class CfDataBlockReader {
        
        private List<CfData> _dataBlocks;

        private int _currentDataBlockNumber = -1;
        private CfData _currentDataBlock;
        private byte[] _currentDataBlockUncompressedData;
        
        private uint _uncompressedFileOffsetToRead;
        private uint _uncompressedFileLengthLeftToRead;
        
        private Dictionary<string, Tuple<uint, uint>> _existingFileInfo = new Dictionary<string, Tuple<uint, uint>>(StringComparer.OrdinalIgnoreCase);

        public CfDataBlockReader(CfFolder folder) {
            _dataBlocks = folder.Data.ToList();
            foreach (var file in folder.Files) {
                if (!_existingFileInfo.ContainsKey(file.RelativePathInCab)) {
                    _existingFileInfo.Add(file.RelativePathInCab, new Tuple<uint, uint>(file.UncompressedFileOffset, file.UncompressedFileSize));
                }
            }
        }

        /// <summary>
        /// Initialize this instance to read the file <paramref name="relativeFilePathInCab"/>.
        /// </summary>
        /// <param name="relativeFilePathInCab"></param>
        public void InitializeToReadFile(string relativeFilePathInCab) {
            if (_existingFileInfo.ContainsKey(relativeFilePathInCab)) {
                _uncompressedFileOffsetToRead = _existingFileInfo[relativeFilePathInCab].Item1;
                _uncompressedFileLengthLeftToRead = _existingFileInfo[relativeFilePathInCab].Item2;
                _currentDataBlockNumber = -1;
            }
        }

        /// <summary>
        /// Read a chunk a uncompressed data for the current file. Store it in <paramref name="buffer"/>, at offset <paramref name="offset"/>
        /// and length <paramref name="length"/>.
        /// The current file is determined using the method <see cref="InitializeToReadFile"/>.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        /// <exception cref="CfCabException"></exception>
        public int ReadUncompressedData(BinaryReader reader, byte[] buffer, int offset, int length) {
            int blockNumberToRead = _currentDataBlockNumber;
            if ((blockNumberToRead = GetBlockNumberToRead(_uncompressedFileOffsetToRead, blockNumberToRead)) >= 0 && _uncompressedFileLengthLeftToRead > 0) {
                if (blockNumberToRead != _currentDataBlockNumber) {
                    _currentDataBlockNumber = blockNumberToRead;
                    _currentDataBlock = _dataBlocks[_currentDataBlockNumber];
                    _currentDataBlockUncompressedData = _currentDataBlock.ReadUncompressedData(reader);
                }
                
                var fileDataOffsetInThisBlockData = (int) _uncompressedFileOffsetToRead - (int) _currentDataBlock.UncompressedDataOffset;
                var fileDataLengthReadInThisBlockData = Math.Min(Math.Min(_currentDataBlockUncompressedData.Length - fileDataOffsetInThisBlockData, (int) _uncompressedFileLengthLeftToRead), length);

                Array.Copy(_currentDataBlockUncompressedData, fileDataOffsetInThisBlockData, buffer, offset, fileDataLengthReadInThisBlockData);
                
                _uncompressedFileOffsetToRead += (uint) fileDataLengthReadInThisBlockData;
                _uncompressedFileLengthLeftToRead -= (uint) fileDataLengthReadInThisBlockData;

                return fileDataLengthReadInThisBlockData;
            }
            
            if (_uncompressedFileLengthLeftToRead > 0) {
                throw new CfCabException($"Failed to read the entire data, {_uncompressedFileLengthLeftToRead} bytes are missing.");
            }
            
            // No more bytes to read
            return 0;
        }
        
        /// <summary>
        /// Find the data block number that contains the data for the offset <paramref name="uncompressedFileOffsetToRead"/> in the
        /// uncompressed stream.
        /// </summary>
        /// <param name="uncompressedFileOffsetToRead"></param>
        /// <param name="startingIndex"></param>
        /// <returns></returns>
        private int GetBlockNumberToRead(uint uncompressedFileOffsetToRead, int startingIndex = 0) {
            var iDataBlock = Math.Max(startingIndex, 0);
            do {
                var dataBlock = _dataBlocks[iDataBlock];

                // find the data block in which the data we want to read starts
                if (dataBlock.UncompressedDataOffset <= uncompressedFileOffsetToRead &&
                    (uncompressedFileOffsetToRead < dataBlock.UncompressedDataOffset + dataBlock.UncompressedDataLength || dataBlock.UncompressedDataLength == 0)) {
                    // the first byte of the uncompressed data will be found in this dataBlock

                    return iDataBlock;
                }
            } while (++iDataBlock < _dataBlocks.Count);
            return -1;
        }
    }
}