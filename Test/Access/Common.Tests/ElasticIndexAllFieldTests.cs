using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMI.Contract.Common;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Shouldly;
using NUnit.Framework;

namespace CMI.Access.Common.Tests
{
    [NUnit.Framework.Ignore("Diese Tests müssen bewusst bzw. Bedarf ausgeführt werden")]
    [TestFixture]
    public class ElasticIndexAllFieldTests
    {
        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            var uri = "(change here, but do not commit)";
            var node = new Uri(uri);
            
            string username = "(change here, but do not commit)";
            string pwd = "(change here, but do not commit)";
            
            helper = new ElasticIndexHelper(node, username,pwd, "test2");
            if (await helper.IndexExists("test2"))
            {
                await helper.DeleteIndex("test2");
            }

            await helper.CreateIndex("test2");

            var ear = new ElasticArchiveRecord();
            ear.ArchiveRecordId = "1";
            ear.PrimaryData = new List<ElasticArchiveRecordPackage>();

            ear.PrimaryData.Add(new ElasticArchiveRecordPackage()
            {
                FileCount = 1,
                FulltextExtractionDuration = TimeSpan.FromSeconds(1500),
            });
            ear.PrimaryData[0].Items.Add(new ElasticRepositoryObject()
            {
                Name = "RepositoryObject",
                Path = "PathToRepositoryObject",
                Type = (long) ElasticRepositoryObjectType.File,
                RepositoryId = "SomeRepositoryId",
                Hash = "SomeHash",
                HashAlgorithm = "SHA256",
                SizeInBytes = 1024,
                MimeType = "application/octet-stream",
                LogicalName = "SomeLogicalName",
                Content = "PrimaryDataContent"
            });

            ear.Title = "Title";
            ear.WithinInfo = "WithinInfo";
            ear.CustomFields = new
            {
                zugänglichkeitGemässBga = "In Schutzfrist",
                publikationsrechte = "ABC",
                
            };

            await helper.Index(ear);

            // Wait till index is ready
            SearchResponse<ElasticArchiveRecord> searchResponse;
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
            result.ShouldBe("ABC");
        }


        private void XShouldBeInField(string x, string field)
        {
            var searchResponse = Search(x, field);

            searchResponse.Hits.Count.ShouldBe(1);
        }

        private void XShouldNotBeInField(string x, string field)
        {
            var searchResponse = Search(x, field);

            searchResponse.Hits.Count.ShouldBe(0);
        }

        private SearchResponse<ElasticArchiveRecord> Search(string searchText, string field)
        {
            var searchRequest = new SearchRequest<ElasticArchiveRecord>
            {
                Query = new QueryStringQuery($"{field}:{searchText}")
            };

            return helper.Client.Search<ElasticArchiveRecord>(searchRequest);
        }
        
        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            if (helper != null && await helper.IndexExists("test2"))
            {
                await helper.DeleteIndex("test2");
            }
        }
    }
}