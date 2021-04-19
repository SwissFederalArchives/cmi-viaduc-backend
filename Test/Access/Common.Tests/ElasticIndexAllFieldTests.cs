using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CMI.Contract.Common;
using FluentAssertions;
using Nest;
using NUnit.Framework;

namespace CMI.Access.Common.Tests
{
    [NUnit.Framework.Ignore("Diese Tests müssen bewusst bzw. Bedarf ausgeführt werden")]
    [TestFixture]
    public class ElasticIndexAllFieldTests
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var uri = "(change here, but do not commit)";
            var node = new Uri(uri);

            helper = new ElasticIndexHelper(node, "test2");
            if (helper.IndexExists("test2"))
            {
                helper.DeleteIndex("test2");
            }

            helper.CreateIndex("test2");

            var ear = new ElasticArchiveRecord();
            ear.ArchiveRecordId = "1";
            ear.PrimaryData = new List<ElasticArchiveRecordPackage>();

            ear.PrimaryData.Add(new ElasticArchiveRecordPackage());
            ear.PrimaryData[0].Items.Add(new ElasticRepositoryObject());
            ear.PrimaryData[0].Items[0].Content = "PrimaryDataContent";

            ear.Title = "Title";
            ear.WithinInfo = "WithinInfo";
            ear.CustomFields = new
            {
                zugänglichkeitGemässBga = "In Schutzfrist",
                publikationsrechte = "ABC"
            };

            helper.Index(ear);

            // Wait till index is ready
            ISearchResponse<ElasticArchiveRecord> searchResponse;
            do
            {
                Thread.Sleep(500);
                searchResponse = Search("Title", "title");
            } while (searchResponse.Hits.Count == 0);
        }

        private ElasticIndexHelper helper;

        [Test]
        public void TestTitle()
        {
            XShouldBeInField("Title", "all_Metadata_\\*");
            XShouldBeInField("Title", "all_\\*");
            XShouldNotBeInField("Title", "all_Primarydata");
        }

        [Test]
        public void TestWithinInfo()
        {
            XShouldBeInField("WithinInfo", "all_Metadata_\\*");
            XShouldBeInField("WithinInfo", "all_\\*");
            XShouldNotBeInField("WithinInfo", "all_Primarydata");
        }

        [Test]
        public void TestPrimaryData()
        {
            XShouldNotBeInField("PrimaryDataContent", "all_Metadata_\\*");
            XShouldBeInField("PrimaryDataContent", "all_\\*");
            XShouldBeInField("PrimaryDataContent", "all_Primarydata");
        }

        [Test]
        public void TestUpperLowerCase()
        {
            var searchResponse = Search("ABC", "customFields.publikationsrechte");
            var result = searchResponse.Hits.First().Source.GetCustomValueOrDefault<string>("publikationsrechte");
            result.Should().Be("ABC");
        }


        private void XShouldBeInField(string x, string field)
        {
            var searchResponse = Search(x, field);

            searchResponse.Hits.Count.Should().Be(1);
        }

        private void XShouldNotBeInField(string x, string field)
        {
            var searchResponse = Search(x, field);

            searchResponse.Hits.Count.Should().Be(0);
        }

        private ISearchResponse<ElasticArchiveRecord> Search(string searchText, string field)
        {
            var searchRequest = new SearchRequest<ElasticArchiveRecord>
            {
                Query = new QueryStringQuery {Query = $"{field}:{searchText}"}
            };

            return helper.Client.Search<ElasticArchiveRecord>(searchRequest);
        }
    }
}