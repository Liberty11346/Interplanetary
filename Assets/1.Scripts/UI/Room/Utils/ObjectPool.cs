using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : Component
{
    private T prefab;
    private Transform container;
    private Queue<T> pool = new Queue<T>();
    private HashSet<T> activeObjects = new HashSet<T>();

    public ObjectPool(T prefab, Transform container, int initialSize = 10)
    {
        this.prefab = prefab;
        this.container = container;

        if (prefab == null)
        {
            Debug.LogError("ObjectPool: Prefab is null!");
            return;
        }

        if (container == null)
        {
            Debug.LogError("ObjectPool: Container is null!");
            return;
        }

        for (int i = 0; i < initialSize; i++)
        {
            CreateNew();
        }
    }

    private T CreateNew()
    {
        T obj = GameObject.Instantiate(prefab, container);
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
        return obj;
    }

    public T Get(Transform parent = null)
    {
        T obj;

        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else
        {
            Debug.LogWarning($"ObjectPool<{typeof(T).Name}>: Pool exhausted. Creating new instance.");
            obj = CreateNew();
            pool.Dequeue();
        }

        if (parent != null)
        {
            obj.transform.SetParent(parent);
        }

        obj.gameObject.SetActive(true);
        activeObjects.Add(obj);

        return obj;
    }

    public void Return(T obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("ObjectPool: Trying to return null object.");
            return;
        }

        if (!activeObjects.Contains(obj))
        {
            Debug.LogWarning($"ObjectPool<{typeof(T).Name}>: Object is not from this pool.");
            return;
        }

        obj.gameObject.SetActive(false);
        obj.transform.SetParent(container);

        activeObjects.Remove(obj);
        pool.Enqueue(obj);
    }

    public void ReturnAll()
    {
        var objectsToReturn = new List<T>(activeObjects);

        foreach (var obj in objectsToReturn)
        {
            Return(obj);
        }
    }

    public void Clear()
    {
        foreach (var obj in activeObjects)
        {
            if (obj != null)
                GameObject.Destroy(obj.gameObject);
        }

        while (pool.Count > 0)
        {
            var obj = pool.Dequeue();
            if (obj != null)
                GameObject.Destroy(obj.gameObject);
        }

        activeObjects.Clear();
        pool.Clear();
    }

    public int ActiveCount => activeObjects.Count;
    public int PoolCount => pool.Count;
    public int TotalCount => ActiveCount + PoolCount;
}
