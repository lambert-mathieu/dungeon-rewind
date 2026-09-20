using System.Collections.Generic;
using UnityEngine;

public enum RoomType
{
    Tutorial,
    Easy,
    Hard,
    MiniBoss,
    Boss,
    Blocked, 
    Teleport
}

public static class RoomRunData
{
    // Null indicates no choice has been made yet
    public static RoomType? SelectedRoomType = null;
    public static int ColumnDepth = 0;
    public static Vector2Int SelectedNodeCoord = new Vector2Int(0, 0);

    public static Vector2Int CurrentNode = new Vector2Int(0, 0);
    public static HashSet<Vector2Int> VisitedNodes = new HashSet<Vector2Int>();

    public static void SelectRoom(Vector2Int coord, RoomType type)
    {
        VisitedNodes.Add(CurrentNode);
        CurrentNode = coord;

        SelectedNodeCoord = coord;
        SelectedRoomType = type;
        ColumnDepth = coord.x;
    }

    public static void ResetRun()
    {
        CurrentNode = new Vector2Int(0, 0);
        VisitedNodes.Clear();
        SelectedRoomType = null;
        ColumnDepth = 0;
    }
}