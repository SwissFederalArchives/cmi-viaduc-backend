using System;
using System.IO;
using Elasticsearch.Net;
using Nest;
using Nest.JsonNetSerializer;

namespace CMI.Web.Frontend.api.Elastic;

public class QueryContainerJsonConverter
{
    private static readonly ElasticClient client = new ElasticClient(new ConnectionSettings(
        new SingleNodeConnectionPool(new Uri("http://localhost:9200")), new InMemoryConnection(), sourceSerializer: JsonNetSerializer.Default));

    public string Serialize(IQueryContainer container)
    {
        return client.RequestResponseSerializer.SerializeToString(container);
    }

    public QueryContainer Deserialize(Stream text)
    {
        return client.RequestResponseSerializer.Deserialize<QueryContainer>(text);
    }
}