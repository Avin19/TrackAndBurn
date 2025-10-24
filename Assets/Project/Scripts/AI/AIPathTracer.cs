using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// AI controller that moves across a node graph, connecting nodes dynamically
/// toward player or strongest signal (decoy, player trace, etc.).
/// Draws visual "ping" connections along its path.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class AIPathTracer : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public GridMapGenerator gridGenerator;
    [Tooltip("Prefab containing LineRenderer + AIConnectionVisualizer component")]
    public GameObject connectionPrefab;

    [Header("AI Behavior")]
    public float reScanInterval = 2f;          // how often AI recalculates path
    public float basePingSpeed = 3f;           // movement speed between nodes
    public float pingAcceleration = 1.05f;     // speed multiplier per node hop
    public float signalWeightBias = 1.5f;      // higher = AI prefers stronger signals over distance

    [Header("Visuals")]
    public Color connectionColor = Color.cyan;
    public bool visualizeConnections = true;

    private AINode currentNode;
    private List<AINode> path;
    private bool isTracing;
    private float currentPingSpeed;
    private List<AINode> allNodes = new List<AINode>();

    void Start()
    {
        if (gridGenerator == null)
            gridGenerator = FindObjectOfType<GridMapGenerator>();

        allNodes = gridGenerator.GetComponentsInChildren<AINode>().ToList();
        currentPingSpeed = basePingSpeed;

        StartCoroutine(MainLoop());
    }

    IEnumerator MainLoop()
    {
        yield return new WaitForSeconds(1f);

        while (true)
        {
            if (!isTracing)
                StartCoroutine(TraceToBestTarget());

            yield return new WaitForSeconds(reScanInterval);
        }
    }

    IEnumerator TraceToBestTarget()
    {
        isTracing = true;

        if (allNodes == null || allNodes.Count == 0)
        {
            allNodes = gridGenerator.GetComponentsInChildren<AINode>().ToList();
            if (allNodes.Count == 0)
            {
                Debug.LogWarning("AIPathTracer: No nodes found!");
                isTracing = false;
                yield break;
            }
        }

        currentNode = FindNearestNode(transform.position, allNodes);

        // choose best target node (player, decoy, or exit)
        AINode targetNode = ChooseTargetNode();
        if (targetNode == null)
        {
            isTracing = false;
            yield break;
        }

        path = GraphAStar.FindPath(currentNode, targetNode);
        if (path == null || path.Count < 2)
        {
            isTracing = false;
            yield break;
        }

        for (int i = 1; i < path.Count; i++)
        {
            AINode nextNode = path[i];
            yield return MoveAndConnect(currentNode, nextNode);
            currentNode = nextNode;

            if (Vector2.Distance(playerTransform.position, currentNode.transform.position) < 0.5f)
            {
                Debug.Log("<color=red>AI reached player node!</color>");
                break;
            }
        }

        isTracing = false;
    }

    IEnumerator MoveAndConnect(AINode from, AINode to)
    {
        float dist = Vector2.Distance(from.transform.position, to.transform.position);
        float travelTime = dist / currentPingSpeed;
        float t = 0f;

        Vector3 start = from.transform.position;
        Vector3 end = to.transform.position;

        while (t < 1f)
        {
            t += Time.deltaTime / travelTime;
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        if (visualizeConnections)
            CreateConnection(start, end);

        currentPingSpeed *= pingAcceleration;

        // AI “pings” the node (increases its signal temporarily)
        if (to != null)
            to.EmitSignal(5f, NodeState.Decoy);
    }

    void CreateConnection(Vector3 from, Vector3 to)
    {
        if (connectionPrefab == null) return;

        GameObject lineObj = Instantiate(connectionPrefab);
        var vis = lineObj.GetComponent<AIConnectionVisualizer>();
        if (vis != null)
            vis.Initialize(from, to, connectionColor, 3.0f);
    }

    /// <summary>
    /// Picks the best target node based on signal strength + proximity.
    /// </summary>
    AINode ChooseTargetNode()
    {
        float bestScore = -Mathf.Infinity;
        AINode bestNode = null;

        foreach (var node in allNodes)
        {
            if (node == null || !node.isActive) continue;

            float signal = node.signalStrength;
            float distance = Vector2.Distance(transform.position, node.transform.position);

            float score = (signal * signalWeightBias) - distance;

            if (node.state == NodeState.PlayerTrace)
                score += 25f; // strong bias for player trace
            else if (node.state == NodeState.Exit)
                score -= 10f; // avoid exit unless no other signal

            if (score > bestScore)
            {
                bestScore = score;
                bestNode = node;
            }
        }

        if (bestNode == null)
        {
            Debug.LogWarning("AIPathTracer: No valid target node found, defaulting to player node.");
            bestNode = FindNearestNode(playerTransform.position, allNodes);
        }

        return bestNode;
    }

    AINode FindNearestNode(Vector2 pos, List<AINode> nodes)
    {
        AINode best = null;
        float bestDist = float.MaxValue;
        foreach (var n in nodes)
        {
            float d = Vector2.Distance(pos, n.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = n;
            }
        }
        return best;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (path == null || path.Count < 2) return;
        Gizmos.color = Color.red;
        for (int i = 0; i < path.Count - 1; i++)
        {
            if (path[i] && path[i + 1])
                Gizmos.DrawLine(path[i].transform.position, path[i + 1].transform.position);
        }
    }
#endif
}
