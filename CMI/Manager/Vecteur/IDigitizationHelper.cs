using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Order;

namespace CMI.Manager.Vecteur
{
    public interface IDigitizationHelper
    {
        Task<DigitalisierungsAuftrag> GetDigitalisierungsAuftrag(string archiveRecordId);

        /// <summary>Erstellt einen DigitalisierungsAuftrag anhand der vom Benutzer gemachten Eingaben bei der Bestellung.</summary>
        /// <param name="digipoolEntry">Der Auftrag aus dem DigiPool</param>
        Task<DigitalisierungsAuftrag> GetManualDigitalisierungsAuftrag(DigipoolEntry digipoolEntry);
    }
}