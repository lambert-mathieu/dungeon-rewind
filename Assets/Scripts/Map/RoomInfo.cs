using System.Collections.Generic;
using UnityEngine;

public enum RoomType
{
    Tutorial,
    Easy,
    Hard,
    MiniBoss,
    Boss,
    Blocked
}

public static class RoomRunData
{
    // Information handed over to your teammate's generator
    public static RoomType SelectedRoomType = RoomType.Tutorial;
    public static int ColumnDepth = 0;
    public static Vector2Int SelectedNodeCoord = new Vector2Int(0, 0);

    // Track progression across the hex tree
    public static Vector2Int CurrentNode = new Vector2Int(0, 0);
    public static HashSet<Vector2Int> VisitedNodes = new HashSet<Vector2Int>();

    public static void SelectRoom(Vector2Int coord, RoomType type)
    {
        VisitedNodes.Add(CurrentNode);
        CurrentNode = coord;

        SelectedNodeCoord = coord;
        SelectedRoomType = type;
        ColumnDepth = coord.x; // Column depth serves as the difficulty scaler
    }

    public static void ResetRun()
    {
        CurrentNode = new Vector2Int(0, 0);
        VisitedNodes.Clear();
        SelectedRoomType = RoomType.Tutorial;
        ColumnDepth = 0;
    }
}