using System;
using System.Collections.Generic;
using System.Text;

namespace CabinetManager
{
    [Flags]
    enum CfHeaderFlag : ushort {

        /// <summary>
        /// flags.cfhdrPREV_CABINET is set if this cabinet file is not the first in a set of cabinet files.
        /// When this bit is set, the szCabinetPrev and szDiskPrev fields are present in this CFHEADER.
        /// </summary>
        CfhdrPrevCabinet = 0x0001,

        /// <summary>
        /// flags.cfhdrNEXT_CABINET is set if this cabinet file is not the last in a set of cabinet files.
        /// When this bit is set, the szCabinetNext and szDiskNext fields are present in this CFHEADER.
        /// </summary>
        CfhdrNextCabinet = 0x0002,

        /// <summary>
        /// flags.cfhdrRESERVE_PRESENT is set if this cabinet file contains any reserved fields.
        /// When this bit is set, the cbCFHeader, cbCFFolder, and cbCFData fields are present in this CFHEADER.
        /// </summary>
        CfhdrReservePresent = 0x0004,
    }
}
