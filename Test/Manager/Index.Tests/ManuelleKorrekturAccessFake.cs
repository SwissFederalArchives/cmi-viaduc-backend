using CMI.Access.Sql.Viaduc.EF;
using CMI.Contract.Common.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMI.Manager.Index.Tests;


public class ManuelleKorrekturAccessFake : IManuelleKorrekturAccess
{
    internal ManuelleKorrektur ManuelleKorrektur = new()
    {
        VeId = "7",
        Aktenzeichen = "XY",
        AnonymisiertZumErfassungszeitpunk = true,
        Anonymisierungsstatus = (int) AnonymisierungsStatusEnum.InProgress,
        Entstehungszeitraum = "1982-2090"
    };

    private List<ManuelleKorrekturFeld> manuelleKorrekturFelder = new()
    {
        new()
        {
            Automatisch = "Heinz Harald Geschichten███",
            Feldname = ManuelleKorrekturFelder.Titel,
            Manuell = "Heinz Harald ███",
            Original = "Heinz Harald Geschichtenbuch"
        }
    };
    public ViaducDb Context { get; }
    public Task<List<VManuelleKorrekturDto>> GetAllManuelleKorrekturen()
    {
        throw new NotImplementedException();
    }

    public Task<ManuelleKorrekturDto> GetManuelleKorrektur(int manuelleKorrekturId)
    {
        throw new NotImplementedException();
    }

    public Task<ManuelleKorrekturDto> GetManuelleKorrektur(Func<ManuelleKorrektur, bool> searchPredicate)
    {

        ManuelleKorrektur.ManuelleKorrekturFelder = [manuelleKorrekturFelder.FirstOrDefault()];
        
        var list = new List<ManuelleKorrektur> { ManuelleKorrektur };
        return Task.FromResult(list.FirstOrDefault(searchPredicate).ToDtoWithRelated(1));
    }

    public Task<ManuelleKorrekturDto> InsertOrUpdateManuelleKorrektur(ManuelleKorrekturDto value, string userId)
    {
        ManuelleKorrektur = value.ToEntity();
        manuelleKorrekturFelder = [value.ManuelleKorrekturFelder.FirstOrDefault().ToEntity()];
        return Task.FromResult(value);
    }

    public Task DeleteManuelleKorrektur(int manuelleKorrekturId)
    {
        if (ManuelleKorrektur.ManuelleKorrekturId == manuelleKorrekturId)
        {
            ManuelleKorrektur = null;
        }
        return Task.CompletedTask;
    }

    public Task BatchDeleteManuelleKorrekturen(int[] manuelleKorrekturIds)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CheckCanInsertManuelleKorrektur(string id)
    {
        throw new NotImplementedException();
    }

    public Task<ManuelleKorrekturDto> Publizieren(int manuelleKorrekturId, string userId)
    {
        throw new NotImplementedException();
    }
}