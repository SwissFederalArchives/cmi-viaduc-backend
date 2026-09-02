using System.Collections.Generic;
using System.Linq;
using CMI.Web.Frontend.api;
using Shouldly;
using NUnit.Framework;

namespace CMI.Web.Frontend.API.Tests.api
{
    [TestFixture]
    public class CombinatorTests
    {
        [Test]
        public void GetCombinationsTest()
        {
            var test = new[] {1, 2, 3};

            var result = test.GetCombinations().ToArray();

            result.ShouldNotBeNull();
            result.Length.ShouldBe(6);

            result[0].ShouldBe(new List<int>(new[] {1, 2, 3}));
            result[1].ShouldBe(new List<int>(new[] {1, 2}));
            result[2].ShouldBe(new List<int>(new[] {2, 3}));
            result[4].ShouldBe(new List<int>(new[] {2}));
        }

        [Test]
        public void GetCombinationsWithLengthTest()
        {
            var test = new[] {1, 2, 3};

            var result = Combinator.GetCombinationsWithLength(test.ToList(), 2).ToArray();

            result.ShouldNotBeNull();
            result.Length.ShouldBe(2);

            result[0].ShouldBe(new List<int>(new[] {1, 2}));
            result[1].ShouldBe(new List<int>(new[] {2, 3}));
        }
    }
}