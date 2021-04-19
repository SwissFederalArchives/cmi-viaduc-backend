using System;

namespace CMI.Web.Frontend.api.Search
{
    public class UnknownElasticSearchFieldException : Exception
    {
        public UnknownElasticSearchFieldException(string fieldKey) : base($"{fieldKey} is not a known search field.")
        {
            InvalidSearchField = fieldKey;
        }

        public string InvalidSearchField { get; set; }
    }
}