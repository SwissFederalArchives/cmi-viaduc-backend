using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Templates;
using CMI.Web.Frontend.API.Tests.ElasticMock;
using Elasticsearch.Net;
using FluentAssertions;
using Moq;
using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CMI.Web.Frontend.API.Tests.api
{
    public class ElasticSearchNewAisTests
    {
        #region Fields
        private ElasticService service;
        private InMemoryConnection connection;
        private InMemoryConnection connectionForId;
        private Mock<IElasticClientProvider> clientProvider;
        #endregion

        #region Tests


        [Test]
        public void If_QueryForId_with_int_id_Scope_method_is_called()
        {
            // arrange
            var data = new List<DetailRecord>
            {
                new ElasticArchiveRecord {ArchiveRecordId = "12345", Title = "Hund", All = "Ball"},
            };
            connection = new InMemoryConnection(GetInMemoryData(data));
            // "Query For Id must return exactly one record"
            connectionForId = new InMemoryConnection(GetInMemoryData(new List<DetailRecord> { data.Last() }));
            InitializeElasticClient();

            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe3, null, null, false);

            // act
            var result = this.service.QueryForId<DetailRecord>("12345", userAccess);

            //assert
            result.Response.Hits.Count.Should().Be(1);
            result.Response.Documents.Should().BeEquivalentTo(data);


            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<DetailRecord>>()), Times.Once);
            // Search for details is not executed
            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<ElasticArchiveDbRecord>>()), Times.Never);
        }


        [Test]
        public void If_QueryForId_with_uuid_id_Scope_method_is_not_called()
        {
            // arrange
            var data = new List<DetailRecord>
            {
                new ElasticArchiveRecord {ArchiveRecordId = "Klas    2badb81d-ca89-5491-a0a2-78051750b341", Title = "Hund", All = "Ball"},
            };
            connection = new InMemoryConnection(GetInMemoryData(data));
            // "Query For Id must return exactly one record"
            connectionForId = new InMemoryConnection(GetInMemoryData([data.Last()]));
            InitializeElasticClient();

            var userAccess = new UserAccess("S31830999", AccessRoles.RoleOe3, null, null, false);

            // act
            var result = this.service.QueryForId<DetailRecord>("Klas    2badb81d-ca89-5491-a0a2-78051750b341", userAccess);

            //assert
            result.Response.Hits.Count.Should().Be(1);
            result.Response.Documents.Should().BeEquivalentTo(data);


            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<DetailRecord>>()), Times.Once);
            // Search for details is not executed
            clientProvider.Verify(c => c.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<ElasticArchiveDbRecord>>()), Times.Never);
        }

        #endregion

        #region private Methods

        private byte[] GetInMemoryData(List<DetailRecord> records)
        {
            int length = records.Count;
            var list = new object[length];

            for (int index = 0; index < length; index++)
            {
                list[index] = RecordJsonStruct(records[index]);
            }

            var inMemoryData = new
            {
                took = 1,
                timed_out = false,
                _shards = new
                {
                    total = 1,
                    successful = 1,
                    failed = 0
                },
                hits = new
                {
                    total = new { value = length },
                    max_score = length,
                    hits = list
                }
            };

            var json = JsonConvert.SerializeObject(inMemoryData);
            return Encoding.UTF8.GetBytes(json);
        }

        private object RecordJsonStruct(DetailRecord record)
        {
            return new
            {
                _index = "archive",
                _type = "elasticArchiveDbRecord",
                _id = "archiveRecordId",
                _score = 1.0,
                _source = record
            };
        }

        private void InitializeElasticClient()
        {
            var node = new Uri("http://localhost:9200");
            var pool = new SingleNodeConnectionPool(node);
          

            var settingsSearchForId = new ConnectionSettings(pool, connectionForId,
                (serializer, values) => new JsonNetSerializer(
                    serializer, values, null, null,
                    new[] { new ExpandoObjectConverter() }));

            var clientSearchForId = new ElasticClient(settingsSearchForId);

            clientProvider = new Mock<IElasticClientProvider>();
           
            clientProvider.Setup(m =>
                m.GetElasticClient(It.IsAny<IElasticSettings>(), It.IsAny<ElasticQueryResult<DetailRecord>>())).Returns(clientSearchForId);

            var translatorMock = new Mock<ITranslator>();

            translatorMock.Setup(f => f.GetTranslation("de", "search.termToShort", It.IsAny<string>()))
                .Returns("search.termToShort");
            translatorMock.Setup(f => f.GetTranslation("de", "search.termToShortForAll", It.IsAny<string>()))
                .Returns("search.termToShortForAll");
            var srb = new SearchRequestBuilder(new ElasticSettings(),
                new QueryTransformationService(ReadSearchSettings()), new List<TemplateField>());
            service = new ElasticService(clientProvider.Object, srb, new ElasticSettings(), new List<TemplateField>());
        }

        private static SearchSetting ReadSearchSettings()
        {
            var newSettings = new JObject();
            var path = AppDomain.CurrentDomain.BaseDirectory + "Resources//settings.json";
            if (File.Exists(path))
            {
                var clientSettings = JsonHelper.GetJsonFromFile(path);
                if (clientSettings != null)
                {
                    SettingsHelper.UpdateSettingsWith(newSettings, clientSettings, true);
                }
            }

            return SettingsHelper.GetSettingsFor<SearchSetting>(newSettings, "search");
        }

        #endregion

    }
}
