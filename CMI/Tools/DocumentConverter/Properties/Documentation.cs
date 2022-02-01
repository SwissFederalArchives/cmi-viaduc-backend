using CMI.Utilities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMI.Tools.DocumentConverter.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<DocumentConverterSettings>(x => x.SftpLicenseKey, "SFTP Lizenzschlüssel");
        }
    }
}
