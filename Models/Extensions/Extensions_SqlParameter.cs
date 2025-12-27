using System.Data;
using Microsoft.Data.SqlClient;

namespace SAT242516026.Models.Extensions;

public static class Extensions_SqlParameter
{
    // dbo.Type_Dictionary_String_String TVP
    public static SqlParameter ToSqlParameter_Table_Type_Dictionary(this Dictionary<string, string> dict, string paramName)
    {
        var dt = new DataTable();
        dt.Columns.Add("Key", typeof(string));
        dt.Columns.Add("Value", typeof(string));

        foreach (var kv in dict)
            dt.Rows.Add(kv.Key, kv.Value);

        return new SqlParameter("@" + paramName, dt)
        {
            SqlDbType = SqlDbType.Structured,
            TypeName = "dbo.Type_Dictionary_String_String"
        };
    }

    // scalar param
    public static SqlParameter ToSqlParameter_Data_Type(this object? value, string paramName)
        => new SqlParameter("@" + paramName, value ?? DBNull.Value);
}
