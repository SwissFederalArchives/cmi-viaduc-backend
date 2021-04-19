using CMI.Contract.Common;

namespace CMI.Manager.Index.ValueExtractors
{
    public class HyperlinkExtractor : ExtractorBase<ElasticHyperlink>
    {
        protected override ElasticHyperlink GetValueInternal(DataElementElementValue value, DataElement dataElement)
        {
            ElasticHyperlink retVal = null;
            if (value != null)
            {
                retVal = new ElasticHyperlink
                {
                    Text = value.Link.Value,
                    Url = value.Link.Href
                };
            }

            return retVal;
        }
    }
}