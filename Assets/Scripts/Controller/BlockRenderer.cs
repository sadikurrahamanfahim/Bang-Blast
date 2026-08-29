using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class BlockRenderer : MonoBehaviour
{
    public GameObject cellPrefab;
    public float sizeMultiplier = 1f;

    private readonly List<GameObject> cellPool = new List<GameObject>();
    private bool renderScheduled = false;
    private BlockDefinition pendingDefinition;
    private float pendingCellSize;
    private int pendingRotationSteps;
    private Color? pendingOverrideColor;

    public void Render(BlockDefinition definition, float cellSize, int rotationSteps = 0, Color? overrideColor = null)
    {
        if (definition == null || cellPrefab == null) return;

        if (IsRebuildingUI())
        {
            ScheduleRender(definition, cellSize, rotationSteps, overrideColor);
            return;
        }

        ApplyRender(definition, cellSize, rotationSteps, overrideColor);
    }

    private void ApplyRender(BlockDefinition definition, float cellSize, int rotationSteps, Color? overrideColor)
    {
        var cells = definition.GetNormalizedCells(rotationSteps);

        int width = definition.GetWidth(rotationSteps);
        int height = definition.GetHeight(rotationSteps);
        var rtSelf = GetComponent<RectTransform>();
        if (rtSelf != null)
        {
            float totalW = width * cellSize * sizeMultiplier;
            float totalH = height * cellSize * sizeMultiplier;
            rtSelf.sizeDelta = new Vector2(totalW, totalH);
        }

        while (cellPool.Count < cells.Count)
        {
            var newCell = Instantiate(cellPrefab, transform);
            newCell.SetActive(false);

            var rt = newCell.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);
            }

            var img = newCell.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;

            cellPool.Add(newCell);
        }

        // Hiển thị và sắp xếp cell
        for (int i = 0; i < cells.Count; i++)
        {
            var go = cellPool[i];
            go.SetActive(true);

            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                float x = (cells[i].x + 0.5f) * cellSize * sizeMultiplier;
                float y = (cells[i].y + 0.5f) * cellSize * sizeMultiplier;
                rt.anchoredPosition = new Vector2(x, y);
                rt.sizeDelta = new Vector2(cellSize * sizeMultiplier, cellSize * sizeMultiplier);
            }

            var img = go.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = definition.sprite;
                img.color = overrideColor.HasValue ? overrideColor.Value : definition.blockColor;
            }
        }

        // Ẩn các cell thừa
        for (int i = cells.Count; i < cellPool.Count; i++)
        {
            cellPool[i].SetActive(false);
        }
    }

    private bool IsRebuildingUI()
    {
#if UNITY_EDITOR || UNITY_UGUI
        try
        {
            return UnityEngine.UI.CanvasUpdateRegistry.IsRebuildingLayout() || UnityEngine.UI.CanvasUpdateRegistry.IsRebuildingGraphics();
        }
        catch
        {
            return false;
        }
#else
        return false;
#endif
    }

    private void ScheduleRender(BlockDefinition definition, float cellSize, int rotationSteps, Color? overrideColor)
    {
        pendingDefinition = definition;
        pendingCellSize = cellSize;
        pendingRotationSteps = rotationSteps;
        pendingOverrideColor = overrideColor;

        if (!renderScheduled)
        {
            renderScheduled = true;
            StartCoroutine(DeferredRender());
        }
    }

    private IEnumerator DeferredRender()
    {
        yield return new WaitForEndOfFrame();
        renderScheduled = false;

        if (pendingDefinition != null)
        {
            ApplyRender(pendingDefinition, pendingCellSize, pendingRotationSteps, pendingOverrideColor);
            pendingDefinition = null;
        }
    }

    public void Clear()
    {
        foreach (var cell in cellPool)
        {
            if (cell != null)
                cell.SetActive(false);
        }
    }
}