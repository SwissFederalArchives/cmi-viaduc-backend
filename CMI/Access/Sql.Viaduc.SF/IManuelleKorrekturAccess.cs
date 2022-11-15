using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Contract.Common.Entities;

namespace CMI.Access.Sql.Viaduc.EF
{
    public interface IManuelleKorrekturAccess
    {
        ViaducDb Context { get; }
        Task<List<VManuelleKorrekturDto>> GetAllManuelleKorrekturen();
        Task<ManuelleKorrekturDto> GetManuelleKorrektur(int manuelleKorrekturId);
        Task<ManuelleKorrekturDto> GetManuelleKorrektur(Func<ManuelleKorrektur, bool> searchPredicate);
        Task<ManuelleKorrekturDto> InsertOrUpdateManuelleKorrektur(ManuelleKorrekturDto value, string userId);
        Task DeleteManuelleKorrektur(int manuelleKorrekturId);
        Task BatchDeleteManuelleKorrekturen(int[] manuelleKorrekturIds);
        Task<bool> CheckCanInsertManuelleKorrektur(string id);
        Task<ManuelleKorrekturDto> Publizieren(int manuelleKorrekturId, string userId);
    }
}
