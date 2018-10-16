namespace CabinetManager {
    
    /// <summary>
    /// The progression type.
    /// </summary>
    public enum CabProgressionType : byte {

        /// <summary>Status message after completion of the packing or unpacking an individual file.</summary>
        FileProcessed,

        /// <summary>Status message after completion of the packing or unpacking of an archive.</summary>
        ArchiveCompleted
    }
}