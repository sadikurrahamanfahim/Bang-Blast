using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BlockTrayManager : MonoBehaviour
{
    [Header("Tray Settings")]
    [SerializeField] private bool autoCalculateTrayWidth = true;
    [SerializeField] private float manualTrayWidth = 750f;
    private float trayWidth;

    [Header("Block Settings")]
    public float blockScale = 0.68f;
    public float spacing = 80f; // Space between blocks

    [Header("Conveyor Settings")]
    [SerializeField] private float scrollSpeed = 120f;
    [SerializeField] private float lockedY = 0f;
    [SerializeField] private float spawnOffsetFromRight = 200f;
    [SerializeField] private float destroyLeftLimit = -400f;

    private RectTransform trayRT;
    private Dictionary<Transform, bool> positionedBlocks = new Dictionary<Transform, bool>();

    private void Awake()
    {
        trayRT = GetComponent<RectTransform>();

        trayWidth = autoCalculateTrayWidth ? trayRT.rect.width : manualTrayWidth;

        // Disable HorizontalLayoutGroup if exists
        var layout = GetComponent<HorizontalLayoutGroup>();
        if (layout != null) layout.enabled = false;
    }

    private void Update()
    {
        ScrollBlocks();
        CheckAndDestroyOffscreenBlocks();
    }

    public bool CanSpawnNewBlock()
    {
        RectTransform rightMost = GetRightMostBlock();
        if (rightMost == null) return true;

        float rightEdge = rightMost.anchoredPosition.x + (rightMost.rect.width * blockScale);
        float maxSpawnX = trayWidth * 0.5f + spawnOffsetFromRight;

        return rightEdge + spacing <= maxSpawnX;
    }

    public float GetNextSpawnPosition()
    {
        RectTransform rightMost = GetRightMostBlock();
        if (rightMost == null)
        {
            return trayWidth * 0.5f + spawnOffsetFromRight;
        }

        float rightEdge = rightMost.anchoredPosition.x + (rightMost.rect.width * blockScale);
        return rightEdge + spacing;
    }

    public void OnBlocksSpawned()
    {
        PositionNewBlocks();
    }

    private void PositionNewBlocks()
    {
        List<Transform> newBlocks = new List<Transform>();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null || !child.gameObject.activeSelf) continue;

            if (!positionedBlocks.ContainsKey(child) || !positionedBlocks[child])
                newBlocks.Add(child);
        }

        foreach (Transform child in newBlocks)
        {
            RectTransform childRT = child as RectTransform;
            if (childRT == null) continue;

            childRT.localScale = Vector3.one * blockScale;

            float spawnX = GetNextSpawnPosition();
            childRT.anchoredPosition = new Vector2(spawnX, lockedY);

            positionedBlocks[child] = true;
        }
    }

    private void ScrollBlocks()
    {
        if (transform.childCount == 0) return;

        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf) continue;

            Vector2 pos = child.anchoredPosition;
            pos.x -= scrollSpeed * Time.deltaTime;
            pos.y = lockedY;
            child.anchoredPosition = pos;
        }
    }

    private void CheckAndDestroyOffscreenBlocks()
    {
        List<Transform> toDestroy = new List<Transform>();

        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf) continue;

            float rightEdge = child.anchoredPosition.x + (child.rect.width * blockScale);
            if (rightEdge < destroyLeftLimit) toDestroy.Add(child);
        }

        foreach (Transform block in toDestroy)
        {
            positionedBlocks.Remove(block);

            var drag = block.GetComponent<BlockDrag>();
            if (drag != null) drag.enabled = false;

            // Notify spawner to replace block
            var spawner = FindObjectOfType<BlockSpawner>();
            spawner?.ReplaceDestroyedBlock(block.gameObject);

            Destroy(block.gameObject);
        }
    }

    public void OnBlockRemoved(Transform block)
    {
        positionedBlocks.Remove(block);
    }

    private RectTransform GetRightMostBlock()
    {
        RectTransform rightMost = null;
        float maxX = float.MinValue;

        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf) continue;

            float rightEdge = child.anchoredPosition.x + (child.rect.width * blockScale);
            if (rightEdge > maxX)
            {
                maxX = rightEdge;
                rightMost = child;
            }
        }

        return rightMost;
    }
}
