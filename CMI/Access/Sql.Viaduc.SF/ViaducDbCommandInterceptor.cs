using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace CMI.Access.Sql.Viaduc.EF
{
    public class ViaducDbCommandInterceptor : IDbCommandInterceptor
    {
        public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            Log.Debug(command.CommandText);
        }

        public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            if (interceptionContext.Exception != null)
            {
                Log.Error(command.CommandText);
                Log.Error(interceptionContext.Exception, interceptionContext.Exception.Message);
            }
        }

        public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            Log.Debug(command.CommandText);
        }

        public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            if (interceptionContext.Exception != null)
            {
                Log.Error(command.CommandText);
                Log.Error(interceptionContext.Exception, interceptionContext.Exception.Message);
            }
        }

        public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            Log.Debug(command.CommandText);
        }

        public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            if (interceptionContext.Exception != null)
            {
                Log.Error(command.CommandText);
                Log.Error(interceptionContext.Exception, interceptionContext.Exception.Message);
            }
        }
    }
}
