namespace CabinetManager.core {
    enum CfFolderTypeCompress : ushort {           
        None = 0x0000,

        /// <summary>
        /// see https://msdn.microsoft.com/en-us/library/cc483131.aspx
        /// </summary>
        MsZip = 0x0001,
        Quantum = 0x0002,

        /// <summary>
        /// see https://msdn.microsoft.com/en-us/library/cc483133.aspx
        /// </summary>
        Lzx = 0x0003,
        Bad = 0x000F,

        //MASK_TYPE = 0x000F,
        //MASK_LZX_WINDOW = 0x1F00,
        //LZX_WINDOW_LO = 0x0F00,
        //LZX_WINDOW_HI = 0x1500,
        //SHIFT_LZX_WINDOW = 0x0008,
        //
        //MASK_QUANTUM_LEVEL = 0x00F0,
        //QUANTUM_LEVEL_LO = 0x0010,
        //QUANTUM_LEVEL_HI = 0x0070,
        //SHIFT_QUANTUM_LEVEL = 0x0004,
        //
        //MASK_QUANTUM_MEM = 0x1F00,
        //QUANTUM_MEM_LO = 0x0A00,
        //QUANTUM_MEM_HI = 0x1500,
        //SHIFT_QUANTUM_MEM = 0x0008,
        //
        //MASK_RESERVED = 0xE000
    }
}