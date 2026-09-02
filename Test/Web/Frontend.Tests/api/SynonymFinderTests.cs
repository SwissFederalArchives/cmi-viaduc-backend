using System.Linq;
using CMI.Web.Frontend.api;
using NUnit.Framework;
using Shouldly;

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

            result.Count().ShouldBe(1);
            result.First().Length.ShouldBe(3);
            result.First().Index.ShouldBe(0);
            result.First().Treffer.ShouldBe("aaa");
        }
    }
}