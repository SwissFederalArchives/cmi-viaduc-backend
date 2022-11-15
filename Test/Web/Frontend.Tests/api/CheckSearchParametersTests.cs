using System.Collections.Generic;
using CMI.Web.Common.api;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Providers;
using CMI.Web.Frontend.api.Search;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace CMI.Web.Frontend.API.Tests.api
{
    /// <summary>
    ///     Prüft die Methode EntityProvider.CheckSearchParameters
    /// </summary>
    [TestFixture]
    public class CheckSearchParametersTests
    {
        private readonly EntityProvider entityProvider;


        public CheckSearchParametersTests()
        {
            var translatorMock = new Mock<ITranslator>();

            translatorMock.Setup(f => f.GetTranslation("de", "search.termToShort", It.IsAny<string>()))
                .Returns("search.termToShort");
            translatorMock.Setup(f => f.GetTranslation("de", "search.termToShortForAll", It.IsAny<string>()))
                .Returns("search.termToShortForAll");

            entityProvider = new EntityProvider(translatorMock.Object, null, null, null);
        }


        [Test]
        public void Check_mit_SearchParameters_Null()
        {
            var result = entityProvider.CheckSearchParameters(null, "de");

            result.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void Check_mit_Query_Null()
        {
            var result = entityProvider.CheckSearchParameters(new SearchParameters(), "de");

            result.Should().NotBeNullOrEmpty();
        }


        [Test]
        public void Check_gueltige_Parameter_werden_akzeptiert()
        {
            var result = entityProvider.CheckSearchParameters(GetValidParameters(), "de");

            result.Should().BeEmpty();
        }


        [Test]
        public void Check_mit_Paging_Take_zu_gross()
        {
            var searchParameter = GetValidParameters();
            searchParameter.Paging.Take = 101;

            var result = entityProvider.CheckSearchParameters(searchParameter, "de");

            result.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void Check_mit_Paging_Take_maximalgroesse()
        {
            var searchParameter = GetValidParameters();
            searchParameter.Paging.Take = 100;

            var result = entityProvider.CheckSearchParameters(searchParameter, "de");

            result.Should().BeEmpty();
        }


        [Test]
        public void Check_Suche_nur_Stern()
        {
            var result = entityProvider.CheckSearchParameters(GetParameters("xy", "*"), "de");

            result.Should().Be("search.termToShort");
        }

        [Test]
        public void Check_Suche_nur_Fragezeichen()
        {
            var result = entityProvider.CheckSearchParameters(GetParameters("xy", "?"), "de");

            result.Should().Be("search.termToShort");
        }

        [Test]
        public void Check_Suche_mit_Null()
        {
            var result = entityProvider.CheckSearchParameters(GetParameters("xy", null), "de");

            result.Should().Be("search.termToShort");
        }

        [Test]
        public void Check_Suche_alles_zu_kurz()
        {
            var result = entityProvider.CheckSearchParameters(GetParameters("allData", "A*"), "de");

            result.Should().Be("search.termToShortForAll");
        }

        [Test]
        public void Check_Suche_alles_leer()
        {
            var result = entityProvider.CheckSearchParameters(GetParameters("allData", ""), "de");

            result.Should().Be("search.termToShortForAll");
        }


        [Test]
        public void Check_Suche_ohne_Felder()
        {
            var searchParameter = new SearchParameters();

            searchParameter.Query = new SearchModel
            {
                SearchGroups = new List<SearchGroup>
                {
                    new SearchGroup
                    {
                        SearchFields = new List<SearchField>()
                    }
                }
            };

            var result = entityProvider.CheckSearchParameters(searchParameter, "de");

            result.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void Check_Suche_mehrere_Felder_leer()
        {
            var searchParameter = new SearchParameters();

            searchParameter.Query = new SearchModel
            {
                SearchGroups = new List<SearchGroup>
                {
                    new SearchGroup
                    {
                        SearchFields =
                        {
                            new SearchField {Key = "X", Value = ""},
                            new SearchField {Key = "Y", Value = ""}
                        }
                    }
                }
            };

            var result = entityProvider.CheckSearchParameters(searchParameter, "de");

            result.Should().Be("search.termToShort");
        }

        [Test]
        public void Check_Suche_mehrere_Felder_gefuellt()
        {
            var searchParameter = new SearchParameters();

            searchParameter.Query = new SearchModel
            {
                SearchGroups = new List<SearchGroup>
                {
                    new SearchGroup
                    {
                        SearchFields =
                        {
                            new SearchField {Key = "X", Value = "A"},
                            new SearchField {Key = "Y", Value = "B"}
                        }
                    }
                }
            };

            var result = entityProvider.CheckSearchParameters(searchParameter, "de");

            result.Should().BeEmpty();
        }


        private SearchParameters GetValidParameters()
        {
            return GetParameters("allData", "BE");
        }

        private SearchParameters GetParameters(string field, string value)
        {
            var searchParameter = new SearchParameters();

            searchParameter.Paging = new Paging();
            searchParameter.Query = new SearchModel
            {
                SearchGroups = new List<SearchGroup>
                {
                    new SearchGroup
                    {
                        SearchFields =
                        {
                            new SearchField {Key = field, Value = value}
                        }
                    }
                }
            };

            return searchParameter;
        }
    }
}