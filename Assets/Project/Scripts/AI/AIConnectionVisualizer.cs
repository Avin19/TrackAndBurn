using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AIConnectionVisualizer : MonoBehaviour
{
    private LineRenderer line;
    private Color baseColor;
    private float lifetime;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startWidth = 0.06f;
        line.endWidth = 0.06f;
    }

    public void Initialize(Vector3 from, Vector3 to, Color color, float duration)
    {
        line.positionCount = 2;
        // Slight Z offset so it’s always visible above grid
        line.SetPosition(0, from + Vector3.back * 0.1f);
        line.SetPosition(1, to + Vector3.back * 0.1f);

        baseColor = color;
        line.startColor = color;
        line.endColor = color;
        lifetime = duration;
    }

    void Update()
    {
        if (lifetime > 0)
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0)
                Destroy(gameObject);
        }
    }
}
