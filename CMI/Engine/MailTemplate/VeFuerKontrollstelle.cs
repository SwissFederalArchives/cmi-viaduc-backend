using System.Collections.Generic;
using CMI.Contract.Common;

namespace CMI.Engine.MailTemplate
{
    public class VeFuerKontrollstelle : InElasticIndexierteVe
    {
        public VeFuerKontrollstelle(ElasticArchiveRecord elasticArchiveRecord, int? begruendung) : base(elasticArchiveRecord)
        {
            var idList = new List<int>();
            var hasBegruendung = begruendung != null && begruendung != 0;

            if (hasBegruendung)
            {
                idList.Add(begruendung.Value);
            }

            Begründung = new Stammdaten(idList, "Reason");
            HatPersonendaten = hasBegruendung;
        }


        public Stammdaten Begründung { get; }

        public bool HatPersonendaten { get; }
    }
}