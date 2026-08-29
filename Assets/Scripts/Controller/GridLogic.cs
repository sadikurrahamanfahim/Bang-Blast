using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public struct BlockInfo
{
    public BlockDefinition def;
    public int rotationSteps;

    public BlockInfo(BlockDefinition def, int rotationSteps)
    {
        this.def = def;
        this.rotationSteps = rotationSteps;
    }
}

public class GridLogic : MonoBehaviour
{
    public System.Action<int> OnScoreChanged;
    public System.Action<int, int> OnLinesCleared;
    public System.Action OnGameOver;

    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject blockPrefab; // The prefab to instantiate when placing blocks
    [SerializeField] private int pointsPerLine = 50;
    [SerializeField] private Color flashColor = new Color(1f, 1f, 0.2f, 0.9f);
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private float blockScale = 1.0f; // Scale of the block relative to cell size (1.0 = full cell)
    [SerializeField] private ParticleSystem particles;
    
    [SerializeField] private AudioClip blastSfx;
    [SerializeField] private AudioClip gameOverSfx;
    
    private bool[,] occupied;
    private GameObject[,] placedBlocks; // Track placed block GameObjects
    private int score = 0;
    private int comboCount = 0;

    void Awake()
    {
        InitGrid();
    }

    private void InitGrid()
    {
        if (gridManager == null) return;
        occupied = new bool[gridManager.width, gridManager.height];
        placedBlocks = new GameObject[gridManager.width, gridManager.height];
    }

