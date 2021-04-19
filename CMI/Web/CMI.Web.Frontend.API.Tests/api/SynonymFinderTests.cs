using System.Linq;
using CMI.Web.Frontend.api;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Web.Frontend.API.Tests.api
{
    [TestFixture]
    public class SynonymFinderTests
    {
        [Test]
        public void simpleTest()
        {
            var mock = new MockIWoerterbuch("aaa").SetDefaultSource("T3ST", "der t3st", "le t3st", "il t3st", "the t3st")
                .AddSynonymGroup("aaa", "bbb", "ccc").SetupAndReturn();


            var finder = new SynonymFinder(mock.Object, 15);
            var result = finder.GetSynonyme("aaa", "de");

            result.Should().HaveCount(1);
            result.First().Length.Should().Be(3);
            result.First().Index.Should().Be(0);
            result.First().Treffer.Should().Be("aaa");
        }
    }
}