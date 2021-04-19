using CMI.Manager.DocumentConverter.Abbyy;
using CMI.Manager.DocumentConverter.Extraction;
using FluentAssertions;
using FREngine;
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
            var sut = new AbbyyWorker(enginePool.Object);

            // Act
            var result = sut.ExtractTextFromDocument("anything", new DefaultTextExtractorSettings("DocumentArchiving_Speed-Unknown"));

            // Assert
            result.HasError.Should().BeTrue();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
            result.ErrorMessage.Should().Contain("Ungültiges Profil <DocumentArchiving_Speed-Unknown> für Textextraktion angegeben.");
            enginePool.Verify(s => s.ReleaseEngine(It.IsAny<IEngine>(), It.IsAny<bool>()), Times.Exactly(1));

        }

        [Test]
        public void Remaining_pages_zero_returns_invalid_result()
        {
            // Arrange
            var enginePool = AbbyyArrange.ArrangeEnginePool(0);
            var sut = new AbbyyWorker(enginePool.Object);

            // Act
            var result = sut.ExtractTextFromDocument("anything", new DefaultTextExtractorSettings("DocumentArchiving_Speed"));

            // Assert
            result.HasError.Should().BeTrue();
            result.ErrorMessage.Should().NotBeNullOrEmpty();
            result.ErrorMessage.Should().Contain("Anzahl Dokumente überschritten");
            enginePool.Verify(s => s.ReleaseEngine(It.IsAny<IEngine>(), It.IsAny<bool>()), Times.Exactly(1));

        }

        [Test]
        public void Empty_one_page_document_returns_empty_string()
        {
            // Arrange
            var enginePool = AbbyyArrange.ArrangeEnginePool(99, 1, true);
            var sut = new AbbyyWorker(enginePool.Object);

            // Act
            var result = sut.ExtractTextFromDocument("anything", new DefaultTextExtractorSettings("DocumentArchiving_Speed"));

            // Assert
            result.HasError.Should().BeFalse();
            result.ErrorMessage.Should().BeNullOrEmpty();
            result.ToString().Should().BeNullOrEmpty();
            enginePool.Verify(s => s.ReleaseEngine(It.IsAny<IEngine>(), It.IsAny<bool>()), Times.Exactly(1));

        }
    }
}