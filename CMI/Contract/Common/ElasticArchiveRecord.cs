using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Xml.Serialization;
using CMI.Contract.Common.JsonConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CMI.Contract.Common
{
    /// <summary>
    ///     Informationen zum Archivplan
    /// </summary>
    public class TreeRecord
    {
        public string ArchiveRecordId { get; set; }
        public string Title { get; set; }
        public string ReferenceCode { get; set; }
        public bool IsAnonymized { get; set; }
        public bool IsLeaf { get; set; }
        public string ParentArchiveRecordId { get; set; }
        public string Level { get; set; }
        public long ChildCount { get; set; }
     
        public int TreeSequence { get; set; }
        public List<ElasticArchiveplanContextItem> ArchiveplanContext { get; set; }
        public ElasticTimePeriod CreationPeriod { get; set; }
        public string ExternalDisplayTemplateName { get; set; }
        public List<string> PrimaryDataDownloadAccessTokens { get; set; }
        public List<string> PrimaryDataFulltextAccessTokens { get; set; }
        public List<string> FieldAccessTokens { get; set; }
    }

    /// <summary>
    ///     Informationen zu den Suchresultaten
    /// </summary>
    public class SearchRecord : TreeRecord
    {
        public string PrimaryDataLink { get; set; }
        public string ManifestLink { get; set; }
        public bool CanBeOrdered { get; set; }
        public string WithinInfo { get; set; }

        [JsonConverter(typeof(ExpandoObjectConverter))]
        public dynamic CustomFields { get; set; } = new ExpandoObject();
    }

    /// <summary>
    ///     Informationen zur Detailansicht
    /// </summary>
    public class DetailRecord : SearchRecord
    {
        public string FormerReferenceCode { get; set; }
        public string Extent { get; set; }
        public bool HasImage { get; set; }
        public int AccessionDate { get; set; }
        public List<ElasticParentContentInfo> ParentContentInfos { get; set; }
        public ElasticBase64 Thumbnail { get; set; }
        public ElasticDateWithYear ProtectionEndDate { get; set; }
        public string ProtectionCategory { get; set; }
        public int? ProtectionDuration { get; set; }
    }

    public class ElasticArchiveRecord : DetailRecord
    {
        public ElasticArchiveRecord()
        {
            PrimaryData = new List<ElasticArchiveRecordPackage>();
            References = new List<ElasticReference>();
            Containers = new List<ElasticContainer>();
            Descriptors = new List<ElasticDescriptor>();
            ArchiveplanContext = new List<ElasticArchiveplanContextItem>();
            ParentContentInfos = new List<ElasticParentContentInfo>();
            Places = new List<ElasticPlace>();
        }

        public string All { get; set; }
        public List<string> MetadataAccessTokens { get; set; }
        public bool HasAudioVideo { get; set; }
        public int PlayingLengthInS { get; set; }
        public string TreePath { get; set; }
        public int TreeLevel { get; set; }
        public bool IsRoot { get; set; }
        public List<ElasticPlace> Places { get; set; }
        public string InternalDisplayTemplateName { get; set; }
        public string PreviousArchiveRecordId { get; set; }
        public string NextArchiveRecordId { get; set; }
        public string FirstChildArchiveRecordId { get; set; }
        public bool ContainsPersonRelatedInformation { get; set; }
        public bool IsPhysicalyUsable { get; set; }
        public string Permission { get; set; }
        public string PhysicalUsability { get; set; }
        public string Accessibility { get; set; }
        public List<ElasticDescriptor> Descriptors { get; set; }
        public List<ElasticContainer> Containers { get; set; }
        public List<ElasticReference> References { get; set; }

        [XmlArrayItem("package", IsNullable = false, ElementName = "primaryData")]
        public List<ElasticArchiveRecordPackage> PrimaryData { get; set; }

        public DateTime LastSyncDate { get; set; }
        public ElasticAggregationFields AggregationFields { get; set; }
    }

    /// <summary>
    /// Erweiterung weil eine Anonymisierung vorhanden ist
    /// </summary>
    public class ElasticArchiveDbRecord : ElasticArchiveRecord
    {
        public UnanonymizedFields UnanonymizedFields { get; set; } = new UnanonymizedFields();
    }

    public class PermissionInfo
    {
        public string[] MetadataAccessToken { get; set; }
        public string[] PrimaryDataDownloadAccessTokens { get; set; }
        public string[] PrimaryDataFulltextAccessTokens { get; set; }
        public string[] FieldAccessTokens { get; set; }
    }

    public class ElasticAggregationFields
    {
        public string Bestand { get; set; }
        public List<string> Ordnungskomponenten { get; set; }
        public bool HasPrimaryData { get; set; }
        public List<int> CreationPeriodYears001 { get; set; }
        public List<int> CreationPeriodYears005 { get; set; }
        public List<int> CreationPeriodYears010 { get; set; }
        public List<int> CreationPeriodYears025 { get; set; }
        public List<int> CreationPeriodYears100 { get; set; }
    }

    public class UnanonymizedFields
    {
        public string Title { get; set; }
        public string WithinInfo { get; set; }
        /// <summary>
        /// Feld heisst ZusätzlicheInformationen an anderer Stelle
        /// </summary>
        public string BemerkungZurVe { get; set; }
        /// <summary>
        /// Feld heisst Zusatzmerkmal an anderer Stelle
        /// </summary>
        public string ZusatzkomponenteZac1 { get; set; }
        public string VerwandteVe { get; set; } 
        public List<ElasticArchiveplanContextItem> ArchiveplanContext { get; set; } = new List<ElasticArchiveplanContextItem>();
        public List<ElasticParentContentInfo> ParentContentInfos { get; set; } = new List<ElasticParentContentInfo>();
        public List<ElasticReference> References { get; set; } = new List<ElasticReference>();
    }

    public class ElasticTimePeriod
    {
        public ElasticTimePeriod()
        {
            Years = new List<int>();
        }

        public string Text { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime SearchStartDate { get; set; }
        public DateTime SearchEndDate { get; set; }
        public bool StartDateApproxIndicator { get; set; }
        public bool EndDateApproxIndicator { get; set; }
        public string StartDateText { get; set; }
        public string EndDateText { get; set; }
        public List<int> Years { get; set; }
    }

    public class ElasticReference
    {
        public string ReferenceName { get; set; }
        public string Role { get; set; }
        public string ArchiveRecordId { get; set; }
        public bool Protected { get; set; }
    }

    public class ElasticContainer
    {
        public string ContainerLocation { get; set; }
        public string ContainerType { get; set; }
        public string IdName { get; set; }
        public string ContainerCode { get; set; }
        public string ContainerCarrierMaterial { get; set; }
    }

    public static class ElasticContainerExtensions
    {
        public static string GetBand(this ElasticContainer container)
        {
            if (container?.ContainerCode == null)
            {
                return string.Empty;
            }

            var parts = container.ContainerCode.Split('_');

            if (parts.Length < 2 || string.IsNullOrEmpty(parts[parts.Length - 1]))
            {
                return container.ContainerCode; // Fallback falls das Band nicht ermittelt werden konnte (Gemäss Marlies Hertig)
            }

            return parts[parts.Length - 1];
        }
    }

    public class ElasticDescriptor
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string OtherLanguageNames { get; set; }
        public string IdName { get; set; }
        public List<string> SeeAlso { get; set; }
        public string Function { get; set; }
        public string Thesaurus { get; set; }
        public string Source { get; set; }
    }

    public class ElasticDateWithYear
    {
        public DateTime Date { get; set; }
        public int Year { get; set; }
    }

    public class ElasticBase64
    {
        public string Value { get; set; }
        public string MimeType { get; set; }
    }

    public class ElasticArchiveplanContextItem
    {
        public int IconId { get; set; }
        public string Level { get; set; }
        public string RefCode { get; set; }
        public string Title { get; set; }
        public string DateRangeText { get; set; }
        public string ArchiveRecordId { get; set; }
        public bool Protected { get; set; }
    }

    public class ElasticParentContentInfo
    {
        public string Title { get; set; }
        public string WithinInfo { get; set; }
    }

    public class ElasticPlace
    {
        public string Name { get; set; }
        public float Longitude { get; set; }
        public float Latitude { get; set; }
    }

    public class ElasticArchiveRecordPackage
    {
        public ElasticArchiveRecordPackage()
        {
            Items = new List<ElasticRepositoryObject>();
        }

        public string FileFormatsInpackage { get; set; }
        public long SizeInBytes { get; set; }
        public int FileCount { get; set; }
        public string PackageId { get; set; }

        [JsonConverter(typeof(LongToTimeSpanConverter))]
        public TimeSpan? RepositoryExtractionDuration { get; set; }

        [JsonConverter(typeof(LongToTimeSpanConverter))]
        public TimeSpan? FulltextExtractionDuration { get; set; }

        public List<ElasticRepositoryObject> Items { get; set; }
    }

    public class ElasticRepositoryObject
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public ElasticRepositoryObjectType Type { get; set; }
        public string RepositoryId { get; set; }
        public string Content { get; set; }
        public string Hash { get; set; }
        public string HashAlgorithm { get; set; }
        public long SizeInBytes { get; set; }
        public string MimeType { get; set; }
        public string LogicalName { get; set; }
    }

    public enum ElasticRepositoryObjectType
    {
        File,
        Folder
    }


    public class ElasticHyperlink
    {
        public string Text { get; set; }
        public string Url { get; set; }
    }

    public class ElasticFloat
    {
        public float Value { get; set; }
        public int DecimalPositions { get; set; }
        public string Text { get; set; }
    }

    public class ElasticEntityLink
    {
        public string Value { get; set; }
        public string EntityRecordId { get; set; }
        public string EntityType { get; set; }
    }

    public class ArchiveRecordContextItem
    {
        public string Level { get; set; }
        public string Title { get; set; }
        public string DateRangeText { get; set; }
        public string ArchiveRecordId { get; set; }
        public bool Protected { get; set; }
        public string ReferenceCode { get; set; }
    }
}