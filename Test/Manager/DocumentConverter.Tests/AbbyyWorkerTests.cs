using CMI.Contract.DocumentConverter;
using CMI.Manager.DocumentConverter.Abbyy;
using CMI.Manager.DocumentConverter.Extraction;
using Shouldly;
using FREngine;
using MassTransit;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.DocumentConverter.Tests
{
    [TestFixture]
    public class AbbyyWorkerTests
    {
        [Test]
        public void Wrong_profile_name_returns_invalid_result()
        {
            // Arrange
            var enginePool = AbbyyArrange.ArrangeEnginePool(99);
            var bus = Mock.Of<IBus>();
            var sut = new AbbyyWorker(enginePool.Object, bus);

            // Act
            var result = sut.ExtractTextFromDocument("anything", new DefaultTextExtractorSettings("DocumentArchiving_Speed-Unknown"));

            // Assert
            result.HasError.ShouldBeTrue();
            result.ErrorMessage.ShouldNotBeNullOrEmpty();
            result.ErrorMessage.ShouldContain("Ungültiges Profil <DocumentArchiving_Speed-Unknown> für Textextraktion angegeben.");
            enginePool.Verify(s => s.ReleaseEngine(It.IsAny<IEngine>(), It.IsAny<bool>()), Times.Exactly(1));

        }

        [Test]
        public void Remaining_pages_zero_returns_invalid_result()
        {
            // Arrange
            var enginePool = AbbyyArrange.ArrangeEnginePool(0);
            var bus = Mock.Of<IBus>();
            var sut = new AbbyyWorker(enginePool.Object, bus);

            // Act
            var result = sut.ExtractTextFromDocument("anything", new DefaultTextExtractorSettings("DocumentArchiving_Speed"));

            // Assert
            result.HasError.ShouldBeTrue();
            result.ErrorMessage.ShouldNotBeNullOrEmpty();
            result.ErrorMessage.ShouldContain("Anzahl Dokumente überschritten");
            enginePool.Verify(s => s.ReleaseEngine(It.IsAny<IEngine>(), It.IsAny<bool>()), Times.Exactly(1));

        }

        [Test]
        public void Empty_one_page_document_returns_empty_string()
        {
            // Arrange
            var enginePool = AbbyyArrange.ArrangeEnginePool(99, 1, true);
            var bus = Mock.Of<IBus>();
            var sut = new AbbyyWorker(enginePool.Object, bus);

            // Act
            var result = sut.ExtractTextFromDocument("anything", new DefaultTextExtractorSettings("DocumentArchiving_Speed"));

            // Assert
            result.HasError.ShouldBeFalse();
            result.ErrorMessage.ShouldBeNullOrEmpty();
            result.ToString().ShouldBeNullOrEmpty();
            enginePool.Verify(s => s.ReleaseEngine(It.IsAny<IEngine>(), It.IsAny<bool>()), Times.Exactly(1));

        }
    }
}