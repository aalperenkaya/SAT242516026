using System.Data;
using System.Reflection;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;


namespace SAT242516026.Models.MyDbModels;

public class MyDbModel_Provider : IMyDbModel_Provider
{
    private readonly string _cs;

    public MyDbModel_Provider(IConfiguration config)
    {
        _cs = config.GetConnectionString("DefaultConnection")!;
    }

    public IMyDbModel<T> Create<T>() where T : class, new() => new MyDbModel<T>();

    public async Task Execute<T>(IMyDbModel<T> model, string spName, bool isPagination = true) where T : class, new()
    {
        model.Message = null;
        model.Items = new();

        try
        {
            await using var con = new SqlConnection(_cs);
            await con.OpenAsync();

            await using var cmd = new SqlCommand(spName, con)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Provider her zaman bu 3 paramı gönderiyor → SP’lerde de olacak
            var whereJson = model.Parameters.Where is null ? null : JsonSerializer.Serialize(model.Parameters.Where);
            var paramsJson = model.Parameters.Params is null ? null : JsonSerializer.Serialize(model.Parameters.Params);

            Dictionary<string, string>? pagDict = null;
            if (isPagination)
            {
                pagDict = new Dictionary<string, string>
                {
                    ["PageNumber"] = model.Parameters.PageNumber.ToString(),
                    ["PageSize"] = model.Parameters.PageSize.ToString(),
                    ["OrderBy"] = model.Parameters.OrderBy ?? "Id desc"
                };
            }
            var paginationJson = pagDict is null ? null : JsonSerializer.Serialize(pagDict);

            cmd.Parameters.Add(new SqlParameter("@where", SqlDbType.NVarChar) { Value = (object?)whereJson ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@params", SqlDbType.NVarChar) { Value = (object?)paramsJson ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@pagination", SqlDbType.NVarChar) { Value = (object?)paginationJson ?? DBNull.Value });

            await using var rd = await cmd.ExecuteReaderAsync();
            var list = new List<T>();

            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

            while (await rd.ReadAsync())
            {
                var item = new T();

                for (int i = 0; i < rd.FieldCount; i++)
                {
                    var col = rd.GetName(i);
                    if (!props.TryGetValue(col, out var prop)) continue;

                    var val = rd.IsDBNull(i) ? null : rd.GetValue(i);
                    if (val is null) { prop.SetValue(item, null); continue; }

                    var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    prop.SetValue(item, Convert.ChangeType(val, targetType));
                }

                // Pagination kolonlarını SP döndürsün (biz ilk satırdan yakalayacağız)
                if (rd.HasColumn("TotalRecordCount"))
                    model.Parameters.TotalRecordCount = rd.GetInt32Safe("TotalRecordCount");

                if (rd.HasColumn("TotalPageCount"))
                    model.Parameters.TotalPageCount = rd.GetInt32Safe("TotalPageCount");

                list.Add(item);
            }

            model.Items = list;
            if (model.Parameters.TotalPageCount <= 0) model.Parameters.TotalPageCount = 1;
        }
        catch (Exception ex)
        {
            model.Message = ex.Message;
        }
    }

    public async Task<List<T>> SetItems<T>(string spName, params (string Key, object? Value)[] parameters) where T : class, new()
    {
        var result = new List<T>();

        await using var con = new SqlConnection(_cs);
        await con.OpenAsync();

        await using var cmd = new SqlCommand(spName, con) { CommandType = CommandType.StoredProcedure };

        foreach (var (Key, Value) in parameters)
            cmd.Parameters.AddWithValue(Key, Value ?? DBNull.Value);

        await using var rd = await cmd.ExecuteReaderAsync();

        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        while (await rd.ReadAsync())
        {
            var item = new T();

            for (int i = 0; i < rd.FieldCount; i++)
            {
                var col = rd.GetName(i);
                if (!props.TryGetValue(col, out var prop)) continue;

                var val = rd.IsDBNull(i) ? null : rd.GetValue(i);
                if (val is null) { prop.SetValue(item, null); continue; }

                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                prop.SetValue(item, Convert.ChangeType(val, targetType));
            }

            result.Add(item);
        }

        return result;
    }
}

internal static class SqlReaderExt
{
    public static bool HasColumn(this SqlDataReader reader, string name)
    {
        for (int i = 0; i < reader.FieldCount; i++)
            if (string.Equals(reader.GetName(i), name, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    public static int GetInt32Safe(this SqlDataReader reader, string name)
    {
        try
        {
            var ord = reader.GetOrdinal(name);
            return reader.IsDBNull(ord) ? 0 : Convert.ToInt32(reader.GetValue(ord));
        }
        catch { return 0; }
    }
}
    