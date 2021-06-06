using Microsoft.Extensions.Logging;
using Polly;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace ConcurrentFlows.DapperResiliency
{
    public static class ContextHelper
    {
        public static readonly string LoggerContextKey = nameof(LoggerContextKey);
        public static readonly string SqlContextKey = nameof(SqlContextKey);
        public static readonly string ParamContextKey = nameof(ParamContextKey);
        public static readonly string ConnectionContextKey = nameof(ConnectionContextKey);

        public static Context NewContext(
            SqlConnection connection,
            ILogger logger,
            string sql,
            object param,
            string operationKey)
        {
            return new Context(operationKey, new Dictionary<string, object>()
            {
                { ConnectionContextKey, connection },
                { LoggerContextKey, logger },
                { SqlContextKey, sql },
                { ParamContextKey, param }
            });
        }

        public static ILogger GetLogger(this Context ctx)
            => ctx[LoggerContextKey] as ILogger;

        public static bool TryGetConnection(this Context ctx, out SqlConnection connection)
            => (connection = ctx[ConnectionContextKey] as SqlConnection) is not null ? true : false;
    }
}
