using CMI.Contract.Parameter;
using CMI.Contract.Parameter.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CMI.Web.Management.ParameterSettings
{
    public class BehaeltnisInhaltTemplate : ISetting
    {
        [ReadDefaultFromResource] public string HtmlTemplate { get; set; }
    }
}