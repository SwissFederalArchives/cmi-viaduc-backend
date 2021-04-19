using CMI.Contract.Parameter;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Web.Management.ParameterSettings
{
    public class VersandkontrolleTemplate : ISetting
    {
        [ReadDefaultFromResource] public string HtmlTemplate { get; set; }
    }
}