using System.Threading.Tasks;

namespace CMI.Contract.Order
{
    public interface IVecteurActions
    {
        Task SetStatusAushebungBereit(int auftragsId);
        Task SetStatusDigitalisierungExtern(int auftragsId);

        Task SetStatusDigitalisierungAbgebrochen(int auftragsId, string grund);

        Task SetStatusZumReponierenBereit(int auftragId);
    }
}