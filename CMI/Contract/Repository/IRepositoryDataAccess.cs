using System.Collections.Generic;
using System.IO;
using CMI.Contract.Common;
using DotCMIS.Client;

namespace CMI.Contract.Repository
{
    public interface IRepositoryDataAccess
    {
        /// <summary>
        ///     Gets the folders for a given parent folder.
        ///     The sub collection of folders or files are not filled.
        /// </summary>
        /// <param name="folderId">The parent folder identifier.</param>
        /// <returns>List&lt;RepositoryFolder&gt;.</returns>
        List<RepositoryFolder> GetFolders(string folderId);

        /// <summary>
        ///     Gets the files for a given parent folder.
        ///     The sub collection of folders or files are not filled.
        /// </summary>
        /// <param name="folderId">The folder identifier.</param>
        /// <param name="filePatternsToIgnore">
        ///     A list with regex expressions.
        ///     Filenames that match the expression will be ignored
        /// </param>
        /// <returns>List&lt;RepositoryFile&gt;.</returns>
        List<RepositoryFile> GetFiles(string folderId, List<string> filePatternsToIgnore, out List<RepositoryFile> ignoredFiles);

        /// <summary>
        ///     Gets the repository root folder.
        ///     The package id is stored in the cmis:description field of the folder.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <returns>RepositoryFolder.</returns>
        RepositoryFolder GetRepositoryRoot(string packageId);

        /// <summary>
        ///     Gets a cmis folder by its id.
        /// </summary>
        /// <param name="folderId">The folder identifier.</param>
        /// <returns>RepositoryFolder.</returns>
        IFolder GetCmisFolder(string folderId);

        /// <summary>
        ///     Gets the content of the file as a buffered stream.
        /// </summary>
        /// <param name="fileId">The file identifier.</param>
        /// <returns>Stream.</returns>
        Stream GetFileContent(string fileId);

        /// <summary>
        ///     Gets the name of the repository.
        /// </summary>
        /// <returns>System.String.</returns>
        string GetRepositoryName();
    }
}