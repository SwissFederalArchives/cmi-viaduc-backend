using System;

namespace CMI.Access.Sql.Viaduc
{
    public abstract class DataAccess
    {
        protected readonly string EOL = Environment.NewLine;

        protected readonly string TAB = "\t";

        protected static object ToDb(object obj)
        {
            if (obj == null ||
                string.Empty.Equals(obj))
            {
                return DBNull.Value;
            }

            return obj;
        }
    }
}