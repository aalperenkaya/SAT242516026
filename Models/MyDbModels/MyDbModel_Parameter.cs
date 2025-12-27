namespace SAT242516026.Models.MyDbModels;

public class MyDbModel_Parameters
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string OrderBy { get; set; } = "Id desc";

    public int TotalRecordCount { get; set; }
    public int TotalPageCount { get; set; }

    public Dictionary<string, string>? Where { get; set; }
    public Dictionary<string, string>? Params { get; set; }
}
