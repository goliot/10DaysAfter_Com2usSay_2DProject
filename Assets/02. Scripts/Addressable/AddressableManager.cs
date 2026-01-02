using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AddressableManager : MonoBehaviour
{
    [SerializeField] private List<PoolEntry> _towers;
    [SerializeField] private bool _preloadAllOnStart = true;

    private async void Start()
    {
        await Addressables.InitializeAsync().Task;

        foreach (var e in _towers)
        {
            if (e.type == ETowerType.Count) continue;
            if (e.prefab == null) continue;

            AddressablePoolManager.Instance.Register(e.type, e.prefab, e.prewarm);
        }

        if (_preloadAllOnStart)
        {
            // 필요하면 스테이지/웨이브 기반으로 필요한 타입만 골라서 넘기면 더 좋음
            var types = new List<ETowerType>();
            foreach (var e in _towers)
                if (e.type != ETowerType.Count && e.prefab != null)
                    types.Add(e.type);

            await AddressablePoolManager.Instance.PreloadAsync(types);
        }
    }
}
