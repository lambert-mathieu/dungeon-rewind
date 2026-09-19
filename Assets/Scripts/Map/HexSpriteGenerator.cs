using UnityEngine;
using UnityEditor;
using System.IO;

public class HexSpriteGenerator
{
    [MenuItem("Tools/Generate Hexagon Sprite")]
    public static void CreateHexagonTexture()
    {
        int size = 512;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Color clear = new Color(0, 0, 0, 0);
        Color fill = Color.white;
        Color border = new Color(0.12f, 0.12f, 0.12f, 1f);

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        
        // For flat-topped hex:
        // Horizontal tip-to-tip span is 2 * radius.
        // Keeping radius at 230 ensures it stays well inside the 256px half-width with 26px margin.
        float radius = 225f;
        float borderWidth = 14f;

        // Inradius (distance from center to flat top and bottom)
        float inradius = radius * 0.8660254f; // radius * sqrt(3)/2

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - center;
                
                float px = Mathf.Abs(p.x);
                float py = Mathf.Abs(p.y);

                // Exact flat-topped regular hexagon boundary:
                // 1. py <= inradius (flat top and bottom)
                // 2. px * sqrt(3)/2 + py * 0.5 <= inradius (the four slanted edges)
                // 3. px <= radius (left and right tips)
                float d1 = py;
                float d2 = px * 0.8660254f + py * 0.5f;
                float dist = Mathf.Max(d1, d2);

                if (dist > inradius)
                {
                    tex.SetPixel(x, y, clear);
                }
                else if (dist >= inradius - (borderWidth * 0.8660254f))
                {
                    tex.SetPixel(x, y, border);
                }
                else
                {
                    tex.SetPixel(x, y, fill);
                }
            }
        }

        tex.Apply();

        byte[] pngData = tex.EncodeToPNG();
        string path = "Assets/Hexagon_Tile.png";
        File.WriteAllBytes(path, pngData);
        AssetDatabase.Refresh();

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 256;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        Debug.Log("Flat-topped hexagon generated successfully with unclipped side tips at: " + path);
    }
}