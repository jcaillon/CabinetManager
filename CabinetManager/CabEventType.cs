namespace CabinetManager {
    
    /// <summary>
    /// The progression type.
    /// </summary>
    public enum CabEventType : byte {

        /// <summary>
        /// Published when the archive process progresses.
        /// </summary>
        GlobalProgression,
        
        /// <summary>
        /// Published when a file has been completed.
        /// </summary>
        FileCompleted,

        /// <summary>
        /// Published when a cabinet has been completed.
        /// </summary>      
        CabinetCompleted
    }
}