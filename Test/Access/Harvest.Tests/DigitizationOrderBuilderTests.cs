using CMI.Access.Harvest.ScopeArchiv;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CMI.Access.Harvest.Tests
{
    [TestFixture]
    public class DigitizationOrderBuilderTests
    {
        [Test]
        public void Inexisting_record_id_must_return_properties_with_no_data_available_flag()
        {
            // ARRANGE
            var mock = new DataProviderMock();
            var provider = mock.GetMock();
            var arBuilder = new Mock<ArchiveRecordBuilder>(null, null, null, null);
            arBuilder.Setup(i => i.Build("1")).Returns(() => null);
            var builder = new DigitizationOrderBuilder(provider, arBuilder.Object, new SipDateBuilder());

            // ACT
            var result = builder.Build("1");

            // ASSERT
            result.Ablieferung.AblieferndeStelle.Should().Be(DigitizationOrderBuilder.NoDataAvailable);
            result.Ablieferung.AktenbildnerName.Should().Be(DigitizationOrderBuilder.NoDataAvailable);
            result.Ablieferung.Ablieferungsnummer.Should().Be(DigitizationOrderBuilder.NoDataAvailable);

            result.OrdnungsSystem.Name.Should().Be(DigitizationOrderBuilder.NoDataAvailable);
            result.OrdnungsSystem.Signatur.Should().Be(DigitizationOrderBuilder.NoDataAvailable);
            result.OrdnungsSystem.Stufe.Should().Be(DigitizationOrderBuilder.NoDataAvailable);

            result.Dossier.Titel.Should().Be(DigitizationOrderBuilder.NoDataAvailable);
            result.Dossier.Entstehungszeitraum.Should().Be(DigitizationOrderBuilder.NoDataAvailable);
        }
    }
}