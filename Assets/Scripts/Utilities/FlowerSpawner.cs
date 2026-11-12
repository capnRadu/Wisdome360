using System.Collections.Generic;
using UnityEngine;

public class FlowerSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public GameObject prefab;
        [Min(0f)] public float weight = 1f;   // higher = spawns more often
        public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    }

    public enum Shape { Rectangle, Ring }

    [Header("What to spawn")]
    public Entry[] entries = new Entry[3];
    [Min(0)] public int maxCount = 200;

    [Header("Where (centred on this object, or on 'center' if set)")]
    public Transform center;
    public Shape shape = Shape.Ring;
    [Tooltip("Rectangle only: full width (X) and depth (Z).")]
    public Vector2 rectSize = new Vector2(40f, 40f);
    [Tooltip("Ring only: flowers spawn between these two radii.")]
    public Vector2 ringRadius = new Vector2(5f, 40f);

    [Header("Ground")]
    public bool useFixedHeight = false;
    public float fixedHeight = 0f;
    public LayerMask groundMask = ~0;
    public float rayStartHeight = 100f;
    public bool alignToSurface = false;       // tilt to the ground normal (raycast mode only)

    [Header("Placement rules")]
    public float minSpacing = 0.5f;
    [Tooltip("Optional: skip spots that overlap colliders on these layers (e.g. trees).")]
    public LayerMask avoidMask = 0;
    public float avoidRadius = 1f;
    public int attemptsPerFlower = 20;

    [Header("Run")]
    public bool spawnOnStart = true;
    public bool useSeed = false;
    public int seed = 12345;

    readonly List<Vector3> placed = new List<Vector3>();
    Transform container;

    void Start()
    {
        if (spawnOnStart) Spawn();
    }

    [ContextMenu("Spawn")]
    public void Spawn()
    {
        Clear();
        if (useSeed) Random.InitState(seed);

        float total = 0f;
        foreach (var e in entries)
            if (e != null && e.prefab != null) total += e.weight;
        if (total <= 0f) { Debug.LogWarning("FlowerSpawner: no prefabs with weight > 0."); return; }

        container = new GameObject("Flowers").transform;
        container.SetParent(transform, false);

        int spawned = 0;
        int budget = maxCount * attemptsPerFlower;   // overall safety limit

        while (spawned < maxCount && budget-- > 0)
        {
            if (!TryFindSpot(out var pos, out var normal)) continue;

            var e = PickEntry(total);
            var rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            if (alignToSurface && !useFixedHeight)
                rot = Quaternion.FromToRotation(Vector3.up, normal) * rot;

            var go = Instantiate(e.prefab, pos, rot, container);
            go.transform.localScale *= Random.Range(e.scaleRange.x, e.scaleRange.y);

            placed.Add(pos);
            spawned++;
        }

        if (spawned < maxCount)
            Debug.Log($"FlowerSpawner: placed {spawned}/{maxCount}. Area full or too many blocked spots.");
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        placed.Clear();
        var old = transform.Find("Flowers");
        if (old != null)
        {
            if (Application.isPlaying) Destroy(old.gameObject);
            else DestroyImmediate(old.gameObject);
        }
    }

    Entry PickEntry(float total)
    {
        float r = Random.value * total;
        foreach (var e in entries)
        {
            if (e == null || e.prefab == null) continue;
            if (r < e.weight) return e;
            r -= e.weight;
        }
        // floating point fallback: last valid entry
        for (int i = entries.Length - 1; i >= 0; i--)
            if (entries[i] != null && entries[i].prefab != null) return entries[i];
        return null;
    }

    bool TryFindSpot(out Vector3 pos, out Vector3 normal)
    {
        Vector3 c = center != null ? center.position : transform.position;
        normal = Vector3.up;

        Vector2 offset;
        if (shape == Shape.Rectangle)
        {
            offset = new Vector2(Random.Range(-rectSize.x / 2f, rectSize.x / 2f),
                                 Random.Range(-rectSize.y / 2f, rectSize.y / 2f));
        }
        else
        {
            // sqrt of squared radii = even density across the ring
            float r = Mathf.Sqrt(Random.Range(ringRadius.x * ringRadius.x, ringRadius.y * ringRadius.y));
            float a = Random.Range(0f, Mathf.PI * 2f);
            offset = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }

        float x = c.x + offset.x;
        float z = c.z + offset.y;

        if (useFixedHeight)
        {
            pos = new Vector3(x, fixedHeight, z);
        }
        else
        {
            var origin = new Vector3(x, c.y + rayStartHeight, z);
            if (!Physics.Raycast(origin, Vector3.down, out var hit, rayStartHeight * 3f, groundMask))
            { pos = default; return false; }
            pos = hit.point;
            normal = hit.normal;
        }

        float minSq = minSpacing * minSpacing;
        foreach (var other in placed)
            if ((other - pos).sqrMagnitude < minSq) return false;

        if (avoidMask.value != 0 && Physics.CheckSphere(pos, avoidRadius, avoidMask))
            return false;

        return true;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 c = center != null ? center.position : transform.position;
        Gizmos.color = new Color(1f, 0.6f, 0.9f);
        if (shape == Shape.Rectangle)
            Gizmos.DrawWireCube(c, new Vector3(rectSize.x, 0.1f, rectSize.y));
        else
        {
            DrawCircle(c, ringRadius.x);
            DrawCircle(c, ringRadius.y);
        }
    }

    void DrawCircle(Vector3 c, float radius)
    {
        const int steps = 64;
        Vector3 prev = c + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= steps; i++)
        {
            float a = i / (float)steps * Mathf.PI * 2f;
            Vector3 cur = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
            Gizmos.DrawLine(prev, cur);
            prev = cur;
        }
    }
}