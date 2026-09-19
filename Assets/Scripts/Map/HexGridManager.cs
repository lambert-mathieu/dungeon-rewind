using System.Collections.Generic;
using UnityEngine;

public class HexGridManager : MonoBehaviour
{
    [Header("Prefab & Sprites")]
    [SerializeField] private GameObject hexPrefab;
    [SerializeField] private Sprite[] hexBiomeSprites;
    [SerializeField] private Sprite bossHexSprite;

    [Header("Data Channel")]

    [Header("Grid Dimensions")]
    [SerializeField] private int regularColumns = 8;
    [SerializeField] private int maxRows = 5;

    [Header("Generation Constraints")]
    [SerializeField] private int maxBlockedTiles = 5;

    [Header("Spacing")]
    [SerializeField] private float horizontalSpacing = 1.0f;
    [SerializeField] private float verticalSpacing = 1.0f;

    // Grid tracking
    private Dictionary<Vector2Int, RoomType> generatedTypes = new Dictionary<Vector2Int, RoomType>();
    private Dictionary<Vector2Int, HexMapNode> gridNodes = new Dictionary<Vector2Int, HexMapNode>();
    private List<Vector2Int> validCoords = new List<Vector2Int>();

    void Start()
    {
        GenerateGrid();
    }

    [ContextMenu("Generate Grid In Editor")]
    public void GenerateGrid()
    {
        ClearGrid();
        gridNodes.Clear();
        generatedTypes.Clear();
        validCoords.Clear();

        if (hexPrefab == null)
        {
            Debug.LogError("Hex Prefab missing!");
            return;
        }

        // 1. Gather all coordinate points in the funnel layout
        BuildCoordinateList();

        // 2. Apply graph rules and assign RoomTypes
        GenerateGraphRules();

        // 3. Instantiate visual tiles
        InstantiateVisualGrid();
    }

    private void BuildCoordinateList()
    {
        for (int col = 0; col < regularColumns; col++)
        {
            int rowCount = (col < maxRows) ? (col + 1) : ((col % 2 == (maxRows - 1) % 2) ? maxRows : maxRows - 1);
            for (int row = 0; row < rowCount; row++)
            {
                validCoords.Add(new Vector2Int(col, row));
            }
        }
        // Boss node
        validCoords.Add(new Vector2Int(regularColumns, 0));
    }

    private void GenerateGraphRules()
    {
        // Rule 1: Column 0 is Tutorial
        generatedTypes[new Vector2Int(0, 0)] = RoomType.Tutorial;

        // Rule 2: Column 1 is strictly two Easy rooms
        generatedTypes[new Vector2Int(1, 0)] = RoomType.Easy;
        generatedTypes[new Vector2Int(1, 1)] = RoomType.Easy;

        // Boss Node
        generatedTypes[new Vector2Int(regularColumns, 0)] = RoomType.Boss;

        // Filter validCoords directly for candidate spots (columns 2 to regularColumns - 1)
        List<Vector2Int> candidates = validCoords.FindAll(c => c.x >= 2 && c.x < regularColumns);

        // Pick 4 distinct random coordinates for the MiniBosses
        int placedMiniBosses = 0;
        while (candidates.Count > 0 && placedMiniBosses < 4)
        {
            int randomIndex = Random.Range(0, candidates.Count);
            generatedTypes[candidates[randomIndex]] = RoomType.MiniBoss;
            candidates.RemoveAt(randomIndex);
            placedMiniBosses++;
        }

        // Fill remaining coordinates with Easy / Hard
        foreach (var coord in validCoords)
        {
            if (!generatedTypes.ContainsKey(coord))
            {
                generatedTypes[coord] = (Random.value < 0.45f) ? RoomType.Easy : RoomType.Hard;
            }
        }
    }

    private void InstantiateVisualGrid()
    {
        SpriteRenderer sr = hexPrefab.GetComponent<SpriteRenderer>();
        float spriteWidth = sr.sprite.bounds.size.x;
        float spriteHeight = sr.sprite.bounds.size.y;

        float xStep = (spriteWidth * 0.75f) * horizontalSpacing;
        float yStep = spriteHeight * verticalSpacing;

        int totalColumns = regularColumns + 1;
        float totalWidth = (totalColumns - 1) * xStep;
        float xStartOffset = -totalWidth * 0.5f;

        Vector2Int playerCoord = RoomRunData.CurrentNode;

        foreach (var coord in validCoords)
        {
            RoomType type = generatedTypes[coord];

            // If tile is blocked, omit it from rendering and navigation
            if (type == RoomType.Blocked)
            {
                continue;
            }

            int col = coord.x;
            int row = coord.y;

            float posX = xStartOffset + (col * xStep);
            float posY;

            if (col == regularColumns) // Boss
            {
                posY = 0f;
            }
            else
            {
                int rowCount = GetRowCount(col);
                float columnHeight = (rowCount - 1) * yStep;
                float yStartOffset = -columnHeight * 0.5f;
                posY = yStartOffset + (row * yStep);
            }

            Vector3 spawnPos = new Vector3(posX, posY, -(col * 0.01f + row * 0.001f));
            GameObject tileObj = Instantiate(hexPrefab, spawnPos, Quaternion.identity, transform);
            tileObj.name = $"Hex_C{col}_R{row}_{type}";

            // Sprite assignment
            SpriteRenderer tileSr = tileObj.GetComponent<SpriteRenderer>();
            if (tileSr != null)
            {
                if (type == RoomType.Boss && bossHexSprite != null)
                {
                    tileSr.sprite = bossHexSprite;
                }
                else if (hexBiomeSprites != null && hexBiomeSprites.Length > 0)
                {
                    tileSr.sprite = hexBiomeSprites[Random.Range(0, hexBiomeSprites.Length)];
                }
            }

            HexMapNode node = tileObj.GetComponent<HexMapNode>();
            if (node == null) node = tileObj.AddComponent<HexMapNode>();

            bool isCurrent = (coord == playerCoord);
            bool isCleared = false;
            bool isSelectable = IsFunnelNeighbor(playerCoord, coord, maxRows, regularColumns);

            node.Setup(coord, type, isSelectable, isCleared, isCurrent);
            gridNodes.Add(coord, node);
        }
    }

    private int GetRowCount(int col)
    {
        if (col >= regularColumns) return 1;
        if (col < maxRows) return col + 1;
        bool matchesParity = (col % 2 == (maxRows - 1) % 2);
        return matchesParity ? maxRows : maxRows - 1;
    }

    public bool IsFunnelNeighbor(Vector2Int from, Vector2Int to, int maxRowCap, int totalRegCols)
    {
        // Blocked tiles cannot be target destinations
        if (generatedTypes.TryGetValue(to, out RoomType targetType) && targetType == RoomType.Blocked)
        {
            return false;
        }

        // Strict forward progression only (up-right or down-right)
        if (to.x != from.x + 1) return false;

        // Transition to Boss
        if (to.x == totalRegCols) return to.y == 0;

        // Funnel expansion phase
        if (from.x < maxRowCap - 1)
        {
            return (to.y == from.y || to.y == from.y + 1);
        }

        // Alternating full columns
        bool fromIsLonger = (from.x % 2 == (maxRowCap - 1) % 2);
        if (fromIsLonger)
        {
            return (to.y == from.y - 1 || to.y == from.y);
        }
        else
        {
            return (to.y == from.y || to.y == from.y + 1);
        }
    }

    public void ClearGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}