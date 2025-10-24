using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AIConnectionVisualizer : MonoBehaviour
{
    private LineRenderer line;
    public float fadeDuration = 2.5f;
    private float lifeTimer = 0f;
    private Color baseColor = Color.cyan;

    void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.startWidth = 0.06f;
        line.endWidth = 0.06f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = baseColor;
        line.endColor = baseColor;
    }

    public void Initialize(Vector3 from, Vector3 to, Color color, float lifetime)
    {
        if (line == null) line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        baseColor = color;
        fadeDuration = lifetime;
        lifeTimer = lifetime;
        line.startColor = color;
        line.endColor = color;
    }

    void Update()
    {
        if (fadeDuration > 0)
        {
            lifeTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(lifeTimer / fadeDuration);
            Color c = baseColor * new Color(1, 1, 1, t);
            line.startColor = c;
            line.endColor = c;

            if (lifeTimer <= 0)
                Destroy(gameObject);
        }
    }
}
