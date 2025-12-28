using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SAT242516026.Data;

public sealed class ApplicationDbContext : IDisposable
{
    private readonly string _connectionString;

    public ApplicationDbContext(IConfiguration configuration)
    {
        // appsettings.json: ConnectionStrings:DefaultConnection
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection bulunamadı.");
    }

    public SqlConnection CreateConnection()
        => new SqlConnection(_connectionString);

    public SqlCommand CreateStoredProcedure(SqlConnection conn, string spName, SqlTransaction? tx = null)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = spName;
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Transaction = tx;
        return cmd;
    }

    public void Dispose()
    {
   
    }
}
