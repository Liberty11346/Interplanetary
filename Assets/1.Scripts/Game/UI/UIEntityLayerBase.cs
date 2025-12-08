using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI 엔티티 레이어의 베이스 클래스
/// 오브젝트 풀링을 사용하여 엔티티를 효율적으로 관리
/// </summary>
/// <typeparam name="TEntity">관리할 엔티티 타입 (예: UIFleet, UIPlanet)</typeparam>
/// <typeparam name="TData">엔티티 데이터 타입 (예: FleetData, PlanetData)</typeparam>
public abstract class UIEntityLayerBase<TEntity, TData> : MonoBehaviour 
    where TEntity : MonoBehaviour
{
    [Header("Prefab Settings")]
    [SerializeField] protected TEntity entityPrefab;

    [Header("Pool Settings")]
    [SerializeField] protected int initialPoolSize = 20;
    [SerializeField] protected int maxPoolSize = 100;

    // 오브젝트 풀
    private Queue<TEntity> _pool = new Queue<TEntity>();
    
    // 활성화된 엔티티들 (ID로 관리)
    private Dictionary<int, TEntity> _activeEntities = new Dictionary<int, TEntity>();

    protected virtual void Awake()
    {
        InitializePool();
    }

    /// <summary>
    /// 오브젝트 풀 초기화
    /// </summary>
    private void InitializePool()
    {
        if (entityPrefab == null)
        {
            Debug.LogError($"[{GetType().Name}] Entity prefab is not assigned!");
            return;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewEntity();
        }

        Debug.Log($"[{GetType().Name}] Pool initialized with {initialPoolSize} entities");
    }

    /// <summary>
    /// 새 엔티티를 생성하고 풀에 추가
    /// </summary>
    private TEntity CreateNewEntity()
    {
        TEntity entity = Instantiate(entityPrefab, transform);
        entity.gameObject.SetActive(false);
        _pool.Enqueue(entity);
        return entity;
    }

    /// <summary>
    /// 풀에서 엔티티를 가져오거나 새로 생성
    /// </summary>
    protected TEntity GetFromPool()
    {
        TEntity entity;

        if (_pool.Count > 0)
        {
            entity = _pool.Dequeue();
        }
        else
        {
            if (_activeEntities.Count >= maxPoolSize)
            {
                Debug.LogWarning($"[{GetType().Name}] Max pool size ({maxPoolSize}) reached!");
                return null;
            }

            entity = CreateNewEntity();
            _pool.Dequeue(); // 방금 추가한 것을 다시 꺼냄
        }

        entity.gameObject.SetActive(true);
        return entity;
    }

    /// <summary>
    /// 엔티티를 풀로 반환
    /// </summary>
    protected void ReturnToPool(TEntity entity)
    {
        if (entity == null) return;

        entity.gameObject.SetActive(false);
        entity.transform.SetParent(transform);
        _pool.Enqueue(entity);
    }

    /// <summary>
    /// 엔티티 생성 또는 업데이트
    /// </summary>
    public TEntity CreateOrUpdateEntity(int id, TData data)
    {
        // 이미 존재하는 엔티티면 업데이트
        if (_activeEntities.TryGetValue(id, out TEntity existingEntity))
        {
            UpdateEntity(existingEntity, data);
            return existingEntity;
        }

        // 새 엔티티 생성
        TEntity newEntity = GetFromPool();
        if (newEntity == null) return null;

        _activeEntities[id] = newEntity;
        InitializeEntity(newEntity, id, data);
        
        return newEntity;
    }

    /// <summary>
    /// 엔티티 제거
    /// </summary>
    public void RemoveEntity(int id)
    {
        if (_activeEntities.TryGetValue(id, out TEntity entity))
        {
            _activeEntities.Remove(id);
            OnEntityRemoved(entity, id);
            ReturnToPool(entity);
        }
    }

    /// <summary>
    /// 모든 엔티티 제거
    /// </summary>
    public void ClearAllEntities()
    {
        foreach (var kvp in _activeEntities)
        {
            OnEntityRemoved(kvp.Value, kvp.Key);
            ReturnToPool(kvp.Value);
        }
        _activeEntities.Clear();
    }

    /// <summary>
    /// 특정 ID의 엔티티 가져오기
    /// </summary>
    public TEntity GetEntity(int id)
    {
        _activeEntities.TryGetValue(id, out TEntity entity);
        return entity;
    }

    /// <summary>
    /// 활성화된 엔티티 개수
    /// </summary>
    public int ActiveEntityCount => _activeEntities.Count;

    /// <summary>
    /// 풀에 남아있는 엔티티 개수
    /// </summary>
    public int PooledEntityCount => _pool.Count;

    /// <summary>
    /// 활성화된 모든 엔티티 ID 목록
    /// </summary>
    public IEnumerable<int> GetActiveEntityIds()
    {
        return _activeEntities.Keys;
    }

    #region Abstract/Virtual Methods - 하위 클래스에서 구현

    /// <summary>
    /// 엔티티 초기화 (생성 시 호출)
    /// </summary>
    protected abstract void InitializeEntity(TEntity entity, int id, TData data);

    /// <summary>
    /// 엔티티 업데이트 (이미 존재하는 엔티티 업데이트 시 호출)
    /// </summary>
    protected abstract void UpdateEntity(TEntity entity, TData data);

    /// <summary>
    /// 엔티티 제거 시 호출 (정리 작업 수행)
    /// </summary>
    protected virtual void OnEntityRemoved(TEntity entity, int id)
    {
        // 기본 구현은 비어있음, 필요시 오버라이드
    }

    #endregion

    protected virtual void OnDestroy()
    {
        ClearAllEntities();
        _pool.Clear();
    }
}
