using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMI.Manager.DocumentConverter.Abbyy;
using FREngine;
using Moq;

namespace CMI.Manager.DocumentConverter.Tests
{
    internal static class AbbyyArrange
    {

        public static Mock<IEnginesPool> ArrangeEnginePool(int pagesRemaining, int documentPageCount = 2, bool pageIsEmpty = false)
        {
            var license = Mock.Of<ILicense>(setup => setup.get_VolumeRemaining(LicenseCounterTypeEnum.LCT_Pages) == pagesRemaining);
            var document = new Mock<FRDocument>();
            document.Setup(s => s.Process(It.IsAny<DocumentProcessingParams>()));
            document.SetupGet(s => s.Pages.Count).Returns(documentPageCount);
            document.Setup(s => s.Pages[0].IsEmpty(null, null, false)).Returns(pageIsEmpty);
            document.Setup(s => s.Export(It.IsAny<string>(), FileExportFormatEnum.FEF_TextUnicodeDefaults, null));
            document.Setup(s => s.Close());
            var engine = new Mock<IEngine>();
            engine.SetupGet(s => s.CurrentLicense).Returns(license);
            engine.Setup(s => s.LoadPredefinedProfile(It.IsAny<string>()));
            engine.Setup(s => s.CreateFRDocumentFromImage(It.IsAny<string>(), null)).Returns(document.Object);
            var p = new Mock<DocumentProcessingParams>();
            p.Setup(s => s.PageProcessingParams.RecognizerParams.SetPredefinedTextLanguage(It.IsAny<string>()));
            engine.Setup(s => s.CreateDocumentProcessingParams()).Returns(() => p.Object);
            var enginePool = new Mock<IEnginesPool>();
            enginePool.Setup(s => s.GetEngine()).Returns(engine.Object);
            return enginePool;
        }
    }
}
