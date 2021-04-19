using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMI.Manager.DocumentConverter.Render
{
    public class TransformResult
    {
        public FileInfo TargetFile { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }
    }
}
