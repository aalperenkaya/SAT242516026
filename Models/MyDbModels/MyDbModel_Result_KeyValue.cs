namespace SAT242516026.Models.MyDbModels;

public class MyDbModel_Result_KeyValue<TKey, TValue>
{
    public TKey Key { get; set; } = default!;
    public TValue Value { get; set; } = default!;
}
