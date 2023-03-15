using SolrNet.Attributes;

namespace CMI.Engine.Asset.Solr
{
    public class SolrRecord
    {
        [SolrUniqueKey("id")]
        public string Id { get; set; }

        [SolrField("source")]
        public string Source { get; set; }

        [SolrField("ocr_text")] 
        public string OCRText { get; set; }

        [SolrField("title")]
        public string Title { get; set; }

        [SolrField("image_url")]
        public string ImageUrl { get; set; }

        [SolrField("archive_record_id")]
        public string ArchiveRecordId { get; set; }
    }
}
