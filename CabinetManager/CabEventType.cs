namespace CabinetManager {
    
    /// <summary>
    /// The progression type.
    /// </summary>
    public enum CabEventType : byte {

        /// <summary>
        /// Published when the archive process progresses, as the cabinet is written on the disc.
        /// </summary>
        /// <remarks>
        /// This event is published for each chunk of data written on the disc when the cabinet is actually saved.
        /// The <see cref="ICabProgressionEventArgs.RelativePathInCab"/> will indicate which file is currently processed.
        /// </remarks>
        GlobalProgression,
        
        /// <summary>
        /// Published when a file has been processed, this can be used to determine which files are actually processed.
        /// </summary>
        /// <remarks>
        /// This event does NOT mean the file is actually stored in the cabinet file on the disc.
        /// This is only to inform that the file has been processed and will be saved in the cabinet file.
        /// Use the <see cref="GlobalProgression"/> to follow the actual writing on disc.
        /// </remarks>
        FileProcessed,

        /// <summary>
        /// Published when a cabinet has been completed and is saved on disc.
        /// </summary>      
        CabinetCompleted
    }
}