using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TreeSpecies
{
    public string name;                       // "ash", "aspen", "oak", "pine"
    public GameObject[] small = new GameObject[3];
    public GameObject[] medium = new GameObject[3];
    public GameObject[] large = new GameObject[3];

    public GameObject[] ForStage(int stage) =>
        stage == 0 ? small : stage == 1 ? medium : large;
}

public class TreeManager : MonoBehaviour
{
    public TreeSpecies[] species;
    public GameObject growEffectPrefab;       // particle system prefab

    [Header("Placement (dome)")]
    [Tooltip("The Dome Creator rig (the audience position).")]
    public Transform viewer;
    [Tooltip("180 = all the way round. 70 = only the front.")]
    [Range(10f, 180f)] public float azimuthRange = 180f;
    [Tooltip("1 = even spread round the room. 2-3 = more trees in front.")]
    [Range(1f, 4f)] public float frontBias = 1.5f;
    [Tooltip("Min distance should stay above ~6 m to leave visible ground.")]
    public Vector2 distanceRange = new Vector2(6f, 35f);
    public LayerMask groundMask = ~0;
    public float minSpacing = 2f;
    public int maxTrees = 300;

    [Header("Growth")]
    public float stageDuration = 20f;         // seconds per stage
    public float startScale = 0.8f;
    public float endScale = 1.3f;


    readonly List<Vector3> placed = new List<Vector3>();

    [Header("Placement height")]
    public bool useFixedHeight = true;
    public float fixedHeight = 0f;   // world Y where trees are planted

    public bool Plant(string type)
    {
        if (placed.Count >= maxTrees) return false;

        var sp = System.Array.Find(species, s => s.name == type.ToLower());
        if (sp == null) return false;

        if (!TryFindSpot(out var pos)) return false;
        placed.Add(pos);

        var go = new GameObject("Tree_" + sp.name);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
        var tree = go.AddComponent<GrowingTree>();
        tree.Init(this, sp, Random.Range(0, 1));
        return true;
    }

    bool TryFindSpot(out Vector3 result)
    {
        Vector3 center = viewer != null ? viewer.position : transform.position;

        for (int i = 0; i < 30; i++)
        {
            float t = Random.Range(-1f, 1f);
            float az = Mathf.Sign(t) * Mathf.Pow(Mathf.Abs(t), frontBias)
                       * azimuthRange * Mathf.Deg2Rad;

            float d = Mathf.Sqrt(Random.Range(distanceRange.x * distanceRange.x,
                                              distanceRange.y * distanceRange.y));

            var p = center + new Vector3(Mathf.Sin(az), 0f, -Mathf.Cos(az)) * d;

            Vector3 point;
            if (useFixedHeight)
            {
                point = new Vector3(p.x, fixedHeight, p.z);
            }
            else
            {
                p.y += 100f;
                if (!Physics.Raycast(p, Vector3.down, out var hit, 300f, groundMask)) continue;
                point = hit.point;
            }

            bool free = true;
            foreach (var other in placed)
                if ((other - point).sqrMagnitude < minSpacing * minSpacing)
                { free = false; break; }

            if (free) { result = point; return true; }
        }
        result = default;
        return false;
    }

    public void PlayEffect(Vector3 pos)
    {
        if (growEffectPrefab == null) return;
        var fx = Instantiate(growEffectPrefab, pos, Quaternion.identity);
        Destroy(fx, 4f);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 c = viewer != null ? viewer.position : transform.position;
        Gizmos.color = Color.green;
        DrawRing(c, distanceRange.x);
        DrawRing(c, distanceRange.y);
    }

    void DrawRing(Vector3 c, float radius)
    {
        const int steps = 64;
        float range = azimuthRange * Mathf.Deg2Rad;
        Vector3 prev = c + new Vector3(Mathf.Sin(-range), 0f, -Mathf.Cos(-range)) * radius;
        for (int i = 1; i <= steps; i++)
        {
            float a = Mathf.Lerp(-range, range, i / (float)steps);
            Vector3 cur = c + new Vector3(Mathf.Sin(a), 0f, -Mathf.Cos(a)) * radius;
            Gizmos.DrawLine(prev, cur);
            prev = cur;
        }
    }
}