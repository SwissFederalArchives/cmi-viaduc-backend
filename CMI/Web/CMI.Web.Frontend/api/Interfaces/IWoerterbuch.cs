using System.Collections.Generic;

namespace CMI.Web.Frontend.api.Interfaces
{
    public interface IWoerterbuch
    {
        List<SynonymGroup> FindGroups(string input);
    }
}