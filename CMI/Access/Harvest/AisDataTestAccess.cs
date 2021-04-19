using CMI.Contract.Harvest;

namespace CMI.Access.Harvest
{
    public partial class AISDataAccess : IDbTestAccess
    {
        public string GetDbVersion()
        {
            return dataProvider.GetDbVersion();
        }
    }
}