namespace CabinetManager {
    public interface IFileToCab {
        /// <summary>
        ///     Need to deploy this file FROM this path
        /// </summary>
        string SourcePath { get; set; }

        /// <summary>
        ///     Path to the pack in which we need to include this file
        /// </summary>
        string CabFilePath { get; set; }

        /// <summary>
        ///     The relative path of the file within the pack
        /// </summary>
        string RelativePathInCab { get; set; }
    }
}