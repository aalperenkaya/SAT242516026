using System.Data;
using Microsoft.EntityFrameworkCore;
using SAT242516026.Models.Extensions;
using SAT242516026.Models.MyDbModels;

namespace SAT242516026.Models.UnitOfWorks;

public sealed class MyDbModel_UnitOfWork<TDbContext>(TDbContext context)
    : IMyDbModel_UnitOfWork where TDbContext : DbContext
{
    private readonly DbContext _context = context;

    public async Task Execute<T>(IMyDbModel<T> myDbModel, string spName, bool isPagination = true)
        where T : class, new()
    {
        var con = _context.Database.GetDbConnection();
        var initialState = con.State;

        try
        {
            if (initialState != ConnectionState.Open)
                await con.OpenAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = spName;
            cmd.Parameters.Clear();

            // 1) Pagination TVP
            if (isPagination)
            {
                var pagination = new Dictionary<string, string>
                {
                    ["PageNumber"] = (myDbModel.Parameters.PageNumber <= 0 ? 1 : myDbModel.Parameters.PageNumber).ToString(),
                    ["PageSize"] = (myDbModel.Parameters.PageSize <= 0 ? 10 : myDbModel.Parameters.PageSize).ToString(),
                    ["OrderBy"] = string.IsNullOrWhiteSpace(myDbModel.Parameters.OrderBy) ? "Id desc" : myDbModel.Parameters.OrderBy.Trim(),
                };

                // TVP param adı: "pagination" (SP tarafı ne bekliyorsa o!)
                cmd.Parameters.Add(pagination.ToSqlParameter_Table_Type_Dictionary("pagination"));
            }

            // 2) Where TVP (boş da olsa gönder)
            // -> SP tarafında JOIN/WHERE builder bunu kaldırabiliyor olmalı.
            var whereDict = myDbModel.Parameters.Where ?? new Dictionary<string, string>();
            cmd.Parameters.Add(whereDict.ToSqlParameter_Table_Type_Dictionary("where"));

            // 3) Extra scalar params
            if (myDbModel.Parameters.Params?.Any() == true)
            {
                foreach (var p in myDbModel.Parameters.Params)
                {
                    // p.Key zaten param adı, p.Value obje
                    cmd.Parameters.Add(p.Value.ToSqlParameter_Data_Type(p.Key));
                }
            }

            await using var reader = await cmd.ExecuteReaderAsync();

            // 1) Items
            var dtItems = new DataTable();
            dtItems.Load(reader);
            var items = dtItems.DataTableToList<T>().ToList();

            // default meta
            int totalRecordCount = 0;
            int totalPageCount = 1;
            int pageNumber = myDbModel.Parameters.PageNumber;
            int pageSize = myDbModel.Parameters.PageSize;

            // 2) Meta (pagination varsa 2. result set)
            if (isPagination && await reader.NextResultAsync())
            {
                var dtMeta = new DataTable();
                dtMeta.Load(reader);

                if (dtMeta.Rows.Count > 0)
                {
                    var row = dtMeta.Rows[0];

                    if (dtMeta.Columns.Contains("TotalRecordCount"))
                        totalRecordCount = Convert.ToInt32(row["TotalRecordCount"]);

                    if (dtMeta.Columns.Contains("TotalPageCount"))
                        totalPageCount = Convert.ToInt32(row["TotalPageCount"]);

                    if (dtMeta.Columns.Contains("PageNumber"))
                        pageNumber = Convert.ToInt32(row["PageNumber"]);

                    if (dtMeta.Columns.Contains("PageSize"))
                        pageSize = Convert.ToInt32(row["PageSize"]);
                }
            }

            // clamp
            if (totalPageCount <= 0) totalPageCount = 1;
            if (pageNumber <= 0) pageNumber = 1;
            if (pageNumber > totalPageCount) pageNumber = totalPageCount;

            myDbModel.Parameters.TotalRecordCount = totalRecordCount;
            myDbModel.Parameters.TotalPageCount = totalPageCount;
            myDbModel.Parameters.PageNumber = pageNumber;
            myDbModel.Parameters.PageSize = pageSize;

            myDbModel.Items = items;
            myDbModel.Message = null;
        }
        catch (Exception ex)
        {
            myDbModel.Message = $"{spName}: {ex.InnerException?.Message ?? ex.Message}";
            myDbModel.Items = new List<T>();
            myDbModel.Parameters.TotalRecordCount = 0;
            myDbModel.Parameters.TotalPageCount = 1;
            myDbModel.Parameters.PageNumber = 1;
        }
        finally
        {
            if (initialState != ConnectionState.Open)
                con.Close();
        }
    }

    public async Task<List<T>> SetItems<T>(string spName, params (string Key, object? Value)[] parameters)
        where T : class, new()
    {
        var con = _context.Database.GetDbConnection();
        var initialState = con.State;

        try
        {
            if (initialState != ConnectionState.Open)
                await con.OpenAsync();

            await using var cmd = con.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = spName;
            cmd.Parameters.Clear();

            foreach (var (key, value) in parameters)
                cmd.Parameters.Add(value.ToSqlParameter_Data_Type(key));

            await using var reader = await cmd.ExecuteReaderAsync();

            var dt = new DataTable();
            dt.Load(reader);

            return dt.DataTableToList<T>().ToList();
        }
        catch (Exception ex)
        {
            // SetItems'ta sessizce patlamasın: çağıran taraf hata yakalayabilsin
            throw new Exception($"{spName}: {ex.InnerException?.Message ?? ex.Message}", ex);
        }
        finally
        {
            if (initialState != ConnectionState.Open)
                con.Close();
        }
    }
}
