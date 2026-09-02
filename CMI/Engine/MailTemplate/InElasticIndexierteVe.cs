using CMI.Contract.Common;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace CMI.Engine.MailTemplate
{
    public class InElasticIndexierteVe : Ve
    {
        private readonly ElasticArchiveRecord elasticArchiveRecord;
        private readonly ElasticArchiveRecord unprotectedElasticArchiveRecord;

        protected InElasticIndexierteVe(ElasticArchiveRecord elasticArchiveRecord, ElasticArchiveRecord unprotectedElasticArchiveRecord)
        {
            this.elasticArchiveRecord = elasticArchiveRecord;
            this.unprotectedElasticArchiveRecord = unprotectedElasticArchiveRecord;
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

        public override string Aktenzeichen => elasticArchiveRecord.Aktenzeichen() ?? "";
        public override string Schutzfristkategorie => elasticArchiveRecord?.ProtectionCategory;
        public override int? Schutzfristdauer => elasticArchiveRecord?.ProtectionDuration;
        public override string Schutzfristende => elasticArchiveRecord?.ProtectionEndDate?.Date.ToString("dd.MM.yyyy");

        public override bool IdDir => !string.IsNullOrEmpty(elasticArchiveRecord?.PrimaryDataLink);

        public override string ZustaendigeStelle => elasticArchiveRecord.ZuständigeStelle() ?? "";

        public override string ZusaetzlicheInformationen => elasticArchiveRecord.ZusätzlicheInformationen() ?? "";
        public override string UnprotectedTitel => unprotectedElasticArchiveRecord.Title;
        public override string UnprotectedDarin => unprotectedElasticArchiveRecord.WithinInfo;
        public override string UnprotectedZusaetzlicheInformationen => unprotectedElasticArchiveRecord.ZusätzlicheInformationen() ?? "";
        public string FrüheresAktenzeichen => elasticArchiveRecord.FrüheresAktenzeichen() ?? "";
        public string ZugaenglichkeitGemaessBga => elasticArchiveRecord?.ZugaenglichkeitGemaessBga() ?? "";
        public string Zusatzmerkmal => elasticArchiveRecord.Zusatzmerkmal() ?? "";

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

        public static InElasticIndexierteVe FromElasticArchiveRecord(ElasticArchiveRecord record, ElasticArchiveRecord unprotectedRecord)
        {
            return new InElasticIndexierteVe(record, unprotectedRecord);
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