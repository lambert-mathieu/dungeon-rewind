using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class HexMapNode : MonoBehaviour
{
    [SerializeField] private SpriteRenderer tileRenderer;
    [SerializeField] private string combatSceneName = "CombatLevelScene";

    public RoomType roomType;
    public Vector2Int gridCoord;
    public bool isSelectable;

    private Color baseColor = Color.white;

    public void Setup(Vector2Int coord, RoomType type, bool selectable, bool isCleared, bool isCurrent)
    {
        gridCoord = coord;
        roomType = type;
        isSelectable = selectable;

        if (tileRenderer == null)
            tileRenderer = GetComponent<SpriteRenderer>();

        // Distinct color tint for each room category
        switch (roomType)
        {
            case RoomType.Tutorial:     baseColor = new Color(0.3f, 0.75f, 0.95f); break; // Blue
            case RoomType.Easy:     baseColor = new Color(0.35f, 0.8f, 0.35f); break; // Green
            case RoomType.Hard:     baseColor = new Color(0.95f, 0.5f, 0.15f); break; // Orange
            case RoomType.MiniBoss: baseColor = new Color(0.65f, 0.25f, 0.85f); break; // Purple
            case RoomType.Boss:     baseColor = new Color(0.9f, 0.15f, 0.15f); break; // Red
        }

        UpdateVisuals(isCleared, isCurrent);
    }

    private void UpdateVisuals(bool isCleared, bool isCurrent)
    {
        if (isCurrent)
            tileRenderer.color = Color.cyan;
        else if (isCleared)
            tileRenderer.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);
        else if (isSelectable)
            tileRenderer.color = baseColor;
        else
            tileRenderer.color = new Color(baseColor.r * 0.4f, baseColor.g * 0.4f, baseColor.b * 0.4f, 0.35f);
    }

    void OnMouseDown()
    {
        if (!isSelectable) return;

        // 1. Commit room payload to the shared static data
        RoomRunData.SelectRoom(gridCoord, roomType);

        Debug.Log($"[Room Dispatched] -> Type: {RoomRunData.SelectedRoomType} | Column Depth: {RoomRunData.ColumnDepth} | Coord: {RoomRunData.SelectedNodeCoord}");

        // 2. Lock cursor back into gameplay mode and transition
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SceneManager.LoadScene(combatSceneName);
    }
}