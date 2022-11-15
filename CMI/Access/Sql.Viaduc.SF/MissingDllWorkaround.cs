using System.Data.Entity.SqlServer;

namespace CMI.Access.Sql.Viaduc.EF
{

    internal static class MissingDllWorkaroiund
    {
        // Must reference a type in EntityFramework.SqlServer.dll so that this dll will be
        // included in the output folder of referencing projects without requiring a direct 
        // dependency on Entity Framework. See http://stackoverflow.com/a/22315164/1141360.
        private static SqlProviderServices instance = SqlProviderServices.Instance;
    }
}