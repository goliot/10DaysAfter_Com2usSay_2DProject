using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading.Tasks;
using static UnityEditor.PlayerSettings;

public class TowerPoolManager : Singleton<TowerPoolManager>
{
    private readonly Dictionary<ETowerType, PoolEntry> _entryByType = new();
    private readonly Dictionary<ETowerType, Queue<GameObject>> _pool = new();

    private readonly Dictionary<ETowerType, AsyncOperationHandle<GameObject>> _loadedPrefabHandles = new();
    private readonly List<GameObject> _allInstances = new();

    public void Register(ETowerType type, AssetReferenceGameObject prefab, int prewarm)
    {
        if (type == ETowerType.Count) return;          // 안전장치
        if (_entryByType.ContainsKey(type)) return;

        var entry = new PoolEntry { type = type, prefab = prefab, prewarm = prewarm };
        _entryByType[type] = entry;
        _pool[type] = new Queue<GameObject>(prewarm);
    }

    public async Task PreloadAsync(IEnumerable<ETowerType> types)
    {
        foreach (var type in types)
        {
            if (!_entryByType.TryGetValue(type, out var entry)) continue;

            if (!_loadedPrefabHandles.ContainsKey(type))
            {
                var h = entry.prefab.LoadAssetAsync<GameObject>();
                await h.Task;
                _loadedPrefabHandles[type] = h;
            }

            while (_pool[type].Count < entry.prewarm)
            {
                var instHandle = entry.prefab.InstantiateAsync(transform);
                var go = await instHandle.Task;
                go.SetActive(false);

                _allInstances.Add(go);
                _pool[type].Enqueue(go);
            }
        }
    }

    public GameObject GetObject(ETowerType type)
    {
        if (_pool.TryGetValue(type, out var q) && q.Count > 0)
        {
            var go = q.Dequeue();
            go.SetActive(true);
            return go;
        }

        Debug.LogError($"Pool empty: {type}. Prewarm 부족 or Preload 누락");
        return null;
    }

    public GameObject Get(ETowerType type, Vector3 pos, Quaternion rot)
    {
        if (_pool.TryGetValue(type, out var q) && q.Count > 0)
        {
            var go = q.Dequeue();
            go.transform.SetPositionAndRotation(pos, rot);
            go.SetActive(true);
            return go;
        }

        Debug.LogError($"Pool empty: {type}. Prewarm 부족 or Preload 누락");
        return null;
    }

    public void Return(ETowerType type, GameObject go)
    {
        go.SetActive(false);
        go.transform.SetParent(transform);
        _pool[type].Enqueue(go);
    }

    public void ReleaseAll()
    {
        foreach (var go in _allInstances)
            Addressables.ReleaseInstance(go);
        _allInstances.Clear();

        foreach (var kv in _loadedPrefabHandles)
            Addressables.Release(kv.Value);
        _loadedPrefabHandles.Clear();

        foreach (var q in _pool.Values) q.Clear();
        _entryByType.Clear();
    }
}