using Elastic.Clients.Elasticsearch;
using System;
using System.IO;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport.Extensions;

namespace CMI.Web.Frontend.api.Elastic;

public class QueryContainerJsonConverter
{
    private static readonly ElasticsearchClient client = new(new ElasticsearchClientSettings(new Uri("http://localhost:9200")));

    public string Serialize(Query container)
    {
        return client.RequestResponseSerializer.SerializeToString(container);
    }

    public Query Deserialize(Stream text)
    {
        return client.RequestResponseSerializer.Deserialize<Query>(text);
    }

}