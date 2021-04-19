using System.Data.Linq;

namespace CMI.Access.Sql.Viaduc
{
    public class ViaducContext : DataContext
    {
        public ViaducContext(string fileOrServerOrConnection) : base(fileOrServerOrConnection)
        {
        }

        public Table<OrderingFlatItem> OrderingFlatItem => GetTable<OrderingFlatItem>();
        public Table<OrderingFlatDetailItem> OrderingFlatDetailItem => GetTable<OrderingFlatDetailItem>();

        public Table<UserOverview> UserOverview => GetTable<UserOverview>();
    }
}