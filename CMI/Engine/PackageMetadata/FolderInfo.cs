using System.Collections.Generic;
using System.Linq;
using DotCMIS.Client;
using Newtonsoft.Json;
using Serilog;

namespace CMI.Engine.PackageMetadata
{
    /// <summary>
    ///     Helper object to store information about the folders from the repository.
    ///     It also helps to link the repository folders (cmisFolderType) to the
    ///     DIP entities Ablieferungen, Ordnungssystempositionen, Dossiers, Dokumente
    /// </summary>
    public class FolderInfo
    {
        /// <summary>
        ///     Gets or sets the identifier of the DIP Package entity.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <summary>
        ///     Gets or sets the cmis folder.
        /// </summary>
        /// <value>The cmis folder.</value>
        [JsonIgnore]
        public IFolder CmisFolder { get; set; }

        /// <summary>
        ///     Gets or sets the type of the folder. The folder type is stored in the metadata of the cmis folder object
        /// </summary>
        /// <value>The type of the folder.</value>
        public PackageFolderType FolderType { get; set; }

        /// <summary>
        ///     Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        public FolderInfo Parent { get; set; }

        /// <summary>Gets or sets a value indicating whether this is the ordered item.</summary>
        /// <value>
        ///     <c>true</c> if this instance is ordered item; otherwise, <c>false</c>.
        /// </value>
        public bool IsOrderedItem { get; set; }

        /// <summary>Gets or sets a value indicating whether this is a child of the ordered item.</summary>
        /// <value>
        ///     <c>true</c> if this instance is child of ordered item; otherwise, <c>false</c>.
        /// </value>
        public bool IsChildOfOrderedItem { get; set; }
    }

    /// <summary>
    ///     A collection of FolderInfo objects. Allows to get the children of a parent.
    /// </summary>
    /// <seealso cref="System.Collections.Generic.List{CMI.Engine.PackageMetadata.FolderInfo}" />
    public class FolderInfoList : List<FolderInfo>
    {
        public List<FolderInfo> GetChildren(FolderInfo folder)
        {
            Log.Verbose("Getting children for folder with id {Id}", folder.Id);

            return this.Where(f => f.Parent?.CmisFolder?.Id == folder.CmisFolder.Id).ToList();
        }
    }
}