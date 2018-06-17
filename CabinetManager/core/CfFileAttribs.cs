using System;

namespace CabinetManager.core {
    [Flags]
    enum CfFileAttribs : ushort {
        None = 0,
        Rdonly = 0x01, /* file is read-only */
        Hiddden = 0x02, /* file is hidden */
        System = 0x04, /* file is a system file */
        Arch = 0x20, /* file modified since last backup */
        Exec = 0x40, /* run after extraction */
        NameIsUtf = 0x80 /* szName[] contains UTF */
    }
}