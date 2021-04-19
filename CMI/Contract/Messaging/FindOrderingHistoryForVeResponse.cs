using System.Collections.Generic;
using CMI.Contract.Order;

namespace CMI.Contract.Messaging
{
    public class FindOrderingHistoryForVeResponse
    {
        public List<Bestellhistorie> History { get; set; }
    }
}