using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class HexMapNode : MonoBehaviour
{

    // Production listener: OpenCorridorDoor subscribes to this
    public static event Action<string> OnRoomNodeSelected;

    [SerializeField] private bool testMode = false;
    [SerializeField] private SpriteRenderer tileRenderer;
    [SerializeField] private string combatSceneName = "EnemyRoom";

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
            case RoomType.Teleport: baseColor = new Color(0f, 0f, 0f); break;
        }

        UpdateVisuals(isCleared, isCurrent);
    }

    private void UpdateVisuals(bool isCleared, bool isCurrent)
    {
        if (isCurrent)
        {
            tileRenderer.color = baseColor;
        }
        else if (isCleared)
        {
            // Compute luminance to get true grayscale value
            float gray = (baseColor.r * 0.3f) + (baseColor.g * 0.59f) + (baseColor.b * 0.11f);
            Color desaturated = new Color(gray, gray, gray, 1f);

            // Blend 40% of the original color with 60% gray, then dim brightness to 45%
            Color dimmedColor = Color.Lerp(baseColor, desaturated, 0.6f) * 0.45f;
            dimmedColor.a = 0.85f;

            tileRenderer.color = dimmedColor;
        }
        else if (isSelectable)
        {
            // Active, selectable tile in full vivid color
            tileRenderer.color = baseColor;
        }
        else
        {
            // Future unreached nodes: deeply dimmed and translucent
            tileRenderer.color = new Color(baseColor.r * 0.25f, baseColor.g * 0.25f, baseColor.b * 0.25f, 0.35f);
        }
    }

    void OnMouseDown()
    {

        Debug.Log("I RUN MOUSE DOWN");
        if (!isSelectable) return;

        HexGridManager manager = GetComponentInParent<HexGridManager>();
        Vector2Int finalCoord = gridCoord;

        // Teleport resolution: If tile is Teleport, resolve and warp to opposite partner
        if (roomType == RoomType.Teleport && manager != null)
        {
            finalCoord = manager.GetOppositeTeleportCoord(gridCoord);
            RoomRunData.VisitedNodes.Add(gridCoord);
            Debug.Log($"<color=magenta>[Teleport Triggered]</color> Warped from {gridCoord} to {finalCoord}!");
        }

        // 1. Commit chosen tile to persistent data
        RoomRunData.SelectRoom(finalCoord, roomType);

        string nextRoomName = roomType.ToString();
        Debug.Log($"<color=cyan>[Tile Clicked]</color> Selected: <b>{nextRoomName}</b> at {finalCoord} | Depth: {RoomRunData.ColumnDepth}");

        // 2. Refresh visual node states immediately on the grid
        if (manager != null)
        {
            manager.RefreshNodeStates();
        }

        // 3. Handle Production vs Test execution
        if (testMode)
        {
            // Keep cursor active and free to click further tiles
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Production: hide cursor, notify OpenCorridorDoor, and unload map
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            OnRoomNodeSelected?.Invoke(nextRoomName);
        }
    }
}