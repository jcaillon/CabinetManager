using System;
using System.Collections.Generic;
using System.Text;

namespace CabinetManager {
    class CfHeader {

        private readonly byte[] _signature = new [] { (byte) 0x4D, (byte) 0x53, (byte) 0x43, (byte) 0x46}; /* cab file signature : MSCF */
        private readonly uint _reserved1 = 0; /* reserved = 0 */

        /// <summary>
        /// size of this cabinet file in bytes
        /// </summary>
        private uint  _cbCabinet;

        private readonly uint  _reserved2 = 0; /* reserved = 0 */

        /// <summary>
        /// offset of the first CFFILE entry
        /// </summary>
        private uint  _coffFiles;

        private readonly uint _reserved3 = 0; /* reserved = 0 */
        private readonly byte _versionMinor = 1; /* cabinet file format version, minor */
        private readonly byte _versionMajor = 3; /* cabinet file format version, major */

        /// <summary>
        /// number of CFFOLDER entries in this cabinet
        /// </summary>
        private ushort _cFolders;

        /// <summary>
        /// number of CFFILE entries in this cabinet
        /// </summary>
        private ushort _cFiles;

        /// <summary>
        /// Bit-mapped values that indicate the presence of optional data
        /// </summary>
        private CfHeaderFlag _flags; /* cabinet file option indicators */

        /// <summary>
        /// An arbitrarily derived (random) value that binds a collection of linked cabinet files together.
        /// All cabinet files in a set will contain the same setID. This field is used by cabinet file extractors to assure that cabinet files are not inadvertently mixed.
        /// This value has no meaning in a cabinet file that is not in a set.
        /// </summary>
        private ushort _setId;

        /// <summary>
        /// Sequential number of this cabinet in a multi-cabinet set. The first cabinet has iCabinet=0.
        /// This field, along with setID, is used by cabinet file extractors to assure that this cabinet is the correct continuation cabinet when spanning cabinet files.
        /// </summary>
        private ushort  _iCabinet; /* number of this cabinet file in a set */

        // optional

        private ushort  _cbCfHeader; /* (optional) size of per-cabinet reserved area */
        private byte  _cbCfFolder; /* (optional) size of per-folder reserved area */
        private byte  _cbCfData; /* (optional) size of per-datablock reserved area */

        private byte[]  _abReserve; /* (optional) per-cabinet reserved area */
        private byte[]  _szCabinetPrev; /* (optional) name of previous cabinet file */
        private byte[]  _szDiskPrev; /* (optional) name of previous disk */
        private byte[] _szCabinetNext; /* (optional) name of next cabinet file */
        private byte[]  _szDiskNext; /* (optional) name of next disk */

    }
}