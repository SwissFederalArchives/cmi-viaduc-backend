using System.Collections.Generic;
using System.Linq;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Frontend.api.Elastic;
using FluentAssertions;
using Nest;
using NUnit.Framework;

namespace CMI.Web.Frontend.API.Tests.api
{
    [TestFixture]
    public class HighlightServiceTests
    {
        /// <summary>
        /// Only for Testing in this class
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class TestHit<T> : IHit<T> where T : TreeRecord
        {
            public string Id { get; }
            public string Index { get; }
            public long? PrimaryTerm { get; }
            public string Routing { get; }
            public long? SequenceNumber { get; }
            public T Source { get; set; }
            public string Type { get; }
            public long Version { get; }
            public Explanation Explanation { get; }
            public FieldValues Fields { get; }
            public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Highlight { get; set; }
            public IReadOnlyDictionary<string, InnerHitsResult> InnerHits { get; }
            public NestedIdentity Nested { get; }
            public IReadOnlyCollection<string> MatchedQueries { get; }
            public double? Score { get; }
            public IReadOnlyCollection<object> Sorts { get; }
        }

        [Test]
        public void Test_if_user_with_BAR_role_record_without_unanonymized_fields_highlights_title()
        {
            // ARRANGE
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleBAR, null, null, false);
            var hit = new TestHit<SearchRecord>
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
            highlight.Should().NotBeNull();
            highlight.Children().Count().Should().Be(2);
            highlight["title"]!.Count().Should().Be(1); ;
            highlight["mostRelevantVektor"]!.Count().Should().Be(1); ;
            highlight["title"]
                .Values<string>().First().Should()
                .Be("Highlighting");
            highlight["mostRelevantVektor"]
                .Values<string>().First().Should()
                .Be(" < em>Fundstelle</em>");
        }

        [Test]
        public void Test_if_user_with_BAR_role_highlights_unanonymized_title()
        {
            // ARRANGE
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleBAR, null, null, false);
            var hit = new TestHit<ElasticArchiveDbRecord>()
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
            var highlight = hit.GetHighlightingObj<SearchRecord>(userAccess, "unanonymized title");

            // ASSERT
            highlight.Should().NotBeNull();
            highlight.Children().Count().Should().Be(2);
            highlight["title"]!.Count().Should().Be(1); ;
            highlight["mostRelevantVektor"]!.Count().Should().Be(0); ;
            highlight["title"]
                .Values<string>().First().Should()
                .Be("unanonymizedFields.Title");
        }

        [Test]
        public void Test_if_user_with_BAR_role_highlights_all_unanonymized_fields()
        {
            // ARRANGE
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleBAR, null, null, false);
            var hit = new TestHit<ElasticArchiveDbRecord>()
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
            var highlight = hit.GetHighlightingObj<SearchRecord>(userAccess, "unanonymized title");

            // ASSERT
            highlight.Should().NotBeNull();
            highlight.Children().Count().Should().Be(2);
            highlight["title"]!.Count().Should().Be(1); ;
            highlight["mostRelevantVektor"]!.Count().Should().Be(1); ;
            highlight["title"]
                .Values<string>().First().Should()
                .Be("unanonymizedFields.title");
            highlight["mostRelevantVektor"]
                .Values<string>().First().Should()
                .Be(" < em>Fundstelle</em> in den Primärdaten");
        }

        [Test]
        public void Test_if_user_with_Oe2_role_highlights_AnonymizedFields()
        {
            // ARRANGE
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe2, null, null, false);
            var hit = new TestHit<ElasticArchiveDbRecord>()
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
            var highlight = hit.GetHighlightingObj<SearchRecord>(userAccess, "Highlighting");

            // ASSERT
            highlight.Should().NotBeNull();
            highlight.Children().Count().Should().Be(2);
            highlight["title"]!.Count().Should().Be(1); ;
            highlight["mostRelevantVektor"]!.Count().Should().Be(1); ;
            highlight["title"]
                .Values<string>().First().Should()
                .Be("Highlighting");
            highlight["mostRelevantVektor"]
                .Values<string>().First().Should()
                .Be(" < em>Fundstelle</em>", "Dies ist eine andere <em>Fundstelle</em>");
        }

        [Test]
        public void Test_if_user_with_Oe2_role_record_without_HighlightKeyTitle_set_RecordTitle()
        {
            // ARRANGE
            var highlightData = new Dictionary<string, IReadOnlyCollection<string>>();
            highlightData.Add("xxx", new[] { "Highlighting" });
            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe2, null, null, false);
            var hit = new TestHit<SearchRecord>()
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
            highlight.Should().NotBeNull();
            highlight.Children().Count().Should().Be(2);
            highlight["title"]!.Count().Should().Be(1); ;
            highlight["mostRelevantVektor"]!.Count().Should().Be(0); ;
            highlight["title"]
                .Values<string>().First().Should()
                .Be("default value");
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


