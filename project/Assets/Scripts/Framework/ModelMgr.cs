using System;
using System.Collections.Generic;

public sealed class ModelMgr : MonoSingle<ModelMgr>
{
    private readonly HashSet<Type> registeredTypes = new();

    protected override void Init()
    {
        Register(NakamaModel.I);
        Register(UserDataModel.I);
    }

    public void Register<T>(T model) where T : class
    {
        if (model == null)
        {
            return;
        }

        var modelType = model.GetType();
        if (!registeredTypes.Add(modelType))
        {
            return;
        }

        if (model is IModel modelLifecycle)
        {
            modelLifecycle.Init();
        }
    }
}

public interface IModel
{
    void Init();
}
