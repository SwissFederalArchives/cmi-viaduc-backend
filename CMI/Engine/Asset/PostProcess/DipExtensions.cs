using System;
using System.Collections.Generic;
using System.Linq;
using CMI.Contract.Common.Gebrauchskopie;

namespace CMI.Engine.Asset.PostProcess;

public static class DipExtensions
{
    public static List<DokumentDIP> OrderItems(this IEnumerable<DokumentDIP> list)
    {
        return list.OrderBy(d => GetZusatzMerkmal(d.zusatzDaten, "ReihenfolgeAnalogesDossier")).ToList();
    }

    public static List<DossierDIP> OrderItems(this IEnumerable<DossierDIP> list)
    {
        return list.OrderBy(d => d.zusatzDaten.GetZusatzMerkmal("ReihenfolgeAnalogesDossier")).ToList();
    }

    public static string GetZusatzMerkmal(this IEnumerable<ZusatzDatenMerkmal> merkmale, string merkmalName)
    {
        return merkmale.FirstOrDefault(d => d.Name.Equals(merkmalName, StringComparison.InvariantCultureIgnoreCase))?.Value;
    }
}