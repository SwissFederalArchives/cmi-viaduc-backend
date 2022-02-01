namespace CMI.Contract.Common
{
    /// <summary>
    /// Base class used for extracting or converting files, with the possibility to split
    /// large (pdf) files in smaller parts.
    /// </summary>
    public abstract class AssetFileBase
    {
        /// <summary>
        /// The original id of the file as defined in the source.
        /// This id is null, if the file is generated as a result of a split
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The name of the file of the disk
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// The parent id contains the id of the "original" file in case this file is
        /// generated as a result of a split
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// The order of the splitted file parts
        /// </summary>
        public int SplitPartNumber { get; set; }
    }
}