using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Access.Sql.Viaduc.AblieferndeStellen;
using CMI.Contract.Common;
using CMI.Engine.MailTemplate;
using CMI.Utilities.Template;
using CMI.Web.Common.api;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api;
using CMI.Web.Frontend.api.Controllers;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Interfaces;
using IBus = MassTransit.IBus;

namespace CMI.Web.Frontend.Helpers
{
    /// <summary>
    ///     Die Klasse dient zur automatischen E-Mail-Benachrichtigung der Kontrollstellen über Einsichtnahmen von VE in SF
    ///     durch AS-Benutzer
    /// </summary>
    public class KontrollstellenInformer : IKontrollstellenInformer
    {
        private readonly IBus bus;
        private readonly IElasticService elasticService;
        private readonly AutomatischeBenachrichtigungAnKontrollstelle template;

        public KontrollstellenInformer(IBus bus, IElasticService elasticService,
            AutomatischeBenachrichtigungAnKontrollstelle template)
        {
            this.bus = bus;
            this.elasticService = elasticService;
            this.template = template;
        }

        public async Task InformIfNecessary(UserAccess userAccess, IList<VeInfo> veInfoList)
        {
            if (userAccess.RolePublicClient != AccessRoles.RoleAS)
            {
                return;
            }

            var archiveRecordIdList = veInfoList.Select(ve => ve.VeId).ToList();
            var entityResult = elasticService.QueryForIds<ElasticArchiveRecord>(archiveRecordIdList, userAccess,
                new Paging {Take = ElasticService.ELASTIC_SEARCH_HIT_LIMIT, Skip = 0});

            if (entityResult.Status != (int) HttpStatusCode.OK)
            {
                throw new SearchException("Elastic query not successful. For more information see viaduc log.");
            }

            var filteredArchiveRecords = entityResult.Entries.Select(entity => entity.Data)
                .Where(ear => ApiFrontendControllerBase.CouldNeedAReason(ear, userAccess)).ToList();

            if (filteredArchiveRecords.Count == 0)
            {
                return;
            }

            var dataAccess = new AblieferndeStelleDataAccess(WebHelper.Settings["sqlConnectionString"]);
            var ablieferndeStelleList = dataAccess.GetAllAblieferndeStelle().ToList();
            var archiveRecordListProKontrollstellen = new Dictionary<string, List<ElasticArchiveRecord>>();

            foreach (var archiveRecord in filteredArchiveRecords)
            {
                var tokensVeUser = userAccess.AsTokens.Intersect(archiveRecord.PrimaryDataDownloadAccessTokens).ToList();

                // Auch wenn nur ein Token übereinstimmt ist dies ein Treffer (gemäss Marlies Hertig)
                foreach (var ablieferndeStelle in ablieferndeStelleList.Where(stelle => stelle.AblieferndeStelleTokenList.Any(t =>
                    tokensVeUser.Contains(t.Token) &&
                    stelle.Kontrollstellen.Count > 0)))
                {
                    var kontrollstellen = string.Join(",", ablieferndeStelle.Kontrollstellen.OrderBy(e => e));

                    if (archiveRecordListProKontrollstellen.TryGetValue(kontrollstellen, out var archiveRecordList))
                    {
                        archiveRecordList.Add(archiveRecord);
                    }
                    else
                    {
                        archiveRecordListProKontrollstellen[kontrollstellen] = new List<ElasticArchiveRecord> {archiveRecord};
                    }
                }
            }

            foreach (var entry in archiveRecordListProKontrollstellen)
            {
                await Inform(entry.Key, entry.Value, veInfoList, userAccess.UserId);
            }
        }

        private async Task Inform(string kontrollstellen, IEnumerable<ElasticArchiveRecord> relevanteArchiveRecords, IList<VeInfo> veInfoList,
            string userId)
        {
            var dataBuilder = new DataBuilder(bus);
            var mailHelper = new MailHelper();
            var veList = new List<InElasticIndexierteVe>();

            foreach (var archiveRecord in relevanteArchiveRecords)
            {
                var veInfo = veInfoList.First(e => e.VeId.ToString() == archiveRecord.ArchiveRecordId);
                var ve = new VeFuerKontrollstelle(archiveRecord, veInfo.BegruendungId);

                veList.Add(ve);
            }

            var dataContext = dataBuilder
                .AddValue("To", kontrollstellen)
                .AddUser(userId)
                .AddVeList(veList)
                .Create();

            await mailHelper.SendEmail(bus, template, dataContext);
        }
    }


    public class VeInfo
    {
        public VeInfo(int veId, int? begruendungId)
        {
            VeId = veId;
            BegruendungId = begruendungId;
        }

        public int VeId { get; }

        public int? BegruendungId { get; }
    }


    public class SearchException : Exception
    {
        public SearchException(string friendlyName) : base(friendlyName)
        {
        }
    }
}