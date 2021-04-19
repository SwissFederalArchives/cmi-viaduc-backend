using CMI.Contract.Parameter;
using CMI.Contract.Parameter.Attributes;
using CMI.Web.Common.Helpers;

namespace CMI.Web.Management.ParameterSettings
{
    public class LaenderSetting : ILaenderSetting, ISetting
    {
        [ReadDefaultFromResource] public string Laender { get; set; }
    }
}