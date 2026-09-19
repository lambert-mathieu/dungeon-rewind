using System.Collections.Generic;
using UnityEngine;

public static class MapRunManager
{
    // Funnel begins at the single root tile: Column 0, Row 0
    public static Vector2Int CurrentNode = new Vector2Int(0, 0);
    public static HashSet<Vector2Int> VisitedNodes = new HashSet<Vector2Int>();

    public static void ResetRun()
    {
        CurrentNode = new Vector2Int(0, 0);
        VisitedNodes.Clear();
    }
}