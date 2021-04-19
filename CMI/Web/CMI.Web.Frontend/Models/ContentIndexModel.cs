using System;
using System.Collections.Generic;

namespace CMI.Web.Frontend.Models
{
    public class ContentIndexModel
    {
        public string BaseUrl { get; set; }

        public bool EditMode { get; set; }

        public Dictionary<string, string> LanguageLinks { get; set; }
    }

    public class ErrorModel
    {
        public string BaseUrl { get; set; }
        public Dictionary<string, string> LanguageLinks { get; set; }
        public string ErrorId { get; set; }
        public string Url { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.Now;
    }
}