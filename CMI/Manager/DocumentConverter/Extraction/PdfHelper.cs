using System;
using Aspose.Pdf;
using Aspose.Pdf.Text;
using CMI.Contract.DocumentConverter;
using Serilog;

namespace CMI.Manager.DocumentConverter.Extraction
{
    public static class PdfHelper
    {
        public static bool HasText(IDoc doc)
        {
            try
            {
                var ta = new TextAbsorber();
                using (var document = new Document(doc.Stream))
                {
                    foreach (Page page in document.Pages)
                    {
                        page.Accept(ta);

                        if (ta.Text.Trim(' ', '\n', '\r').Length != 0)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var identifier = string.IsNullOrWhiteSpace(doc.Identifier) ? "???" : doc.Identifier;
                Log.Error(e, "HasText für {Identifier} failed.", identifier);
            }

            return false;
        }
    }
}