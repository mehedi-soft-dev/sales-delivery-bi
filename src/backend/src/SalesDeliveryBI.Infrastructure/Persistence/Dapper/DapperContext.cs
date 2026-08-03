using Microsoft.Extensions.Configuration;
using Npgsql;

namespace SalesDeliveryBI.Infrastructure.Persistence.Dapper;

public class DapperContext
{
    private readonly string _connectionString;

    public DapperContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SalesDeliveryBi")
            ?? throw new InvalidOperationException("Missing 'ConnectionStrings:SalesDeliveryBi' configuration.");
    }

    public NpgsqlConnection CreateConnection() => new(_connectionString);
}
