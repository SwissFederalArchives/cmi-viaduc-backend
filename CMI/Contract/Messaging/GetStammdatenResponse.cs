using System.Collections.Generic;

namespace CMI.Contract.Messaging
{
    public class GetStammdatenResponse
    {
        public IEnumerable<NameAndId> NamesAndIds { get; set; }
    }
}