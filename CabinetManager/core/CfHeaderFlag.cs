using System;

namespace CabinetManager.core {
    [Flags]
    enum CfHeaderFlag : ushort {
        /// <summary>
        /// is set if this cabinet file is not the first in a set of cabinet files.
        /// When this bit is set, the <see cref="CfCabinet.PreviousCabinetFileName"/> and <see cref="CfCabinet.PreviousCabinetPromptName"/> fields are present in this <see cref="CfCabinet"/>.
        /// </summary>
        CfhdrPrevCabinet = 0x0001,

        /// <summary>
        /// is set if this cabinet file is not the last in a set of cabinet files.
        /// When this bit is set, the <see cref="CfCabinet.NextCabinetFileName"/> and <see cref="CfCabinet.NextCabinetPromptName"/> fields are present in this <see cref="CfCabinet"/>.
        /// </summary>
        CfhdrNextCabinet = 0x0002,

        /// <summary>
        /// is set if this cabinet file contains any reserved fields.
        /// When this bit is set, the <see cref="CfCabinet.CabinetReservedArea"/>, <see cref="CfCabinet.DataReservedAreaSize"/>, and <see cref="CfCabinet.FolderReservedAreaSize"/> fields are present in this <see cref="CfCabinet"/>.
        /// </summary>
        CfhdrReservePresent = 0x0004,
    }
}