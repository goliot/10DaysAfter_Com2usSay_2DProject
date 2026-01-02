using UnityEngine.AddressableAssets;

[System.Serializable]
public class PoolEntry
{
    public ETowerType type;
    public AssetReferenceGameObject prefab;
    public int prewarm = 10;
}
