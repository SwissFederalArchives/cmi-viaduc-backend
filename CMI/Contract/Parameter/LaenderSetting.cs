using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMI.Contract.Parameter
{
    public class LaenderSetting: CentralizedSetting
    {
        [ReadDefaultFromResource]
        public string Laender { get; set; }
    }
}
