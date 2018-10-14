namespace Oetools.Utilities.Archive {
    public enum CabProgressionType : byte {

        /// <summary>Status message after completion of the packing or unpacking an individual file.</summary>
        FinishFile,

        /// <summary>Status message after completion of the packing or unpacking of an archive.</summary>
        FinishArchive
    }
}