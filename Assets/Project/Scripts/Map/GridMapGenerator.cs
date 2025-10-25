using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[ExecuteAlways]
public class GridMapGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public float desiredSpacing = 1.2f;
    public float edgePadding = 0.5f;
    public bool connectDiagonals = false;
    public bool randomHoles = false;
    [Range(0f, 1f)] public float holeChance = 0.0f;

    [Header("Prefabs & References")]
    public GameObject nodePrefab;
    public Camera targetCamera;
    public bool autoGenerateOnStart = true;

    [Header("Special Node Counts")]
    public int batteryCount = 3;
    public bool showDebugLines = true;

    private List<AINode> nodes = new List<AINode>();


    [ContextMenu("Generate Full Dynamic Grid")]
    public void GenerateGrid()
    {
        if (nodePrefab == null)
        {
            Debug.LogError("GridMapGenerator: Please assign a Node Prefab!");
            return;
        }

        if (targetCamera == null)
            targetCamera = Camera.main;

        ClearExisting();
        nodes.Clear();

        // --- Camera world bounds ---
        float camHeight = targetCamera.orthographicSize * 2f;
        float camWidth = camHeight * targetCamera.aspect;
        float usableWidth = camWidth - (edgePadding * 2f);
        float usableHeight = camHeight - (edgePadding * 2f);

        int width = Mathf.Max(2, Mathf.RoundToInt(usableWidth / desiredSpacing));
        int height = Mathf.Max(2, Mathf.RoundToInt(usableHeight / desiredSpacing));

        float spacingX = usableWidth / Mathf.Max(1, (width - 1));
        float spacingY = usableHeight / Mathf.Max(1, (height - 1));

        Vector3 bottomLeft = targetCamera.transform.position
            - new Vector3(camWidth / 2f, camHeight / 2f, 0f)
            + new Vector3(edgePadding, edgePadding, 0f);

        // --- Spawn nodes ---
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (randomHoles && Random.value < holeChance)
                    continue;

                Vector3 pos = bottomLeft + new Vector3(x * spacingX, y * spacingY, 0f);
                pos.z = 0f;

                GameObject go = Instantiate(nodePrefab, pos, Quaternion.identity, transform);
                go.name = $"Node_{x}_{y}";

                AINode node = go.GetComponent<AINode>();
                if (node == null) node = go.AddComponent<AINode>();

                nodes.Add(node);
            }
        }

        ConnectNodes(Mathf.Max(spacingX, spacingY));
        AssignSpecialNodes();
        Debug.Log($"✅ Generated grid {width}x{height} ({nodes.Count} nodes).");
    }

    private void ConnectNodes(float spacing)
    {
        float maxDist = spacing * 1.5f;
        foreach (var a in nodes)
        {
            a.neighbors.Clear();
            foreach (var b in nodes)
            {
                if (a == b) continue;
                float d = Vector2.Distance(a.transform.position, b.transform.position);
                if (d <= maxDist)
                {
                    if (!connectDiagonals)
                    {
                        Vector2 diff = b.transform.position - a.transform.position;
                        if (Mathf.Abs(diff.x) > 0.1f && Mathf.Abs(diff.y) > 0.1f)
                            continue;
                    }
                    a.Connect(b);
                }
            }
        }
    }

    private void AssignSpecialNodes()
    {
        if (nodes.Count < 4) return;

        // Sort by position for simple quadrant logic
        float midX = nodes.Average(n => n.transform.position.x);
        float midY = nodes.Average(n => n.transform.position.y);

        var bottomLeftNodes = nodes.Where(n => n.transform.position.x < midX && n.transform.position.y < midY).ToList();
        var topRightNodes = nodes.Where(n => n.transform.position.x > midX && n.transform.position.y > midY).ToList();

        // --- Player node ---
        AINode playerNode = bottomLeftNodes[Random.Range(0, bottomLeftNodes.Count)];
        playerNode.state = NodeState.PlayerTrace;
        playerNode.EmitSignal(50f, NodeState.PlayerTrace);

        // --- AI node ---
        AINode aiNode = topRightNodes[Random.Range(0, topRightNodes.Count)];
        aiNode.state = NodeState.Decoy; // just to visualize differently
        aiNode.EmitSignal(30f, NodeState.Decoy);

        // --- Exit node ---
        AINode exitNode = nodes.OrderByDescending(n => Vector2.Distance(n.transform.position, playerNode.transform.position)).First();
        exitNode.state = NodeState.Exit;

        // --- Battery nodes ---
        var available = nodes.Except(new[] { playerNode, aiNode, exitNode }).ToList();
        for (int i = 0; i < Mathf.Min(batteryCount, available.Count); i++)
        {
            int r = Random.Range(0, available.Count);
            AINode battery = available[r];
            battery.state = NodeState.Battery;
            available.RemoveAt(r);
        }

        Debug.Log($"Player at {playerNode.name}, AI at {aiNode.name}, Exit: {exitNode.name}, Batteries: {batteryCount}");
    }

    [ContextMenu("Clear Grid")]
    public void ClearExisting()
    {
        var children = new List<GameObject>();
        foreach (Transform child in transform)
            children.Add(child.gameObject);

        foreach (var c in children)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.Undo.DestroyObjectImmediate(c);
            else
                Destroy(c);
#else
            Destroy(c);
#endif
        }
        nodes.Clear();
    }

    private void Start()
    {
        if (Application.isPlaying && autoGenerateOnStart)
            GenerateGrid();
    }
}
