using UnityEngine;

public abstract class MonoSingle<T> : MonoBehaviour where T : MonoSingle<T>
{
    private static T instance;
    private bool isInitialized;

    public static T I
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<T>();
            }

            if (instance == null)
            {
                var gameObject = new GameObject(typeof(T).Name);
                instance = gameObject.AddComponent<T>();
                instance.TryInit();
            }

            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = (T)this;
        DontDestroyOnLoad(gameObject);
        TryInit();
    }

    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    protected virtual void Init()
    {
    }

    private void TryInit()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        Init();
    }
}
