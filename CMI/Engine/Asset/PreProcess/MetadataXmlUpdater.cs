using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CMI.Contract.Common.Gebrauchskopie;

namespace CMI.Engine.Asset.PreProcess
{
    /// <summary>
    ///     Die Klasse führt das Metadata.xml bzw. PaketDIP nach
    /// </summary>
    public class MetadataXmlUpdater
    {
        public static void AddFile(FileInfo file, DateiParents parents)
        {
            var id = Guid.NewGuid().ToString("N");
            var datei = new DateiDIP
            {
                Id = id,
                Name = file.Name,
                OriginalName = file.Name,
                Pruefalgorithmus = Pruefalgorithmus.MD5,
                Pruefsumme = CalculateMd5(file)
            };

            if (parents.DossierOderDokument is DokumentDIP dokumentDIP)
            {
                dokumentDIP.DateiRef.Add(id);
            }
            else if (parents.DossierOderDokument is DossierDIP dossierDIP)
            {
                dossierDIP.DateiRef.Add(id);
            }

            GetDateiList(parents.OrdnerOderInhaltverzeinis).Add(datei);
        }

        public static DateiParents RemoveFile(FileInfo file, PaketDIP paket, string tempFolder)
        {
            var datei = GetDatei(file, paket, tempFolder, out var ordnerOderInhaltverzeinis);
            var parents = new DateiParents
            {
                OrdnerOderInhaltverzeinis = ordnerOderInhaltverzeinis
            };

            GetDateiList(ordnerOderInhaltverzeinis).Remove(datei);

            foreach (var ordnungssystemposition in paket.Ablieferung.Ordnungssystem.Ordnungssystemposition)
            {
                var dossierOderDokument = RemoveDateiRef(ordnungssystemposition, datei.Id);
                if (dossierOderDokument != null)
                {
                    parents.DossierOderDokument = dossierOderDokument;
                }
            }

            return parents;
        }

        public static void UpdateFile(FileInfo file, FileInfo newFile, PaketDIP paket, string tempFolder)
        {
            var datei = GetDatei(file, paket, tempFolder, out var _);

            if (datei == null)
            {
                if (paket.Ablieferung.Bemerkung != "Metadata.xml das nicht zum Inhalt passt für Testsysteme")
                {
                    throw new Exception($"Im Metadata.xml wurde für die Datei '{file.FullName}' kein Eintrag gefunden.");
                }

                return;
            }

            datei.Name = newFile.Name;
            datei.Pruefsumme = CalculateMd5(newFile);
            datei.Pruefalgorithmus = Pruefalgorithmus.MD5;
            datei.Eigenschaft.Clear();
        }


        public static DateiDIP GetDatei(FileInfo file, PaketDIP paket, string tempFolder, out object ordnerOderInhaltsverzeichnis)
        {
            var fileWithShortPath = file.FullName.Remove(0, tempFolder.Length);
            var fileNameParts = fileWithShortPath.Split(new[] {'\\'}, StringSplitOptions.RemoveEmptyEntries);
            var ordnerOderInhaltsverzeichnisList = new List<object> {paket.Inhaltsverzeichnis};
            ordnerOderInhaltsverzeichnis = ordnerOderInhaltsverzeichnisList.FirstOrDefault();

            for (var i = 0; i < fileNameParts.Length - 1; i++)
            {
                var fileNamePart = fileNameParts[i];
                ordnerOderInhaltsverzeichnisList = FindOrdner(fileNamePart, ordnerOderInhaltsverzeichnisList).Cast<object>().ToList();
                // If we can't find a folder with the given name, then certainly we don't find a file --> Can stop here
                if (!ordnerOderInhaltsverzeichnisList.Any())
                {
                    return null;
                }
            }

            var dateien = FindDateiInOrdnerList(fileNameParts.Last(), ordnerOderInhaltsverzeichnisList);
            // Actually only one file could/should be found. But in case
            if (dateien.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Found more than one file with the name {fileNameParts.Last()} with the same path <{fileWithShortPath}>. This is not possible.");
            }

            // As an output we have to set the folder or root where the file was found
            ordnerOderInhaltsverzeichnis = ordnerOderInhaltsverzeichnisList.FirstOrDefault(o =>
            {
                switch (o)
                {
                    case InhaltsverzeichnisDIP inhaltsVerzeichnis:
                        return inhaltsVerzeichnis.Datei.Any(d => d.Name.Equals(fileNameParts.Last(), StringComparison.InvariantCultureIgnoreCase));
                    case OrdnerDIP ordner:
                        return ordner.Datei.Any(d => d.Name.Equals(fileNameParts.Last(), StringComparison.InvariantCultureIgnoreCase));
                    default:
                        return false;
                }
            });

            return dateien.FirstOrDefault();
        }

