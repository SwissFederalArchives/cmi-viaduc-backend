using System.Collections.Generic;
using System.Linq;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Frontend.api.Elastic;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Explain;
using Elastic.Clients.Elasticsearch.Core.Search;
using Shouldly;
using NUnit.Framework;

namespace CMI.Web.Frontend.API.Tests.api
{
    [TestFixture]
    public class HighlightServiceTests
    {

        [Test]
        public void Test_if_user_with_BAR_role_record_without_unanonymized_fields_highlights_title()
        {
            // ARRANGE
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleBAR, null, null, false);
            var hit = new Hit<SearchRecord> ("2", "archive")
            {
                Source = new SearchRecord
                {
                    Title = "Highlighting"

                },
                Highlight = DefaultHighlightData("Highlighting")
            };

            // ACT
            var highlight = hit.GetHighlightingObj<SearchRecord>(userAccess, "unanonymized title");

            // ASSERT
            highlight.ShouldNotBeNull();
            highlight.Children().Count().ShouldBe(2);
            highlight["title"]!.Count().ShouldBe(1); 
            highlight["title"]
                .Values<string>().First().ShouldBe("Highlighting");
          
            }

        [Test]
        public void Test_if_user_with_BAR_role_highlights_unanonymized_title()
        {
            // ARRANGE
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleBAR, null, null, false);
            var hit = new Hit<ElasticArchiveDbRecord>("2", "archive")
            {
                Source = new ElasticArchiveDbRecord
                {
                    Title = "Highlighting",
                    FieldAccessTokens = new List<string>{ "FieldAccessTokens" , "BAR"},
                    IsAnonymized = true,
                    UnanonymizedFields = new UnanonymizedFields
                    {
                        Title = "Unanonymized.Title"
                    }
                },
                Highlight = DefaultHighlightData("Title")
            };

            // ACT
            var highlight = hit.Highlight;

            // ASSERT
            highlight.ShouldNotBeNull();
            highlight.Count().ShouldBe(4);
            highlight["title"]!.Count().ShouldBe(1); 
            highlight["title"].First().ShouldBe("Title");
        }

        [Test]
        public void Test_if_user_with_BAR_role_highlights_all_unanonymized_fields()
        {
            // ARRANGE
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleBAR, null, null, false);
            var hit = new Hit<ElasticArchiveDbRecord>("2", "archive")
            {
                Source = new ElasticArchiveDbRecord
                {
                    Title = "Highlighting",
                    IsAnonymized = true,
                    FieldAccessTokens = new List<string> { "BAR" },
                    PrimaryDataFulltextAccessTokens = new List<string> { "BAR" },
                    UnanonymizedFields = new UnanonymizedFields
                    {
                        Title = "unanonymized.title"
                    }
                },
                Highlight = DefaultHighlightData("title")
            };

            // ACT
            var highlight = hit.Highlight;

            // ASSERT
            highlight.ShouldNotBeNull();
            highlight.Count().ShouldBe(4);
            highlight["title"]!.Count().ShouldBe(1);
            highlight["title"]
                .First().ShouldBe("title");
        }

        [Test]
        public void Test_if_user_with_Oe2_role_highlights_AnonymizedFields()
        {
            // ARRANGE
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe2, null, null, false);
            var hit = new Hit<ElasticArchiveDbRecord>("2", "archive")
            {
                Source = new ElasticArchiveDbRecord
                {
                    Title = "Highlighting",
                    FieldAccessTokens = new List<string> { "FieldAccessTokens", "BAR" },
                    PrimaryDataFulltextAccessTokens = new List<string> { "FieldAccessTokens", "BAR" },
                    UnanonymizedFields = new UnanonymizedFields
                    {
                        Title = "UnanonymizedTitle"
                    }
                },
                Highlight = DefaultHighlightData("Highlighting")
            };

            // ACT
            var highlight = hit.Highlight;

            // ASSERT
            highlight.ShouldNotBeNull();
            highlight.Count().ShouldBe(4);
            highlight["title"]!.Count().ShouldBe(1); ;
            highlight["title"]
                .First().ShouldBe("Highlighting");
        }

        [Test]
        public void Test_if_user_with_Oe2_role_record_without_HighlightKeyTitle_set_RecordTitle()
        {
            // ARRANGE
            var highlightData = new Dictionary<string, IReadOnlyCollection<string>>();
            highlightData.Add("xxx", new[] { "Highlighting" });
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe2, null, null, false);
            var hit = new Hit<SearchRecord>("2", "archive")
            {
                Source = new SearchRecord
                {
                    Title = "default value"
                },
                Highlight = highlightData
            };
            
            // ACT
            var highlight = hit.GetHighlightingObj<SearchRecord>(userAccess, "default value");

            // ASSERT
            highlight.ShouldNotBeNull();
            highlight.Children().Count().ShouldBe(2);
            highlight["title"]!.Count().ShouldBe(1); ;
            highlight["mostRelevantVektor"]!.Count().ShouldBe(0); ;
            highlight["title"]
                .Values<string>().First().ShouldBe("default value");
        }



        private static Dictionary<string, IReadOnlyCollection<string>> DefaultHighlightData(string title)
        {
            var highlightData = new Dictionary<string, IReadOnlyCollection<string>>();
            highlightData.Add("title", new[] { title });
            highlightData.Add("unanonymizedFields.title", new[] { "unanonymizedFields." + title });
            highlightData.Add("all_Metadata_Text", new[] { " < em>Fundstelle</em>", "Dies ist eine andere <em>Fundstelle</em>" });
            highlightData.Add("all_Primarydata", new[] { " < em>Fundstelle</em> in den Primärdaten" });
            return highlightData;
        }
    }
}


