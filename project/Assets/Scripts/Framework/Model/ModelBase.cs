public abstract class ModelBase<T> : IModel where T : ModelBase<T>, new()
{
    private static T instance;

    public static T I => instance ??= new T();

    public virtual void Init()
    {
    }
}
