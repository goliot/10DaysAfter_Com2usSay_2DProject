using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class GridBuildingSystem : Singleton<GridBuildingSystem>
{
    [Header("# Grid Components")]
    public GridLayout GridLayout;
    public Tilemap MainTilemap;
    public Tilemap Temptilemap;

    [Header("# Tile Settings")]
    [SerializeField] private string _tilePath = @"Tiles\";
    private static Dictionary<TileType, TileBase> _tileBases;

    private Building _tempBuilding;
    private TowerRoot _tempTower;
    private Vector3Int _prevCellPos;
    private BoundsInt _prevArea;

    public Action OnBuildFailed;

    private void Awake()
    {
        InitializeTileBases();
    }

    private void InitializeTileBases()
    {
        _tileBases = new Dictionary<TileType, TileBase>
        {
            { TileType.Empty, null },
            { TileType.White, Resources.Load<TileBase>("Tiles/RandGroundPixel") },
            { TileType.Green, Resources.Load<TileBase>("Tiles/green") },
            { TileType.Red, Resources.Load<TileBase>("Tiles/red") }
        };
    }

    private void Update()
    {
        if (!_tempBuilding || _tempBuilding.IsPlaced) return;
        
        if (EventSystem.current.IsPointerOverGameObject(0)) return;

        Vector2 touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPos = GridLayout.WorldToCell(new Vector3(touchPos.x, touchPos.y, 0));

        if (_prevCellPos != cellPos)
        {
            _tempBuilding.SetGridPosition(cellPos);
            _prevCellPos = cellPos;
            UpdatePreviewTiles();
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceBuilding();
        }
        else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CancelBuilding();
        }
    }

    public void InitializeWithBuilding(TowerRoot building)
    {
        CancelBuilding();
        _tempTower = TowerPoolManager.Instance.GetObject(building.TowerType).GetComponent<TowerRoot>();
        _tempBuilding = _tempTower.GetComponent<Building>();
        
        // 초기 위치 설정 및 미리보기 표시
        Vector2 touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPos = GridLayout.WorldToCell(new Vector3(touchPos.x, touchPos.y, 0));
        _tempBuilding.SetGridPosition(cellPos);
        _prevCellPos = cellPos;
        UpdatePreviewTiles();
    }

    private void UpdatePreviewTiles()
    {
        if (_tempBuilding == null) return;

        ClearPreviewTiles();
        BoundsInt buildingArea = _tempBuilding.GetGridArea();

        TileBase[] baseArray = GetTilesBlock(buildingArea, MainTilemap);
        TileBase[] tileArray = new TileBase[baseArray.Length];

        bool canPlace = true;
        for (int i = 0; i < baseArray.Length; i++)
        {
            if (baseArray[i] != _tileBases[TileType.White])
            {
                canPlace = false;
                break;
            }
        }

        TileType tileType = (canPlace && !_tempBuilding.HasEnemyOverlap) ? TileType.Green : TileType.Red;
        FillTiles(tileArray, tileType);
        Temptilemap.SetTilesBlock(buildingArea, tileArray);
        _prevArea = buildingArea;
    }

    private void ClearPreviewTiles()
    {
        if (_prevArea.size == Vector3Int.zero) return;
        
        int size = _prevArea.size.x * _prevArea.size.y * _prevArea.size.z;
        TileBase[] toClear = new TileBase[size];
        FillTiles(toClear, TileType.Empty);
        Temptilemap.SetTilesBlock(_prevArea, toClear);
    }

    private static TileBase[] GetTilesBlock(BoundsInt area, Tilemap tilemap)
    {
        TileBase[] array = new TileBase[area.size.x * area.size.y * area.size.z];
        int counter = 0;

        foreach (var v in area.allPositionsWithin)
        {
            Vector3Int pos = new Vector3Int(v.x, v.y, 0);
            array[counter] = tilemap.GetTile(pos);
            counter++;
        }

        return array;
    }

    private static void FillTiles(TileBase[] arr, TileType type)
    {
        if (!_tileBases.ContainsKey(type))
        {
            Debug.LogError($"TileType {type} not found in tileBases dictionary!");
            return;
        }

        TileBase tile = _tileBases[type];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = tile;
        }
    }

    public bool CanTakeArea(BoundsInt area)
    {
        TileBase[] baseArray = GetTilesBlock(area, MainTilemap);
        foreach (var b in baseArray)
        {
            if (b != _tileBases[TileType.White])
            {
                return false;
            }
        }
        return true;
    }

    public void TakeArea(BoundsInt area)
    {
        SetTilesBlock(area, TileType.Empty, Temptilemap);
        SetTilesBlock(area, TileType.Green, MainTilemap);
    }

    public void ClearArea(BoundsInt area)
    {
        SetTilesBlock(area, TileType.Empty, Temptilemap);
        SetTilesBlock(area, TileType.Empty, MainTilemap);
        SetTilesBlock(area, TileType.White, MainTilemap);
    }

    private static void SetTilesBlock(BoundsInt area, TileType type, Tilemap tilemap)
    {
        int size = area.size.x * area.size.y * area.size.z;
        TileBase[] tileArray = new TileBase[size];
        FillTiles(tileArray, type);
        tilemap.SetTilesBlock(area, tileArray);
    }

    private void TryPlaceBuilding()
    {
        if (_tempBuilding.CanBePlaced() && ResourceManager.Instance.TryUseMultipleResources(_tempTower.CostDataDict))
        {
            _tempBuilding.Place();
            ClearPreviewTiles();  // 건물 배치 후 미리보기 제거
            _tempBuilding = null;
            _tempTower = null;
        }
        else
        {
            OnBuildFailed?.Invoke();
        }
    }

    private void CancelBuilding()
    {
        ClearPreviewTiles();
        if (_tempTower != null)
        {
            TowerPoolManager.Instance.Return(_tempTower.TowerType, _tempTower.gameObject);
            _tempTower = null;
            _tempBuilding = null;
        }
    }
}

public enum TileType
{
    Empty,
    White,
    Green,
    Red
}