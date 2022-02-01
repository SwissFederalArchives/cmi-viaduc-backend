using System;
using System.Linq;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Messaging;
using CMI.Manager.Order.Status;
using MassTransit;

namespace CMI.Manager.Order.Consumers
{
    public static class UpdateIndivTokensHelper
    {
        public static async Task SendToIndexManager(RecalcIndivTokens recalcTokens, IOrderDataAccess dataAccess,
            ISendEndpointProvider sendEndpointProvider, Uri uri)
        {
            var indivTokens = await dataAccess.GetIndividualAccessTokens(recalcTokens.ArchiveRecordId);

            // Mix the existing tokens with the indiv tokens
            var download = recalcTokens.ExistingPrimaryDataDownloadAccessTokens.Where(IsNotIndivToken)
                .Union(indivTokens.PrimaryDataDownloadAccessTokens).Distinct().ToArray();
            var fulltext = recalcTokens.ExistingPrimaryDataFulltextAccessTokens.Where(IsNotIndivToken)
                .Union(indivTokens.PrimaryDataFulltextAccessTokens).Distinct().ToArray();
            var metadata = recalcTokens.ExistingMetadataAccessTokens.Where(IsNotIndivToken).Union(indivTokens.MetadataAccessTokens).Distinct()
                .ToArray();


            var ep = await sendEndpointProvider.GetSendEndpoint(new Uri(uri, BusConstants.IndexManagerUpdateIndivTokensMessageQueue));
            await ep.Send(new UpdateIndivTokens
            {
                ArchiveRecordId = recalcTokens.ArchiveRecordId,
                CombinedPrimaryDataFulltextAccessTokens = fulltext,
                CombinedPrimaryDataDownloadAccessTokens = download,
                CombinedMetadataAccessTokens = metadata
            });
        }

        /// <summary>
        ///     Sorgt dafür, dass nach dem Commit in die Datenbank ein "refresh"
        ///     der individuellen AccessTokens durchgeführt wird.
        /// </summary>
        /// <param name="auftragStatus"></param>
        public static void RegisterActionForIndivTokensRefresh(AuftragStatus auftragStatus)
        {
            // die folgenden lokalen Variabeln sind notwendig,
            // weil später beim Ausführen der PostCommitAction
            // die Property 'Context' NULL SEIN WIRD!

            var orderItemVeId = auftragStatus.Context.OrderItem.VeId;
            if (!orderItemVeId.HasValue)
            {
                return; // Bei Aufträgen ohne Ve (=Formularbestellung) kann kein Token zurückgesetzt werden. 
            }


            var busAddress = auftragStatus.Context.Bus.Address;
            var archiveRecordId = orderItemVeId.Value;
            var contextOrderDataAccess = auftragStatus.Context.OrderDataAccess;
            var sendEndpointProvider = auftragStatus.Context.Bus;

            var archiveRecord = auftragStatus.Context.IndexAccess.FindDocument(archiveRecordId.ToString(), false);
            
            // It is possible, that a VE record was delete while the order item is still in progress
            // So if the archiveRecord does not exist anymore, no need to update the Indiv Tokens
            if (archiveRecord != null)
            {
                var recalcTokens = new RecalcIndivTokens
                {
                    ArchiveRecordId = archiveRecordId,
                    ExistingMetadataAccessTokens = archiveRecord.MetadataAccessTokens.ToArray(),
                    ExistingPrimaryDataDownloadAccessTokens = archiveRecord.PrimaryDataDownloadAccessTokens.ToArray(),
                    ExistingPrimaryDataFulltextAccessTokens = archiveRecord.PrimaryDataFulltextAccessTokens.ToArray()
                };


                auftragStatus.Context.PostCommitActionsRegistry.RegisterPostCommitAction(async () =>
                {
                    await SendToIndexManager(recalcTokens, contextOrderDataAccess, sendEndpointProvider, busAddress);
                });
            }
        }

        private static bool IsNotIndivToken(string t)
        {
            if (t == null)
            {
                throw new ArgumentNullException(nameof(t));
            }

            if (t.StartsWith("EB_"))
            {
                return false;
            }

            if (t.StartsWith("FG_"))
            {
                return false;
            }

            if (t == "DDS")
            {
                return false;
            }

            return true;
        }
    }
}