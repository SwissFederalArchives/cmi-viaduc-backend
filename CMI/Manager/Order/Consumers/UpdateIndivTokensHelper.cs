using System;
using System.Linq;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Order.Status;
using MassTransit;

namespace CMI.Manager.Order.Consumers
{
    public static class UpdateIndivTokensHelper
    {
        public static async Task SendToIndexManager(RecalcIndivTokens recalcTokens, IOrderDataAccess dataAccess, ISendEndpointProvider sendEndpointProvider, Uri uri)
        {
            var indivTokens = await dataAccess.GetIndividualAccessTokens(recalcTokens.ArchiveRecordId);
            // es benötigt keine individuellen MetadatenAccessTokens  
            var metadata = recalcTokens.ExistingMetadataAccessTokens;

            var fieldData = recalcTokens.ExistingFieldAccessTokens;
            // Nur individuelle Access Tokens hinzufügen, wenn von der DB überhaupt FieldAccessTokens geliefert werden
            if (fieldData != null && fieldData.Length > 0)
            {
                // Mix the existing tokens with the indiv tokens
                fieldData = recalcTokens.ExistingFieldAccessTokens == null ? indivTokens.FieldDataAccessTokens
                    : recalcTokens.ExistingFieldAccessTokens.Where(IsNotIndivToken).Union(indivTokens.FieldDataAccessTokens).Distinct().ToArray();
            }

            var download = recalcTokens.ExistingPrimaryDataDownloadAccessTokens.Where(IsNotIndivToken).ToArray();
            // Besitzt die VE ein Ö2 PrimaryDataDownloadAccessToken, so benötigt es keine Individuellen Token
            if (!download.Contains(AccessRoles.RoleOe2))
            {
                download = download.Union(indivTokens.PrimaryDataDownloadAccessTokens).Distinct().ToArray();
            }
            var fulltext = recalcTokens.ExistingPrimaryDataFulltextAccessTokens.Where(IsNotIndivToken).ToArray();
            // Besitzt die VE ein Ö2 PrimaryDataFulltextAccessTokens, so benötigt es keine Individuellen Token
            if (!fulltext.Contains(AccessRoles.RoleOe2))
            {
                fulltext = fulltext.Union(indivTokens.PrimaryDataFulltextAccessTokens).Distinct().ToArray();
            }

            var ep = await sendEndpointProvider.GetSendEndpoint(new Uri(uri, BusConstants.IndexManagerUpdateIndivTokensMessageQueue));
            await ep.Send(new UpdateIndivTokens
            {
                ArchiveRecordId = recalcTokens.ArchiveRecordId,
                CombinedPrimaryDataFulltextAccessTokens = fulltext,
                CombinedPrimaryDataDownloadAccessTokens = download,
                CombinedMetadataAccessTokens = metadata,
                CombinedFieldAccessTokens = fieldData
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
                    ExistingPrimaryDataFulltextAccessTokens = archiveRecord.PrimaryDataFulltextAccessTokens.ToArray(),
                    ExistingFieldAccessTokens = archiveRecord.FieldAccessTokens?.ToArray()
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