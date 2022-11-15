using System.Linq;
using CMI.Web.Frontend.api;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Web.Frontend.API.Tests.api
{
    [TestFixture]
    public class FileWoerterbuchTests
    {
        [Test]
        public void Wenn_2_Eintraege_vorhanden_sind_die_sich_nur_durch_Gross_Kleinschreibung_unterscheiden_dann_darf_nur_eine_Quelle_genannt_werden()
        {
            var fs = new MockFileSystem();
            fs.AddFile("c:\\synonyme\\SourceMapping.txt", new[]
            {
                "Test	TestDe	TestFr	TestIt	TestEn"
            });
            fs.AddFile("c:\\synonyme\\Test.txt", new[]
            {
                "A|a"
            });

            var wb = new FileWoerterbuch(fs, "c:\\synonyme");

            var synonymGroups = wb.FindGroups("a");
            synonymGroups.Should().HaveCount(1);
            synonymGroups.First().Sources.Count.Should().Be(1);
        }

        [Test]
        public void Duplikate_sollen_folgebegriffe_nicht_abbrechen()
        {
            var fs = new MockFileSystem();
            fs.AddFile("c:\\synonyme\\SourceMapping.txt", new[]
            {
                "Test	TestDe	TestFr	TestIt	TestEn"
            });
            fs.AddFile("c:\\synonyme\\Test.txt", new[]
            {
                "A|a|c"
            });

            var wb = new FileWoerterbuch(fs, "c:\\synonyme");

            var synonymGroups = wb.FindGroups("c");
            synonymGroups.Should().HaveCount(1);
            synonymGroups.First().Sources.Count.Should().Be(1);
        }
    }
}