using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class BlockDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public BlockDefinition definition;
    public GridManager gridManager;
    public GridLogic gridLogic;
    public BlockRenderer rendererComp;
    public System.Action onPlaced;
    public System.Action onPlacementFailed; // ← NEW
    [HideInInspector] public int rotationSteps;
    
    [Header("Drag Offset")]
    [SerializeField] private Vector2 dragScreenOffset = new Vector2(0f, 120f);

    [Header("Grid Shadow")]
    [SerializeField] private float shadowAlpha = 0.4f;

    private Vector3 startWorldPos;
    private Vector3 startLocalPos;
    private Transform startParent;
    private RectTransform rectTransform;

    private GameObject shadowObj;
    private RectTransform shadowRect;
    private BlockRenderer shadowRenderer;
    private bool shadowShown;
    private Vector2Int shadowBase;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rendererComp == null) rendererComp = GetComponent<BlockRenderer>();
    }

    private void OnEnable()
    {
        if (gridManager != null)
            gridManager.OnGridResized += RenderBlock;

        StartCoroutine(RenderNextFrame());
    }

    private void OnDisable()
    {
        if (gridManager != null)
            gridManager.OnGridResized -= RenderBlock;

        HideShadow();
    }

    private IEnumerator RenderNextFrame()
    {
        yield return null;
        RenderBlock();
    }

    private void OnRectTransformDimensionsChange()
    {
        RenderBlock();
    }

    private void RenderBlock()
    {
        if (gridManager == null || definition == null || rendererComp == null) return;
        float cellSize = gridManager.GetCellSize();
        rendererComp.Render(definition, cellSize, rotationSteps);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (gridManager == null) return;

        startWorldPos = rectTransform.position;
        startLocalPos = rectTransform.localPosition;
        startParent = transform.parent;

        Canvas rootCanvas = gridManager.GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            transform.SetParent(rootCanvas.transform, true);
        }

        RenderBlock();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (gridManager == null) return;

        var canvas = gridManager.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var canvasRect = canvas.transform as RectTransform;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        // Apply offset in screen space
        Vector2 offsetPos = eventData.position + dragScreenOffset;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                offsetPos,
                cam,
                out var localPoint))
        {
            rectTransform.localPosition = localPoint;
            UpdateShadow();
        }
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        HideShadow();

        if (gridManager == null) return;

        var canvas = gridManager.GetComponentInParent<Canvas>();

        if (canvas != null)
        {
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            // Use BLOCK visual position, not finger position
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, rectTransform.position);

            if (TryGetBestBase(gridManager.GetBoardRect(), cam, screenPos, out var bestBase) &&
                CanPlaceBlock(bestBase.x, bestBase.y))
            {
                PlaceBlock(bestBase.x, bestBase.y);
                return;
            }
        }

        // Placement failed → reset
        transform.SetParent(startParent, true);
        rectTransform.position = startWorldPos;
        rectTransform.localPosition = startLocalPos;
        RenderBlock();

        onPlacementFailed?.Invoke();
    }

    private void UpdateShadow()
    {
        if (definition == null || gridManager == null || gridLogic == null) return;

        var canvas = gridManager.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, rectTransform.position);

        if (!TryGetBestBase(gridManager.GetBoardRect(), cam, screenPos, out var bestBase) ||
            !CanPlaceBlock(bestBase.x, bestBase.y))
        {
            HideShadow();
            return;
        }

        ShowShadowAt(bestBase);
    }

    private bool TryGetBestBase(RectTransform board, Camera cam, Vector2 screenPos, out Vector2Int bestBase)
    {
        bestBase = default;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(board, screenPos, cam, out var localPoint))
            return false;

        // Convert to board coordinate space
        localPoint.x += board.rect.width * 0.5f;
        localPoint.y += board.rect.height * 0.5f;

        float cellSize = gridManager.GetCellSize();
        Vector2 offset = gridManager.GetGridOffset();
        Vector2Int blockSize = definition.GetBounds(rotationSteps);
        Vector2 halfBlockSize = new Vector2(
            blockSize.x * cellSize * 0.5f,
            blockSize.y * cellSize * 0.5f
        );

        float bestDist = float.MaxValue;
        bool found = false;

        foreach (var cell in definition.GetNormalizedCells(rotationSteps))
        {
            Vector2 cellPos = localPoint - halfBlockSize + (Vector2)cell * cellSize;

            int gx = Mathf.RoundToInt((cellPos.x - offset.x) / cellSize);
            int gy = Mathf.RoundToInt((cellPos.y - offset.y) / cellSize);

            Vector2 snapped = new Vector2(
                offset.x + gx * cellSize,
                offset.y + gy * cellSize
            );

            float dist = (snapped - cellPos).sqrMagnitude;

            if (dist < bestDist)
            {
                bestDist = dist;
                bestBase = new Vector2Int(gx - cell.x, gy - cell.y);
                found = true;
            }
        }
        return found;
    }

    private void ShowShadowAt(Vector2Int basePos)
    {
        if (shadowShown && shadowBase == basePos) return;

        EnsureShadow();
        if (shadowObj == null) return;

        float cellSize = gridManager.GetCellSize();
        Vector2 offset = gridManager.GetGridOffset();
        Vector2Int size = definition.GetBounds(rotationSteps);

        shadowRect.anchorMin = shadowRect.anchorMax = new Vector2(0f, 0f);
        shadowRect.pivot = new Vector2(0f, 0f);
        shadowRect.sizeDelta = new Vector2(size.x * cellSize, size.y * cellSize);
        shadowRect.anchoredPosition = offset + new Vector2(basePos.x * cellSize, basePos.y * cellSize);
        shadowObj.transform.SetAsLastSibling();

        Color c = definition.blockColor;
        c.a = shadowAlpha;
        shadowRenderer.Render(definition, cellSize, rotationSteps, c);

        shadowObj.SetActive(true);
        shadowShown = true;
        shadowBase = basePos;
    }

    private void EnsureShadow()
    {
        if (shadowObj != null) return;

        shadowObj = new GameObject("DragShadow", typeof(RectTransform));
        shadowRect = shadowObj.GetComponent<RectTransform>();
        shadowRect.SetParent(gridManager.transform, false);
        shadowObj.SetActive(false);

        shadowRenderer = shadowObj.AddComponent<BlockRenderer>();
        if (rendererComp != null)
            shadowRenderer.cellPrefab = rendererComp.cellPrefab;
    }

    private void HideShadow()
    {
        shadowShown = false;

        if (shadowObj != null && shadowObj.activeSelf)
        {
            if (shadowRenderer != null) shadowRenderer.Clear();
            shadowObj.SetActive(false);
        }
    }

    private bool CanPlaceBlock(int baseX, int baseY)
    {
        if (definition == null || gridManager == null) return false;

        foreach (var cell in definition.GetNormalizedCells(rotationSteps))
        {
            int x = baseX + cell.x;
            int y = baseY + cell.y;

            if (x < 0 || x >= gridManager.width || y < 0 || y >= gridManager.height)
                return false;
            if (gridLogic != null && gridLogic.IsOccupied(x, y))
                return false;
        }
        return true;
    }

    private void PlaceBlock(int baseX, int baseY)
    {
        Debug.Log("[BlockDrag] PlaceBlock CALLED");

        if (definition == null || gridLogic == null) return;

        foreach (var cell in definition.GetNormalizedCells(rotationSteps))
        {
            int x = baseX + cell.x;
            int y = baseY + cell.y;
            gridLogic.OccupyCell(x, y, definition.blockColor);
        }

        gridLogic.CheckAndClearLines();
        Debug.Log($"[BlockDrag] Placed {definition.name} at base=({baseX},{baseY}), rot={rotationSteps}");
        onPlaced?.Invoke();
        gameObject.SetActive(false);
    }
}