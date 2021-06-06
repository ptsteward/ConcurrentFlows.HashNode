using Microsoft.Extensions.DependencyInjection;
using System.Data.SqlClient;
using static ConcurrentFlows.DapperResiliency.SqlConnectionDelegate;

namespace ConcurrentFlows.DapperResiliency
{
    public static class RegistrationExtensions
    {
        public static void AddSqlDapperClient(this IServiceCollection services, string connectionString)
        {
            services.AddSingleton<SqlConnectionFactory>(() => new SqlConnection(connectionString));
            services.AddScoped(_ => SqlResiliencyPolicy.GetSqlResiliencyPolicy());
            services.AddScoped<ISqlDapperClient, SqlDapperClient>();
        }
    }
}