    public void ResetGrid()
    {
        InitGrid();
        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                // Destroy any placed blocks
                if (placedBlocks[x, y] != null)
                {
                    Destroy(placedBlocks[x, y]);
                    placedBlocks[x, y] = null;
                }
                
                occupied[x, y] = false;
            }
        }
        score = 0;
        comboCount = 0;
        OnScoreChanged?.Invoke(score);
    }

    public void OccupyCell(int x, int y, Color color)
    {
        if (!InBounds(x, y)) return;
        
        occupied[x, y] = true;
        
        // Get the cell GameObject from GridManager
        GameObject cellObj = gridManager.GetCellObject(x, y);
        if (cellObj == null)
        {
            Debug.LogWarning($"[GridLogic] Cell at ({x},{y}) is null!");
            return;
        }
        
        if (blockPrefab == null)
        {
            Debug.LogWarning("[GridLogic] blockPrefab is not assigned!");
            return;
        }
        
        // Destroy old block if exists
        if (placedBlocks[x, y] != null)
        {
            Destroy(placedBlocks[x, y]);
        }
        
        // Instantiate the block prefab as a child of the cell
        GameObject newBlock = Instantiate(blockPrefab, cellObj.transform);
        newBlock.name = $"Block_{x}_{y}";
        
        // Set up the block to fill the entire cell
        RectTransform blockRect = newBlock.GetComponent<RectTransform>();
        if (blockRect != null)
        {
            // Stretch to fill parent cell completely
            blockRect.anchorMin = Vector2.zero;
            blockRect.anchorMax = Vector2.one;
            blockRect.pivot = new Vector2(0.5f, 0.5f);
            blockRect.anchoredPosition = Vector2.zero;
            blockRect.offsetMin = Vector2.zero;
            blockRect.offsetMax = Vector2.zero;
            
            // Use blockScale only if you want to add padding (1.0 = full size)
            blockRect.localScale = Vector3.one;
            blockRect.localRotation = Quaternion.identity;
        }
        
        // Apply color to the block
        Image blockImage = newBlock.GetComponent<Image>();
        if (blockImage != null)
        {
            blockImage.color = color;
            blockImage.raycastTarget = false; // Don't block raycasts
        }
        
        // Make sure it renders on top
        newBlock.transform.SetAsLastSibling();
        
        placedBlocks[x, y] = newBlock;
        
        Debug.Log($"[GridLogic] Placed block at ({x},{y}) with color {color}");
    }

    public void ClearCell(int x, int y)
    {
        if (!InBounds(x, y)) return;
        
        occupied[x, y] = false;
        
        // Destroy the placed block
        if (placedBlocks[x, y] != null)
        {
            Destroy(placedBlocks[x, y]);
            placedBlocks[x, y] = null;
        }
    }

    public bool IsOccupied(int x, int y)
    {
        if (!InBounds(x, y)) return true;
        return occupied[x, y];
    }

    public void CheckAndClearLines()
    {
        List<int> fullRows = GetFullRows();
        List<int> fullCols = GetFullCols();

        int cleared = 0;

        foreach (int y in fullRows)
        {
            StartCoroutine(FlashRow(y));
            Instantiate(particles);
            particles.Play();
            cleared++;
        }

        foreach (int x in fullCols)
        {
            StartCoroutine(FlashCol(x));
            Instantiate(particles);
            particles.Play();
            cleared++;
        }

        if (cleared > 0)
        {
            comboCount++;
            int points = cleared * pointsPerLine * comboCount;
            AddScore(points);

            Debug.Log($"[GridLogic] Cleared {cleared} lines/cols, combo x{comboCount}, + {points} points");
            OnLinesCleared?.Invoke(cleared, comboCount);
        }
        else
        {
            comboCount = 0;
        }
    }

    private IEnumerator FlashRow(int y)
    {
        if (gridManager == null) yield break;

        // Flash effect - change color of all blocks in the row
        for (int x = 0; x < gridManager.width; x++)
        {
            if (placedBlocks[x, y] != null)
            {
                Image img = placedBlocks[x, y].GetComponent<Image>();
                if (img != null)
                {
                    img.color = flashColor;
                }
            }
        }

        yield return new WaitForSeconds(flashDuration);

        // Clear all cells in the row
        for (int x = 0; x < gridManager.width; x++)
            ClearCell(x, y);
    }

    private IEnumerator FlashCol(int x)
    {
        if (gridManager == null) yield break;

        // Flash effect - change color of all blocks in the column
        for (int y = 0; y < gridManager.height; y++)
        {
            if (placedBlocks[x, y] != null)
            {
                Image img = placedBlocks[x, y].GetComponent<Image>();
                if (img != null)
                {
                    img.color = flashColor;
                }
            }
        }

        yield return new WaitForSeconds(flashDuration);

        // Clear all cells in the column
        for (int y = 0; y < gridManager.height; y++)
            ClearCell(x, y);
    }

    private void AddScore(int amount)
    {
        if (amount <= 0) return;
        score += amount;
        MusicManager.PlaySFX(blastSfx);
        OnScoreChanged?.Invoke(score);
    }

    public int GetScore()
    {
        return score;
    }

    private List<int> GetFullRows()
    {
        var fullRows = new List<int>();
        for (int y = 0; y < gridManager.height; y++)
        {
            bool full = true;
            for (int x = 0; x < gridManager.width; x++)
            {
                if (!occupied[x, y]) { full = false; break; }
            }
            if (full) fullRows.Add(y);
        }
        return fullRows;
    }

    private List<int> GetFullCols()
    {
        var fullCols = new List<int>();
        for (int x = 0; x < gridManager.width; x++)
        {
            bool full = true;
            for (int y = 0; y < gridManager.height; y++)
            {
                if (!occupied[x, y]) { full = false; break; }
            }
            if (full) fullCols.Add(x);
        }
        return fullCols;
    }

    private bool InBounds(int x, int y)
    {
        return x >= 0 && x < gridManager.width && y >= 0 && y < gridManager.height;
    }

    public void CheckGameOver(List<BlockInfo> trayBlocks)
    {
        if (!CanPlaceAnyBlock(trayBlocks))
        {
            Debug.Log("[GridLogic] No block can be placed → GAME OVER");
            MusicManager.PlaySFX(gameOverSfx);
            OnGameOver?.Invoke();
        }
    }

    private bool CanPlaceAnyBlock(List<BlockInfo> trayBlocks)
    {
        foreach (var info in trayBlocks)
        {
            if (info.def != null && CanPlaceBlock(info.def, info.rotationSteps))
                return true;
        }
        return false;
    }

    private bool CanPlaceBlock(BlockDefinition def, int rotationSteps)
    {
        int maxX = gridManager.width - def.GetWidth(rotationSteps);
        int maxY = gridManager.height - def.GetHeight(rotationSteps);

        for (int x = 0; x <= maxX; x++)
        {
            for (int y = 0; y <= maxY; y++)
            {
                if (CanPlaceAt(def, x, y, rotationSteps))
                    return true;
            }
        }
        return false;
    }

    private bool CanPlaceAt(BlockDefinition def, int startX, int startY, int rotationSteps)
    {
        foreach (Vector2Int cell in def.GetNormalizedCells(rotationSteps))
        {
            int gridX = startX + cell.x;
            int gridY = startY + cell.y;

            if (!InBounds(gridX, gridY)) return false;
            if (occupied[gridX, gridY]) return false;
        }
        return true;
    }
}