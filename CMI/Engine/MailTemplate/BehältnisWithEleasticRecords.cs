using CMI.Contract.Common;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static CMI.Engine.MailTemplate.DataBuilder;

namespace CMI.Engine.MailTemplate
{
    public class BehältnisWithEleasticRecords : Behältnis
    {
        public List<InElasticIndexierteVe> elasticRecords;
        public BehältnisWithEleasticRecords(List<ElasticArchiveRecord> records, ElasticContainer searchContainer) : base(searchContainer)
        {
            elasticRecords = new List<InElasticIndexierteVe>();
            elasticRecords.AddRange(from record in records
                                         select InElasticIndexierteVe.FromElasticArchiveRecord(record, record));
            elasticRecords = elasticRecords.OrderBy(e => e.Signatur).ToList();
        }

        public List<InElasticIndexierteVe> ElasticRecords => elasticRecords;
    }
}
