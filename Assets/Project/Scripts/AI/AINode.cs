using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AINode: Represents a single node in the grid used by both the player and AI.
/// Handles signal strength, decay, and special node states (Player, Decoy, Battery, Exit, etc.)
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AINode : GraphNode
{
    [Header("Node Properties")]
    public bool isActive = true;               // whether node is usable
    public float traversalCost = 1f;           // used in A* pathfinding
    public List<AINode> neighbors = new List<AINode>();

    [Header("Signal Properties")]
    [Range(0f, 100f)] public float signalStrength = 0f;   // how strong the trace signal is
    public float maxSignal = 100f;
    public float decayRate = 5f;               // how fast signal fades per second
    public float minSignalVisible = 0.1f;

    [Header("Visual Settings")]
    public NodeState state = NodeState.Idle;
    public Color baseColor = Color.cyan;
    public float glowIntensity = 1.5f;
    public bool showSignalPulse = true;

    private SpriteRenderer sr;
    private float pulseTime;

    // A* fields
    [HideInInspector] public float gCost;
    [HideInInspector] public float fCost;
    [HideInInspector] public AINode parent;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            baseColor = sr.color;
    }

    void Update()
    {
        if (!isActive) return;

        // Decay signal over time for dynamic gameplay
        if (state == NodeState.PlayerTrace || state == NodeState.Decoy)
            DecaySignal();

        UpdateVisuals();
    }

    // Connects this node to another (bidirectional)
    public void Connect(AINode other)
    {
        if (other == null || neighbors.Contains(other)) return;
        neighbors.Add(other);
        other.neighbors.Add(this);
    }

    // Apply a trace signal (e.g., player movement, decoy)
    public void EmitSignal(float amount, NodeState newState)
    {
        if (!isActive) return;

        state = newState;
        signalStrength = Mathf.Clamp(signalStrength + amount, 0, maxSignal);
    }

    // Signal decay logic
    public void DecaySignal()
    {
        if (signalStrength <= 0f) return;

        signalStrength -= decayRate * Time.deltaTime;
        if (signalStrength <= 0f)
        {
            signalStrength = 0f;
            if (state == NodeState.PlayerTrace || state == NodeState.Decoy)
                state = NodeState.Idle;
        }
    }

    // Disable node (e.g., burned or corrupted)
    public void BurnNode()
    {
        isActive = false;
        state = NodeState.Burned;
        signalStrength = 0;
        traversalCost = 999f;
        if (sr) sr.color = Color.black;
    }

    // Visual feedback for states
    private void UpdateVisuals()
    {
        if (!sr) return;

        switch (state)
        {
            case NodeState.Idle:
                sr.color = Color.Lerp(baseColor, Color.gray, 0.3f);
                break;

            case NodeState.PlayerTrace:
                sr.color = Color.Lerp(Color.cyan, Color.white, signalStrength / maxSignal);
                break;

            case NodeState.Decoy:
                sr.color = Color.Lerp(Color.yellow, Color.white, signalStrength / maxSignal);
                break;

            case NodeState.Battery:
                sr.color = Color.green;
                break;

            case NodeState.Exit:
                sr.color = Color.magenta;
                break;

            case NodeState.Burned:
                sr.color = Color.black;
                break;
        }

        // Add pulsing glow effect for active signals
        if (showSignalPulse && (state == NodeState.PlayerTrace || state == NodeState.Decoy))
        {
            pulseTime += Time.deltaTime * 3f;
            float pulse = Mathf.Sin(pulseTime) * 0.3f + 0.7f;
            sr.color *= new Color(pulse, pulse, pulse, 1);
        }
    }

    // Used in A* pathfinding
    public float DistanceTo(AINode other)
    {
        if (other == null) return Mathf.Infinity;
        return Vector2.Distance(transform.position, other.transform.position);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (neighbors == null) return;
        Gizmos.color = Color.gray;
        foreach (var n in neighbors)
        {
            if (n != null)
                Gizmos.DrawLine(transform.position, n.transform.position);
        }
    }
#endif
}

public enum NodeState
{
    Idle,
    PlayerTrace,
    Decoy,
    Battery,
    Exit,
    Burned
}
