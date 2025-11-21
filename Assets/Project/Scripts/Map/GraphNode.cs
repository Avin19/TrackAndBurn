using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single node (dot) in the network grid.
/// Can be connected to other nodes and used for pathfinding (A*).
/// Extendable for device, decoy, or battery node types.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class GraphNode : MonoBehaviour
{
    [Header("Node Properties")]
    public string nodeID;
    public NodeType nodeType = NodeType.Normal;

    [Tooltip("Energy cost to traverse through this node (used by AI).")]
    public float traversalCost = 1f;

    [Tooltip("Signal strength emitted by this node (for AI tracking).")]
    [Range(0f, 100f)] public float signalStrength = 10f;

    [Tooltip("If true, this node is currently active (not burned or offline).")]
    public bool isActive = true;

    [Header("Connections")]
    public List<GraphNode> neighbors = new List<GraphNode>();

    // --- RUNTIME FIELDS for A* ---
    [HideInInspector] public float gCost;       // cost from start
    [HideInInspector] public float fCost;       // g + heuristic
    [HideInInspector] public GraphNode parent;  // previous node in path

    [Header("Debug Visuals")]
    public Color nodeColor = Color.cyan;
    public float gizmoRadius = 0.15f;

    private void Awake()
    {
        // Generate a unique ID if not set
        if (string.IsNullOrEmpty(nodeID))
            nodeID = System.Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Calculates distance (heuristic) to another node.
    /// </summary>
    public float DistanceTo(GraphNode other)
    {
        if (other == null) return float.MaxValue;
        return Vector2.Distance(transform.position, other.transform.position);
    }

    /// <summary>
    /// Adds a neighbor connection if not already connected.
    /// </summary>
    public void Connect(GraphNode target)
    {
        if (target == null || target == this) return;
        if (!neighbors.Contains(target))
            neighbors.Add(target);
        if (!target.neighbors.Contains(this))
            target.neighbors.Add(this);
    }

    /// <summary>
    /// Disconnects from a neighbor.
    /// </summary>
    public void Disconnect(GraphNode target)
    {
        if (target == null) return;
        neighbors.Remove(target);
        target.neighbors.Remove(this);
    }

    /// <summary>
    /// Marks node as burned/offline (signal disabled).
    /// </summary>
    public void DeactivateNode()
    {
        isActive = false;
        signalStrength = 0f;
        nodeColor = Color.gray;
    }

    /// <summary>
    /// Reactivates node (useful for respawn or recharge).
    /// </summary>
    public void ActivateNode(float strength = 10f)
    {
        isActive = true;
        signalStrength = strength;
        nodeColor = Color.cyan;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isActive ? nodeColor : Color.gray;
        Gizmos.DrawSphere(transform.position, gizmoRadius);

        // draw neighbor lines
        if (neighbors != null)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            foreach (var n in neighbors)
            {
                if (n != null)
                    Gizmos.DrawLine(transform.position, n.transform.position);
            }
        }

        // draw ID in Scene view (optional)
#if UNITY_EDITOR
        if (UnityEditor.SceneView.lastActiveSceneView != null)
        {
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.2f, nodeType.ToString());
        }
#endif
    }
}

/// <summary>
/// Node type helps AI and game logic differentiate special nodes.
/// </summary>
public enum NodeType
{
    Normal,     // Default grid node
    Device,     // Can be burned
    Decoy,      // Fake node placed by player
    Battery,    // Restores player energy
    Exit        // Goal node for player
}