        private static List<OrdnerDIP> FindOrdner(string orderName, IEnumerable<object> ordnerOderInhaltsverzeichnisList)
        {
            var retVal = new List<OrdnerDIP>();
            foreach (var ordner in ordnerOderInhaltsverzeichnisList)
            {
                retVal.AddRange(GetOrdnerList(ordner).Where(o => o.Name.Equals(orderName, StringComparison.InvariantCultureIgnoreCase)));
            }

            return retVal;
        }

        private static List<DateiDIP> FindDateiInOrdnerList(string dateiName, List<object> ordnerOderInhaltsverzeichnisListe)
        {
            var retVal = new List<DateiDIP>();
            foreach (var ordnerOderInhaltsverzeichnis in ordnerOderInhaltsverzeichnisListe)
            {
                retVal.AddRange(GetDateiList(ordnerOderInhaltsverzeichnis)
                    .Where(o => o.Name.Equals(dateiName, StringComparison.InvariantCultureIgnoreCase)));
            }

            return retVal;
        }


        private static List<OrdnerDIP> GetOrdnerList(object ordnerContainer)
        {
            if (ordnerContainer is InhaltsverzeichnisDIP)
            {
                return ((InhaltsverzeichnisDIP) ordnerContainer).Ordner;
            }

            return ((OrdnerDIP) ordnerContainer).Ordner;
        }

        private static List<DateiDIP> GetDateiList(object dateiContainer)
        {
            if (dateiContainer is InhaltsverzeichnisDIP)
            {
                return ((InhaltsverzeichnisDIP) dateiContainer).Datei;
            }

            return ((OrdnerDIP) dateiContainer).Datei;
        }


        private static object RemoveDateiRef(OrdnungssystempositionDIP ordnungssystemposition, string dateiRef)
        {
            foreach (var dossier in ordnungssystemposition.Dossier)
            {
                var dossierOderDokument = RemoveDateiRef(dossier, dateiRef);
                if (dossierOderDokument != null)
                {
                    return dossierOderDokument;
                }
            }

            foreach (var ordnungssystemSubPosition in ordnungssystemposition.Ordnungssystemposition)
            {
                var dossierOderDokument = RemoveDateiRef(ordnungssystemSubPosition, dateiRef);
                if (dossierOderDokument != null)
                {
                    return dossierOderDokument;
                }
            }

            return null;
        }

        private static object RemoveDateiRef(DossierDIP dossier, string dateiRef)
        {
            if (dossier.DateiRef.Remove(dateiRef))
            {
                return dossier;
            }

            foreach (var dokument in dossier.Dokument)
            {
                if (dokument.DateiRef.Remove(dateiRef))
                {
                    return dokument;
                }
            }

            foreach (var subDossier in dossier.Dossier)
            {
                var dossierOderDokument = RemoveDateiRef(subDossier, dateiRef);
                if (dossierOderDokument != null)
                {
                    return dossierOderDokument;
                }
            }

            return null;
        }


        private static string CalculateMd5(FileInfo file)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = file.OpenRead())
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }


    /// <summary>
    ///     <para>
    ///         A structure containing information about references of a removed file.<br />If a file is removed, then it is
    ///         removed from the Inhaltsverzeichnis or a Ordner within the Inhaltsverzeichnis.<br />And then the DateiRef is
    ///         removed either from the Dossier or Dokument.
    ///     </para>
    /// </summary>
    public struct DateiParents
    {
        public object DossierOderDokument { get; set; }
        public object OrdnerOderInhaltverzeinis { get; set; }
    }
}