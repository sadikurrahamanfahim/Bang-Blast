using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BlockSpawner : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public GridLogic gridLogic;
    public Transform spawnParent;
    public GameObject[] blockPrefabs;

    [Header("Settings")]
    public bool respawnOnPlaced = true;
    public int slotCount = 4; // Number of active blocks in the tray

    private Dictionary<GameObject, Queue<GameObject>> poolDict = new Dictionary<GameObject, Queue<GameObject>>();
    private GameObject[] activeBlocks;
    private Queue<int> pendingSpawnSlots = new Queue<int>();

    private void Awake()
    {
        activeBlocks = new GameObject[slotCount];

        if (blockPrefabs == null || blockPrefabs.Length == 0)
        {
            Debug.LogWarning("[BlockSpawner] blockPrefabs not assigned!");
            blockPrefabs = new GameObject[0];
        }

        // Initialize pool
        foreach (var prefab in blockPrefabs)
        {
            if (prefab == null) continue;
            if (!poolDict.ContainsKey(prefab)) poolDict[prefab] = new Queue<GameObject>();
        }
    }

    private void Start()
    {
        StartCoroutine(SpawnAllDelayed());
    }

    private IEnumerator SpawnAllDelayed()
    {
        yield return null;
        SpawnAll();
    }

    private void Update()
    {
        ProcessPendingSpawns();
    }

    public void SpawnAll()
    {
        ClearTray();

        for (int i = 0; i < slotCount; i++)
        {
            TrySpawnSlot(i);
        }
    }

    private void TrySpawnSlot(int slotIndex)
    {
        var tray = spawnParent.GetComponent<BlockTrayManager>();
        if (tray != null && !tray.CanSpawnNewBlock())
        {
            if (!pendingSpawnSlots.Contains(slotIndex))
                pendingSpawnSlots.Enqueue(slotIndex);
            return;
        }

        SpawnOne(slotIndex);
        tray?.OnBlocksSpawned();
    }

    private void ProcessPendingSpawns()
    {
        if (pendingSpawnSlots.Count == 0) return;

        var tray = spawnParent.GetComponent<BlockTrayManager>();
        if (tray == null) return;

        while (pendingSpawnSlots.Count > 0 && tray.CanSpawnNewBlock())
        {
            int slotIndex = pendingSpawnSlots.Dequeue();
            SpawnOne(slotIndex);
            tray.OnBlocksSpawned();
        }
    }

    private void SpawnOne(int slotIndex)
    {
        if (blockPrefabs.Length == 0) return;

        GameObject prefab = blockPrefabs[Random.Range(0, blockPrefabs.Length)];
        if (prefab == null) return;

        GameObject block = GetFromPool(prefab);

        var blockInstance = block.GetComponent<BlockInstance>();
        if (blockInstance == null) blockInstance = block.AddComponent<BlockInstance>();
        blockInstance.prefabRef = prefab;

        block.transform.SetParent(spawnParent, false);
        block.SetActive(true);
        block.transform.SetAsLastSibling();

        var drag = block.GetComponent<BlockDrag>();
        if (drag != null)
        {
            drag.enabled = true;
            drag.gridManager = gridManager;
            drag.gridLogic = gridLogic;
            drag.onPlaced = null;
            drag.onPlacementFailed = null;
            drag.rotationSteps = Random.Range(0, 4);

            if (respawnOnPlaced)
            {
                int capturedIndex = slotIndex;
                drag.onPlaced += () => OnBlockPlaced(capturedIndex, block);
                drag.onPlacementFailed += () => OnBlockPlacementFailed(capturedIndex, block);
            }
        }

        activeBlocks[slotIndex] = block;
    }

    private void OnBlockPlaced(int slotIndex, GameObject instance)
    {
        StartCoroutine(HandlePlacedDelayed(slotIndex, instance));
    }

    private void OnBlockPlacementFailed(int slotIndex, GameObject instance)
    {
        if (instance == null) return;

        activeBlocks[slotIndex] = null;

        var tray = spawnParent.GetComponent<BlockTrayManager>();
        tray?.OnBlockRemoved(instance.transform);

        Destroy(instance);

        StartCoroutine(RespawnAfterFailed(slotIndex));
    }

    private IEnumerator RespawnAfterFailed(int slotIndex)
    {
        yield return new WaitForSeconds(0.1f);
        TrySpawnSlot(slotIndex);
    }

    private IEnumerator HandlePlacedDelayed(int slotIndex, GameObject instance)
    {
        yield return null;

        activeBlocks[slotIndex] = null;

        if (gridLogic != null)
            gridLogic.CheckGameOver(GetActiveBlocks());

        TrySpawnSlot(slotIndex);
    }

    public List<BlockInfo> GetActiveBlocks()
    {
        List<BlockInfo> list = new List<BlockInfo>();

        foreach (var go in activeBlocks)
        {
            if (go == null) continue;
            var drag = go.GetComponent<BlockDrag>();
            if (drag != null && drag.definition != null)
                list.Add(new BlockInfo(drag.definition, drag.rotationSteps));
        }

        return list;
    }

    public void ReplaceDestroyedBlock(GameObject destroyedBlock)
    {
        for (int i = 0; i < activeBlocks.Length; i++)
        {
            if (activeBlocks[i] == destroyedBlock)
            {
                activeBlocks[i] = null;
                StartCoroutine(RespawnAfterFailed(i));
                return;
            }
        }
    }

    public void ClearTray()
    {
        pendingSpawnSlots.Clear();

        for (int i = 0; i < activeBlocks.Length; i++)
        {
            var inst = activeBlocks[i];
            if (inst == null) continue;

            var tag = inst.GetComponent<BlockInstance>();
            if (tag != null && tag.prefabRef != null)
                ReturnToPool(tag.prefabRef, inst);
            else
                inst.SetActive(false);

            activeBlocks[i] = null;
        }
    }

    private GameObject GetFromPool(GameObject prefab)
    {
        if (!poolDict.ContainsKey(prefab))
            poolDict[prefab] = new Queue<GameObject>();

        if (poolDict[prefab].Count > 0)
            return poolDict[prefab].Dequeue();

        var inst = Instantiate(prefab);
        var tag = inst.GetComponent<BlockInstance>();
        if (tag == null) tag = inst.AddComponent<BlockInstance>();
        tag.prefabRef = prefab;
        return inst;
    }

    private void ReturnToPool(GameObject prefab, GameObject instance)
    {
        if (instance == null || prefab == null) return;

        instance.SetActive(false);
        instance.transform.SetParent(transform, false);

        if (!poolDict.ContainsKey(prefab))
            poolDict[prefab] = new Queue<GameObject>();

        poolDict[prefab].Enqueue(instance);
    }
}
