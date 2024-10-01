using Oracle.ManagedDataAccess.Client;

namespace WorkerOperaCloud.Context
{
    public class DapperContext
    {
        private readonly IConfiguration _configuration;
        private readonly string? _connectionString;
        public DapperContext(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DbConnection");
        }

        public OracleConnection CreateConnection()
        => new (_connectionString);
    }
}
