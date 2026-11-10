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

    [Header("Placement")]
    public Vector3 areaCenter = Vector3.zero;
    public Vector2 areaSize = new Vector2(50, 50);
    public LayerMask groundMask = ~0;
    public float minSpacing = 2f;

    [Header("Growth")]
    public float stageDuration = 20f;         // seconds per stage
    public float startScale = 0.8f;
    public float endScale = 1.3f;

    readonly List<Vector3> placed = new List<Vector3>();

    public bool Plant(string type)
    {
        var sp = System.Array.Find(species, s => s.name == type.ToLower());
        if (sp == null) return false;

        if (!TryFindSpot(out var pos)) return false;
        placed.Add(pos);

        var go = new GameObject("Tree_" + sp.name);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0));
        var tree = go.AddComponent<GrowingTree>();
        tree.Init(this, sp, Random.Range(0, 3));
        return true;
    }

    bool TryFindSpot(out Vector3 result)
    {
        for (int i = 0; i < 30; i++)
        {
            var p = areaCenter + new Vector3(
                Random.Range(-areaSize.x / 2, areaSize.x / 2), 100f,
                Random.Range(-areaSize.y / 2, areaSize.y / 2));

            if (!Physics.Raycast(p, Vector3.down, out var hit, 300f, groundMask)) continue;

            bool free = true;
            foreach (var other in placed)
                if ((other - hit.point).sqrMagnitude < minSpacing * minSpacing) { free = false; break; }

            if (free) { result = hit.point; return true; }
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
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(areaCenter, new Vector3(areaSize.x, 1, areaSize.y));
    }
}