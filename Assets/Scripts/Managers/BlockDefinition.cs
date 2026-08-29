using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewBlock", menuName = "Block/Definition")]
public class BlockDefinition : ScriptableObject
{
    public Sprite sprite;
    public Color blockColor = Color.white;
    public List<Vector2Int> shapeCells = new List<Vector2Int>();

    public Vector2Int GetSize(int rotationSteps = 0)
    {
        var cells = GetNormalizedCells(rotationSteps);
        if (cells.Count == 0) return Vector2Int.zero;

        int maxX = int.MinValue, maxY = int.MinValue;
        foreach (var c in cells)
        {
            if (c.x > maxX) maxX = c.x;
            if (c.y > maxY) maxY = c.y;
        }
        return new Vector2Int(maxX + 1, maxY + 1);
    }

    public int GetWidth(int rotationSteps = 0) => GetSize(rotationSteps).x;
    public int GetHeight(int rotationSteps = 0) => GetSize(rotationSteps).y;

    // List cell đã chuẩn hóa (gốc về 0,0)
    public List<Vector2Int> GetNormalizedCells(int rotationSteps = 0)
    {
        var rotated = GetRotatedCells(rotationSteps);
        if (rotated.Count == 0)
        {
            Debug.LogWarning($"[BlockDefinition] {name} rot={rotationSteps} → rotated list EMPTY");
            return new List<Vector2Int>();
        }

        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var c in rotated)
        {
            if (c.x < minX) minX = c.x;
            if (c.y < minY) minY = c.y;
        }

        var normalized = new List<Vector2Int>(rotated.Count);
        foreach (var c in rotated)
            normalized.Add(new Vector2Int(c.x - minX, c.y - minY));

        string rawCells = string.Join(",", rotated);
        string normCells = string.Join(",", normalized);
        Debug.Log($"[BlockDefinition] {name} rot={rotationSteps} raw=[{rawCells}] min=({minX},{minY}) norm=[{normCells}]");
        
        return normalized;
    }

    // Xoay cell theo rotationSteps (0,1,2,3) = (0°,90°,180°,270°)
    public List<Vector2Int> GetRotatedCells(int rotationSteps)
    {
        var rotated = new List<Vector2Int>();
        if (shapeCells == null || shapeCells.Count == 0) return rotated;

        rotationSteps = ((rotationSteps % 4) + 4) % 4;

        foreach (var cell in shapeCells)
        {
            Vector2Int newCell;
            switch (rotationSteps)
            {
                case 1: newCell = new Vector2Int(-cell.y, cell.x); break;
                case 2: newCell = new Vector2Int(-cell.x, -cell.y); break;
                case 3: newCell = new Vector2Int(cell.y, -cell.x); break;
                default: newCell = cell; break;
            }
            rotated.Add(newCell);
        }
        return rotated;
    }

    // Kiểm tra shape có hợp lệ không (không trùng lặp)
    public bool ValidateShape()
    {
        if (shapeCells == null || shapeCells.Count == 0) return false;

        HashSet<Vector2Int> unique = new HashSet<Vector2Int>(shapeCells);
        if (unique.Count != shapeCells.Count)
        {
            Debug.LogWarning($"[BlockDefinition] {name} có cell trùng lặp.");
            return false;
        }
        return true;
    }

    public Vector2Int GetBounds(int rotationSteps)
    {
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var c in GetNormalizedCells(rotationSteps))
        {
            if (c.x < minX) minX = c.x;
            if (c.x > maxX) maxX = c.x;
            if (c.y < minY) minY = c.y;
            if (c.y > maxY) maxY = c.y;
        }

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        return new Vector2Int(width, height);
    }
}
