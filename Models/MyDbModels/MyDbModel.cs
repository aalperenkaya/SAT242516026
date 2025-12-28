namespace SAT242516026.Models.MyDbModels;

public sealed class MyDbModel<T> : IMyDbModel<T> where T : class, new()
{
    public List<T> Items { get; set; } = new();
    public string? Message { get; set; }

    public int TotalCount { get; set; }       
    public MyDbModel_Parameters Parameters { get; } = new();
}
