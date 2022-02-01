using System;
using System.Diagnostics;
using System.Linq;
using CMI.Access.Harvest.ScopeArchiv;
using CMI.Contract.Common;
using CMI.Contract.Harvest;
using Devart.Data.Oracle;
using Serilog;

namespace CMI.Access.Harvest
{
    public partial class AISDataAccess : IDbExternalContentAccess
    {
        private readonly DigitizationOrderBuilder digitizationOrderBuilder;

        /// <summary>
        ///     Gets the digitization order data for a given archive record.
        ///     If the archive record cannot be found, success is returned, but the
        ///     but the contained DigitizationOrder property will have most
        ///     of its properties set to "keine Angabe".
        /// </summary>
        /// <param name="archiveRecordId">The archive record identifier.</param>
        /// <returns>DigitizationOrderDataResult.</returns>
        public DigitizationOrderDataResult GetDigitizationOrderData(string archiveRecordId)
        {
            var sw = new Stopwatch();
            sw.Start();

            var retVal = new DigitizationOrderDataResult();
            try
            {
                retVal.DigitizationOrder = digitizationOrderBuilder.Build(archiveRecordId);
                retVal.Success = true;
            }
            catch (AggregateException ex)
            {
                Log.Error(ex, "Unexpected error while getting digitization order data.");
                retVal.ErrorMessage = ex.GetBaseException().Message;
            }
            catch (OracleException ex)
            {
                Log.Error(ex, "Unexpected error while getting digitization order data.");
                // Only interested if connection could not be established
                if (ex.Code == 0)
                {
                    throw new AISDatabaseNotFoundOrNotRunningException();
                }

                retVal.ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while getting digitization order data.");
                retVal.ErrorMessage = ex.Message;
            }

            Log.Information("Took {Time}ms to build DigitizationOrderDataResult for id {Id}", sw.ElapsedMilliseconds, archiveRecordId);

            return retVal;
        }

        public SyncInfoForReportResult GetReportExternalContent(int[] mutationsIds)
        {
            return new SyncInfoForReportResult { Records = dataProvider.GetSyncInfoForReport(mutationsIds.ToList()) };
        }
    }
}