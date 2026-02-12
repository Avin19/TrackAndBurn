using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// AI that moves across the grid using A* pathfinding,
/// dynamically connecting nodes toward the player node.
/// </summary>
public class AIPathTracer : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public GridMapGenerator gridGenerator;
    public GameObject connectionPrefab;

    [Header("AI Behavior")]
    public float reScanInterval = 2f;
    public float basePingSpeed = 3f;
    public float pingAcceleration = 1.05f;
    public Color connectionColor = Color.cyan;

    private AINode currentNode;
    private List<AINode> path;
    private bool isTracing;
    private float currentPingSpeed;
    private List<AINode> allNodes = new List<AINode>();

    void Start()
    {
        if (gridGenerator == null)
            gridGenerator = FindObjectOfType<GridMapGenerator>();
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        }

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
                StartCoroutine(TraceToPlayer());

            yield return new WaitForSeconds(reScanInterval);
        }
    }

    IEnumerator TraceToPlayer()
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
        AINode targetNode = FindNearestNode(playerTransform.position, allNodes);

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

            // Optional: Check for reaching player proximity
            if (Vector2.Distance(playerTransform.position, currentNode.transform.position) < 0.5f)
            {
                Debug.Log("<color=red>AI reached player!</color>");
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

        // Create visual link
        if (connectionPrefab != null)
        {
            GameObject lineObj = Instantiate(connectionPrefab);
            var vis = lineObj.GetComponent<AIConnectionVisualizer>();
            if (vis != null)
                vis.Initialize(start, end, connectionColor, 2.5f);
        }

        currentPingSpeed *= pingAcceleration;
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
