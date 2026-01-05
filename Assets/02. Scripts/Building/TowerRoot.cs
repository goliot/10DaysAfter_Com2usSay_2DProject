using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TowerRoot : Building
{
    [Header("# Stats")]
    public ETowerType TowerType;
    [SerializeField] protected TowerData Data;
    protected float _maxHp;
    protected float _hp;
    protected float _damage;
    protected float _atkSpeed;
    protected float _range;

    [Header("# Cost")]
    public Dictionary<ResourceType, int> CostDataDict { get; private set; }

    [Header("# Effect")]
    [SerializeField] private GameObject _buildEffect;

    private bool _isDataInitialized = false;

    protected override void Awake()
    {
        base.Awake();
        ResourceManager.Instance.OnPopulationChange += UpdateStats;

        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
    }


    protected override void OnEnable()
    {
        base.OnEnable();
        TryInitializeData();
        _collider.enabled = false;
        _spriteRenderer.color = _tempColor;
    }

    private void TryInitializeData()
    {
        if (_isDataInitialized) return;

        if (!TowerDataManager.Instance.IsInitialized)
        {
            StartCoroutine(WaitForDataManager());
            return;
        }

        InitializeData();
    }

    private IEnumerator WaitForDataManager()
    {
        while (!TowerDataManager.Instance.IsInitialized)
        {
            yield return null;
        }
        InitializeData();
    }

    private void InitializeData()
    {
        Data = TowerDataManager.Instance.GetTowerData(TowerType);
        if (Data == null) return;

        _maxHp = Data.MaxHp;
        _hp = _maxHp;
        _damage = Data.Damage;
        _atkSpeed = Data.AtkSpeed;
        _range = Data.Range;

        CostDataDict = TowerDataManager.Instance.GetTowerCost(TowerType);
        _isDataInitialized = true;
    }

    private void UpdateStats()
    {
        if (!_isDataInitialized)
        {
            TryInitializeData();
            return;
        }

        _maxHp = TowerDataManager.Instance.GetModifiedStat(TowerType, Data.MaxHp);
        _damage = TowerDataManager.Instance.GetModifiedStat(TowerType, Data.Damage);
        _range = TowerDataManager.Instance.GetModifiedStat(TowerType, Data.Range);
    }

    protected override void OnPlaced()
    {
        _collider.enabled = true;
        _spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100);
        if(_buildEffect != null)
            _buildEffect.SetActive(true);
        if(SoundManager.Instance !=null)
            SoundManager.Instance.PlaySfx(ESfxType.BuildSound);
        StartCoroutine(CoBuildRoutine());
    }

    private IEnumerator CoBuildRoutine()
    {
        float timer = 0f;

        while(timer < Data.BuildTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        ResourceManager.Instance.AddResource(ResourceType.Population, 3);
        UpdateStats();
        _spriteRenderer.color = Color.white;
        IsPlaced = true;
    }

    public void TakeDamage(float damage)
    {
        _hp -= damage;
        if (_hp <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        BoundsInt areaToClean = GetGridArea();
        GridBuildingSystem.Instance.ClearArea(areaToClean);
        ResourceManager.Instance.TryUseResource(ResourceType.Population, 2);
        GameObject explode = EffectPoolManager.Instance.GetObject(EEffectType.BuildingExplode);
        explode.transform.position = transform.position;
        SoundManager.Instance.PlaySfx(ESfxType.BuildingExplode);
        
        TowerPoolManager.Instance.Return(TowerType, gameObject);
    }
}