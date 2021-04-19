using System;
using System.IO;
using CMI.Manager.DocumentConverter.Abbyy;
using CMI.Manager.DocumentConverter.Extraction;
using CMI.Manager.DocumentConverter.Extraction.Interfaces;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CMI.Manager.DocumentConverter.Tests
{
    [TestFixture]
    public class AbbyyTextExtractorTests
    {
        [Test]
        public void Not_setting_Abbyy_installation_path_returns_invalid_result()
        {
            // Arrange
            var worker = new Mock<IAbbyyWorker>();
            worker.Setup(s => s.ExtractTextFromDocument(It.IsAny<string>(), new DefaultTextExtractorSettings("Dummy")))
                .Returns(new ExtractionResult(int.MaxValue));
            var sut = new AbbyyTextExtractor(worker.Object);

            sut.MissingAbbyyPathInstallationMessage = "Missing-Message";

            // Act
            var result = sut.ExtractText(new Doc(new FileInfo(Path.GetTempFileName()), "id"), new DefaultTextExtractorSettings("Dummy"));

            // Test
            result.HasError.Should().BeTrue();
            result.ErrorMessage.Should().StartWith("Missing-Message");
        }

        [Test]
        public void Setting_invalid_Abbyy_installation_path_throws_exception()
        {
            // Arrange
            var worker = new Mock<IAbbyyWorker>();
            worker.Setup(s => s.ExtractTextFromDocument(It.IsAny<string>(), new DefaultTextExtractorSettings("Dummy")))
                .Returns(new ExtractionResult(int.MaxValue));
            var sut = new AbbyyTextExtractor(worker.Object);

            // Act
            Assert.Throws<FileNotFoundException>(() => sut.PathToAbbyyFrEngineDll = "Anything");

        }

        [Test]
        public void Setting_empty_Abbyy_installation_path_throws_exception()
        {
            // Arrange
            var worker = new Mock<IAbbyyWorker>();
            worker.Setup(s => s.ExtractTextFromDocument(It.IsAny<string>(), new DefaultTextExtractorSettings("Dummy")))
                .Returns(new ExtractionResult(int.MaxValue));
            var sut = new AbbyyTextExtractor(worker.Object);

            // Act
            Assert.Throws<ArgumentException>(() => sut.PathToAbbyyFrEngineDll = string.Empty);

        }

        [Test]
        public void Extracting_text_returns_valid_result()
        {
            // Arrange
            var er = new ExtractionResult(int.MaxValue);
            er.Append("Test");
            var worker = new Mock<IAbbyyWorker>();
            worker.Setup(s => s.ExtractTextFromDocument(It.IsAny<string>(), It.IsAny<ITextExtractorSettings>()))
                .Returns(er);
            var sut = new AbbyyTextExtractor(worker.Object);
            sut.PathToAbbyyFrEngineDll = this.GetType().Assembly.Location;  // Just pass an existing file

            // Act
            var result = sut.ExtractText(new Doc(new FileInfo(Path.GetTempFileName()), "id"), new DefaultTextExtractorSettings("Dummy"));

            // Test
            result.HasError.Should().BeFalse();
            result.ErrorMessage.Should().BeNullOrEmpty();
            result.ToString().Should().Be("Test\r\n");
        }

    }
}