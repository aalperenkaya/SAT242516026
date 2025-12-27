namespace SAT242516026.Models.MyDbModels;

public interface IMyDbModel_Provider
{
    IMyDbModel<T> Create<T>() where T : class, new();
    Task Execute<T>(IMyDbModel<T> model, string spName, bool isPagination = true) where T : class, new();

    Task<List<T>> SetItems<T>(string spName, params (string Key, object? Value)[] parameters) where T : class, new();
}

