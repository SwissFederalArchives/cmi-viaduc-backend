using System;
using System.Collections.Generic;
using Aspose.Pdf;
using Aspose.Pdf.Text;
using CMI.Contract.DocumentConverter;
using CMI.Manager.DocumentConverter.Extraction.Interfaces;
using Serilog;
using License = Aspose.Pdf.License;

namespace CMI.Manager.DocumentConverter.Extraction
{
    public class AsposePdfTextExtractor : TextExtractorBase
    {
        private static readonly string[] extensions = {"pdf"};


        static AsposePdfTextExtractor()
        {
            try
            {
                var licensePdf = new License();
                licensePdf.SetLicense("Aspose.Total.NET.lic");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while setting Aspose license.");
                throw;
            }
        }

        public override IEnumerable<string> AllowedExtensions => extensions;
        public override int Rank => 3;
        public override bool IsAvailable => true;

        public override ExtractionResult ExtractText(IDoc doc, ITextExtractorSettings settings)
        {
            var result = new ExtractionResult(settings.MaxExtractionSize);

            var textAbsorber = new TextAbsorber();

            using (var pdfDocument = new Document(doc.Stream))
            {
                pdfDocument.Pages.Accept(textAbsorber);
            }

            result.Append(textAbsorber.Text);

            return result;
        }
    }
}