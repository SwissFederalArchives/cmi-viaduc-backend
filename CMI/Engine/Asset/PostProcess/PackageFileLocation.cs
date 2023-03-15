using System.Collections.Generic;
using System.IO;
using System.Linq;
using CMI.Contract.Common.Gebrauchskopie;

namespace CMI.Engine.Asset.PostProcess;

public class PackageFileLocation
{
    public DateiDIP Datei { get; set; }
    public List<OrdnerDIP> OrdnerList { get; set; } = new();
    public string RelativeDateiPfad => string.Join("\\", OrdnerList.Select(o => o.Name));
    public string FullName => Path.Combine(RelativeDateiPfad, Datei.Name);
}