using SAT242516026.Models.MyDbModels;

namespace SAT242516026.Models.UnitOfWorks;

public interface IMyDbModel_UnitOfWork
{
    Task Execute<T>(IMyDbModel<T> myDbModel, string spName, bool isPagination = true)
        where T : class, new();

    Task<List<T>> SetItems<T>(string spName, params (string Key, object? Value)[] parameters)
        where T : class, new();
}
