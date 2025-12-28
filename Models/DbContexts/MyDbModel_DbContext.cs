using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SAT242516026.Models.DbContexts;

public sealed class MyDbModel_Context
{
    private readonly string _connectionString;

    public MyDbModel_Context(IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection bulunamadı (appsettings.json).");
    }

    public IDbConnection CreateConnection()
        => new SqlConnection(_connectionString);
}

