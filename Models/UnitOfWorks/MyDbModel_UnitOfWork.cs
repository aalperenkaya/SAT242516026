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

            using var cmd = con.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = spName;
            cmd.Parameters.Clear();

            // Pagination TVP
            if (isPagination)
            {
                var pagination = new Dictionary<string, string>
                {
                    { "PageNumber", myDbModel.Parameters.PageNumber.ToString() },
                    { "PageSize", myDbModel.Parameters.PageSize.ToString() },
                    { "OrderBy", string.IsNullOrWhiteSpace(myDbModel.Parameters.OrderBy) ? "Id desc" : myDbModel.Parameters.OrderBy },
                };

                cmd.Parameters.Add(pagination.ToSqlParameter_Table_Type_Dictionary("pagination"));
            }

            // Where TVP (boş da olsa gönder)
            var whereDict = myDbModel.Parameters.Where ?? new Dictionary<string, string>();
            cmd.Parameters.Add(whereDict.ToSqlParameter_Table_Type_Dictionary("where"));

            // Extra params (scalar)
            if (myDbModel.Parameters.Params?.Any() == true)
            {
                foreach (var p in myDbModel.Parameters.Params)
                    cmd.Parameters.Add(p.Value.ToSqlParameter_Data_Type(p.Key));
            }

            using var reader = await cmd.ExecuteReaderAsync();

            // 1) Items
            var dtItems = new DataTable();
            dtItems.Load(reader);
            var items = dtItems.DataTableToList<T>().ToList();

            // 2) Meta
            int totalRecordCount = 0;
            int totalPageCount = 1;
            int pageNumber = myDbModel.Parameters.PageNumber;
            int pageSize = myDbModel.Parameters.PageSize;

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

            using var cmd = con.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = spName;
            cmd.Parameters.Clear();

            foreach (var (key, value) in parameters)
                cmd.Parameters.Add(value.ToSqlParameter_Data_Type(key));

            using var reader = await cmd.ExecuteReaderAsync();
            var dt = new DataTable();
            dt.Load(reader);

            return dt.DataTableToList<T>().ToList();
        }
        finally
        {
            if (initialState != ConnectionState.Open)
                con.Close();
        }
    }
}
