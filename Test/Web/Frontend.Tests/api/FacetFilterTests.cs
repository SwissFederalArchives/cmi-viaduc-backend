using System;
using CMI.Web.Frontend.api.Elastic;
using FluentAssertions;
using NUnit.Framework;

namespace CMI.Web.Frontend.API.Tests.api
{
    [TestFixture]
    internal class FacetFilterTests
    {
        [Test]
        public void Vergleich_mit_illegalem_Feld_am_Arraybegin_wirft_Exception()
        {
            var action = new Action(() => { SearchRequestBuilder.GetSecuredFacetFilters(new[] {"all_Primarydata:\"Dossier\"", "level:\"Dossier\""}); });

            action.Should().Throw<Exception>();
        }

        [Test]
        public void Vergleich_mit_illegalem_Feld_am_Arrayende_wirft_Exception()
        {
            var action = new Action(() => { SearchRequestBuilder.GetSecuredFacetFilters(new[] {"level:\"Dossier\"", "evil:\"Dossier\""}); });

            action.Should().Throw<Exception>();
        }

        [Test]
        public void Exists_mit_illegalem_Feld_wirft_Exception()
        {
            var action = new Action(() => { SearchRequestBuilder.GetSecuredFacetFilters(new[] {"(_exists_:evil)"}); });

            action.Should().Throw<Exception>();
        }

        [Test]
        public void Not_Exists_mit_illegalem_Feld_wirft_Exception()
        {
            var action = new Action(() => { SearchRequestBuilder.GetSecuredFacetFilters(new[] {"(!_exists_:evil)"}); });

            action.Should().Throw<Exception>();
        }

        [Test]
        public void Vergleichswert_wird_Escaped()
        {
            var secured = SearchRequestBuilder.GetSecuredFacetFilters(new[] {"level:Dossier:123"});

            secured.Should().BeEquivalentTo("level:Dossier\\:123");
        }
    }
}