using NUnit.Framework;
using CMI.Manager.DocumentConverter.Abbyy;
using Shouldly;
using FREngine;
using Moq;

namespace CMI.Manager.DocumentConverter.Tests
{
    [TestFixture]
    public class AbbyyLicenseTests
    {
        [Test]
        public void License_call_returns_the_correct_number_of_pages()
        {
            // Arrange
            var enginePool = AbbyyArrange.ArrangeEnginePool(99);
            var sut = new AbbyyLicense(enginePool.Object);

            // Act
            var result = sut.GetRemainingPages();

            // Assert
            result.ShouldBe(99);
        }

  
    }
}
