using CMI.Access.Harvest.ActaPro;
using CMI.Contract.Common;
using CMI.Contract.Harvest;
using Serilog;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CMI.Access.Harvest
{
    public partial class AISDataAccess : IDbExternalContentAccess
    {
        private readonly IDigitizationOrderBuilder digitizationOrderBuilder;

        /// <summary>
        ///     Gets the digitization order data for a given archive record.
        ///     If the archive record cannot be found, success is returned, but the
        ///     contained DigitizationOrder property will have most
        ///     of its properties set to "keine Angabe".
        /// </summary>
        /// <param name="archiveRecordId">The archive record identifier.</param>
        /// <returns>DigitizationOrderDataResult.</returns>
        public async Task<DigitizationOrderDataResult> GetDigitizationOrderData(string archiveRecordId)
        {
            var sw = new Stopwatch();
            sw.Start();

            var retVal = new DigitizationOrderDataResult();
            try
            {
                retVal.DigitizationOrder = await digitizationOrderBuilder.Build(archiveRecordId);
                retVal.Success = true;
            }
            catch (ApiException apiException)
            {
                Log.Error(apiException, "ApiException while getting digitization order data for id {archiveRecordId}. Record was not found in the database anymore.", archiveRecordId);
                retVal.ErrorMessage = apiException.Message;
            }
            catch (AggregateException ex)
            {
                Log.Error(ex, "Unexpected error while getting digitization order data.");
                retVal.ErrorMessage = ex.GetBaseException().Message;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while getting digitization order data.");
                retVal.ErrorMessage = ex.Message;
            }

            Log.Information("Took {Time}ms to build DigitizationOrderDataResult for id {Id}", sw.ElapsedMilliseconds, archiveRecordId);

            return retVal;
        }
    }
}