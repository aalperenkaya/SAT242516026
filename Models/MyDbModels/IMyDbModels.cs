namespace SAT242516026.Models.MyDbModels;

public interface IMyDbModel<T> where T : class, new()
{
    List<T> Items { get; set; }
    string? Message { get; set; }
    MyDbModel_Parameters Parameters { get; }
}
