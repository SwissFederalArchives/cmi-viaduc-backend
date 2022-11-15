using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Contract.Common.Entities;

namespace CMI.Contract.Common
{
    public interface IManuelleKorrekturManager 
    {
        Task<ManuelleKorrekturDetailItem> GetManuelleKorrektur(int manuelleKorrekturId);
        Task<ManuelleKorrekturDto> InsertOrUpdateManuelleKorrektur(ManuelleKorrekturDto manuelleKorrektur, string userId);
        Task DeleteManuelleKorrektur(int manuelleKorrekturId);
        Task BatchDeleteManuelleKorrektur(int[] manuelleKorrekturIds);
        Task<Dictionary<string, string>> BatchAddManuelleKorrektur(string[] identifiers, string userId);
        Task<ManuelleKorrekturDto> PublizierenManuelleKorrektur(int manuelleKorrekturId, string userId);
    }
}
