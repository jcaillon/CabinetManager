namespace CabinetManager {
    
    /// <summary>
    /// Sent through the <see cref="ICabManager.OnProgress"/> event.
    /// </summary>
    public interface ICabProgressionEventArgs {
        
        /// <summary>
        /// The type of event.
        /// </summary>
        CabEventType EventType { get; }

        /// <summary>
        /// The path of the cabinet file concerned by this event.
        /// </summary>
        string CabPath { get; }

        /// <summary>
        /// The relative path, within the cabinet file, concerned by this event.
        /// </summary>
        string RelativePathInCab { get; }

        /// <summary>
        /// The total percentage already done for the current process, from 0 to 100.
        /// </summary>
        /// <remarks>
        /// This is a TOTAL percentage for the current process, it is not a number for a single file or a single cabinet file.
        /// </remarks>
        double PercentageDone { get; }
    }
}