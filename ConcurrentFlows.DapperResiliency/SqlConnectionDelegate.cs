using System.Data.SqlClient;

namespace ConcurrentFlows.DapperResiliency
{
    public class SqlConnectionDelegate
    {
        public delegate SqlConnection SqlConnectionFactory();
    }
}
