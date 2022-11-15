using System.Collections.Generic;
using CMI.Contract.Common.Entities;

namespace CMI.Contract.Messaging
{
    public class GetManuelleKorrekturRequest
    {
        public int ManuelleKorrekturId { get; set; }
    }

    public class GetManuelleKorrekturResponse
    {
        public ManuelleKorrekturDetailItem ManuelleKorrektur { get; set; }
    }

    public class InsertOrUpdateManuelleKorrekturRequest
    {
        public ManuelleKorrekturDto ManuelleKorrektur { get; set; }
        public string UserId { get; set; }
    }

    public class InsertOrUpdateManuelleKorrekturResponse
    {
        public ManuelleKorrekturDto ManuelleKorrektur { get; set; }
    }

    public class DeleteManuelleKorrekturRequest
    {
        public int ManuelleKorrekturId { get; set; }
    }
    public class DeleteManuelleKorrekturResponse{}

    public class BatchDeleteManuelleKorrekturRequest
    {
        public int[] ManuelleKorrekturIds { get; set; }
    }
    public class BatchDeleteManuelleKorrekturResponse{}

    public class BatchAddManuelleKorrekturRequest
    {
       public  string[] Identifiers { get; set; }
       public string UserId { get; set; }
    }

    public class BatchAddManuelleKorrekturResponse
    {
        public Dictionary<string, string> Result { get; set; }
    }

    public class PublizierenManuelleKorrekturRequest
    {
        public int Id { get; set; }
        public string UserId { get; set; }
    }

    public class PublizierenManuelleKorrekturResponse
    {
        public ManuelleKorrekturDto ManuelleKorrektur { get; set; }
    }
}
