using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using CMI.Contract.Common;

namespace CMI.Engine.MailTemplate
{
    public class InElasticIndexierteVe : Ve
    {
        private readonly ElasticArchiveRecord elasticArchiveRecord;

        protected InElasticIndexierteVe(ElasticArchiveRecord elasticArchiveRecord)
        {
            this.elasticArchiveRecord = elasticArchiveRecord;
        }

        public override string TeilBestand
        {
            get
            {
                var archiveplanContextItem = elasticArchiveRecord.ArchiveplanContext.FirstOrDefault(p => p.Level == "Teilbestand") ??
                                             elasticArchiveRecord.ArchiveplanContext.FirstOrDefault(p => p.Level == "Bestand");

                return archiveplanContextItem == null ? string.Empty : archiveplanContextItem.RefCode;
            }
        }

        public override string TitelTeilBestand
        {
            get
            {
                var archiveplanContextItem = elasticArchiveRecord.ArchiveplanContext.FirstOrDefault(p => p.Level == "Teilbestand") ??
                                             elasticArchiveRecord.ArchiveplanContext.FirstOrDefault(p => p.Level == "Bestand");

                return archiveplanContextItem == null ? string.Empty : archiveplanContextItem.Title;
            }
        }


        public override string Signatur => elasticArchiveRecord.ReferenceCode;

        public override string Titel => elasticArchiveRecord.Title;

        public override string Darin => elasticArchiveRecord.WithinInfo;

        public override string Id => elasticArchiveRecord.ArchiveRecordId;

        public override bool IstFreiZugänglich => elasticArchiveRecord.PrimaryDataDownloadAccessTokens.Contains(AccessRoles.RoleOe2);

        public override string Level => elasticArchiveRecord.Level;

        public override string Entstehungszeitraum => elasticArchiveRecord.CreationPeriod.Text;

        public override string Aktenzeichen
        {
            get
            {
                var stringBuilder = new StringBuilder();
                foreach (var az in elasticArchiveRecord.CustomFields.aktenzeichen)
                {
                    if (stringBuilder.Length > 0)
                    {
                        stringBuilder.Append(", ");
                    }

                    stringBuilder.Append(az.ToString());
                }

                return stringBuilder.ToString();
            }
        }

        public override string Schutzfristkategorie => elasticArchiveRecord?.ProtectionCategory;
        public override int? Schutzfristdauer => elasticArchiveRecord?.ProtectionDuration;
        public override string Schutzfristende => elasticArchiveRecord?.ProtectionEndDate?.Date.ToString("dd.MM.yyyy");

        public override bool IdDir => !string.IsNullOrEmpty(elasticArchiveRecord?.PrimaryDataLink);

        public override string ZustaendigeStelle => elasticArchiveRecord.ZuständigeStelle() ?? "";

        public override string ZusaetzlicheInformationen => elasticArchiveRecord.ZusätzlicheInformationen() ?? "";


        public override Behältnis[] Behältnisse
        {
            get { return elasticArchiveRecord.Containers.Select(c => new Behältnis(c)).ToArray(); }
        }

        public override string BehältnisCodesText
        {
            get { return string.Join("; ", Behältnisse.Select(c => c.Code)); }
        }

        public override string Band
        {
            get
            {
                var bandEnumerable = elasticArchiveRecord.Containers.Select(c => c.GetBand());
                return string.Join(" / ", bandEnumerable);
            }
        }

        public override string Bestand
        {
            get
            {
                var archiveplanContextItem = elasticArchiveRecord.ArchiveplanContext.FirstOrDefault(p => p.Level == "Bestand");
                return archiveplanContextItem == null ? string.Empty : archiveplanContextItem.RefCode;
            }
        }

        public override string TitelBestand
        {
            get
            {
                var archiveplanContextItem = elasticArchiveRecord.ArchiveplanContext.FirstOrDefault(p => p.Level == "Bestand");
                return archiveplanContextItem == null ? string.Empty : archiveplanContextItem.Title;
            }
        }

        public override string Ablieferung => elasticArchiveRecord.Ablieferung();

        public static InElasticIndexierteVe FromElasticArchiveRecord(ElasticArchiveRecord r)
        {
            return new InElasticIndexierteVe(r);
        }


        public static bool DoesPropertyExist(dynamic settings, string name)
        {
            if (settings is ExpandoObject)
            {
                return ((IDictionary<string, object>) settings).ContainsKey(name);
            }

            return settings.GetType().GetProperty(name) != null;
        }
    }
}