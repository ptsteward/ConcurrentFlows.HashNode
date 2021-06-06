using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using static ConcurrentFlows.DapperResiliency.ContextHelper;

namespace ConcurrentFlows.DapperResiliency
{
    public static class SqlResiliencyPolicy
    {
        private static readonly ISet<int> transientNumbers = new HashSet<int>(new[] { 40613, 40197, 40501, 49918, 40549, 40550, 1205 });
        private static readonly ISet<int> networkingNumbers = new HashSet<int>(new[] { 258, -2, 10060, 0, 64, 26, 40, 10053 });
        private static readonly ISet<int> constraintViolationNumbers = new HashSet<int>(new[] { 2627, 547, 2601 });

        public static IAsyncPolicy GetSqlResiliencyPolicy(TimeSpan? maxTimeout = null, int transientRetries = 3, int networkRetries = 3)
        {
            var timeoutPolicy = Policy.TimeoutAsync(maxTimeout ?? TimeSpan.FromMinutes(2));

            var transientPolicy = Policy.Handle<SqlException>(ex => transientNumbers.Contains(ex.Number))
                .WaitAndRetryAsync(
                transientRetries,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (ex, _, ctx) => ctx.GetLogger()?.LogWarning(ex, "{@Operation} Encountered Transient SqlException. Params:{@Param} Sql:{@Sql}", ctx.OperationKey, ctx[ParamContextKey], ctx[SqlContextKey]));

            var networkPolicy = Policy.Handle<SqlException>(ex => networkingNumbers.Contains(ex.Number))
                .WaitAndRetryAsync(
                networkRetries,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (ex, _, ctx) =>
                {
                    ctx.GetLogger()?.LogWarning(ex, "{@Operation} Encountered a Network Error. Params:{@Param} Sql:{@Sql}", ctx.OperationKey, ctx[ParamContextKey], ctx[SqlContextKey]);
                    if (ctx.TryGetConnection(out var connection))
                        SqlConnection.ClearPool(connection);
                });

            var constraintPolicy = Policy.Handle<SqlException>(ex => constraintViolationNumbers.Contains(ex.Number))
                .CircuitBreakerAsync(
                1,
                TimeSpan.MaxValue,
                (ex, _, ctx) => ctx.GetLogger()?.LogError(ex, "{@Operation} Encountered a Constraint Violation. Params:{@Param} Sql:{@Sql}", ctx.OperationKey, ctx[ParamContextKey], ctx[SqlContextKey]),
                ctx => { }
                );

            var resiliencyPolicy = timeoutPolicy
                .WrapAsync(transientPolicy)
                .WrapAsync(networkPolicy)
                .WrapAsync(constraintPolicy);

            return resiliencyPolicy;
        }
    }
}
