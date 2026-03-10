using System.IO;
using System.Threading.Tasks;
using CommonServiceLocator;
using Serilog;
using SolrNet;

namespace CMI.Engine.Asset.Solr;

public interface ISolrEngine
{
    Task<bool> UploadDocument(SolrRecord document);
    Task<bool> DeleteDocumentsByQuery(string solrRecordFieldQuery);
    SolrConnectionInfo ConnectionInfo { get; }
}

public class SolrEngine : ISolrEngine
{
    public SolrConnectionInfo ConnectionInfo { get; }
    protected AddParameters addParameters;
    protected ISolrOperations<SolrRecord> solr;


    public SolrEngine(SolrConnectionInfo solrConnectionInfo)
    {
        ConnectionInfo = solrConnectionInfo;
        if (!ConnectionInfo.SolrUrl.Equals("SkipSolrForTesting"))
        {
            Startup.InitContainer();
            Startup.Init<SolrRecord>(ConnectionInfo.SolrUrl + ConnectionInfo.SolrCoreName);
            solr = ServiceLocator.Current.GetInstance<ISolrOperations<SolrRecord>>();
            if (!Directory.Exists(ConnectionInfo.SolrHighlightingPath))
            {
                Directory.CreateDirectory(ConnectionInfo.SolrHighlightingPath);
            }

            addParameters = new AddParameters
            {
                CommitWithin = 1000,
                Overwrite = true
            };
        }
        else
        {
            Log.Warning("Skip Solr for testing is enabled");
        }
    }

    public async Task<bool> UploadDocument(SolrRecord document)
    {
        if (ConnectionInfo.SolrUrl.Equals("SkipSolrForTesting"))
        {
            Log.Warning("Skip Solr for testing is enabled");
            return true;
        }
        Log.Debug("Solr upload document {Id}", document.Id);
        var result = solr.AddAsync(document, addParameters);
        if (result.Status == 0)
        {
            var commitResult = await solr.CommitAsync();
            if (commitResult.Status == 0)
            {
                return true;
            }
        }

        return false;
    }

    public async Task<bool> DeleteDocumentsByQuery(string solrRecordFieldQuery)
    {
        if (ConnectionInfo.SolrUrl.Equals("SkipSolrForTesting"))
        {
            Log.Warning("Skip Solr for testing is enabled");
            return true;
        }

        Log.Debug("Solr delete documents by Query {solrRecordFieldQuery}", solrRecordFieldQuery);
        solrRecordFieldQuery = solrRecordFieldQuery.Replace("/", @"\/") + @"\/*";
        var query = new SolrQuery(@"id:" + solrRecordFieldQuery);
        var result = await solr.DeleteAsync(query);
        if (result.Status == 0)
        {
            var commitResult = await solr.CommitAsync();
            if (commitResult.Status == 0)
            {
                return true;
            }
        }

        return false;
    }
}
