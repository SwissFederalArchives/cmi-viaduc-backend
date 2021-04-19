using CMI.Contract.Common.Gebrauchskopie;
using CMI.Manager.Asset.ParameterSettings;

namespace CMI.Manager.Asset
{
    /// <summary>
    ///     <para>
    ///         The interface provides methods to process scanned documents.
    ///     </para>
    /// </summary>
    public interface IScanProcessor
    {
        /// <summary>
        ///     <para>
        ///         Converts single page jpeg2000 Scans found within the package into (multi-paged) pdf documents.
        ///         Per document or dossier (with direct dateiRef's) one pdf is created. The metadata information in the package is
        ///         updated to reflect the changes made.
        ///     </para>
        ///     <para>The following assumptions are made:</para>
        ///     <list type="bullet">
        ///         <item>JPEG 2000 Files have the extension .jp2</item>
        ///         <item>
        ///             Within one document or (dossier with dateiRef) only .jp2 files are allowed. If other file types are mixed
        ///             in, the conversion silently fails for that document.
        ///         </item>
        ///     </list>
        /// </summary>
        /// <param name="paket">The package to be converted</param>
        /// <param name="folder">The root folder where the files can be found.</param>
        /// <param name="settings">The conversion settings</param>
        void ConvertSingleJpeg2000ScansToPdfDocuments(PaketDIP paket, string folder, ScansZusammenfassenSettings settings);
    }
}