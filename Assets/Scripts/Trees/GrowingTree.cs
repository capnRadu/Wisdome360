using UnityEngine;

public class GrowingTree : MonoBehaviour
{
    TreeManager manager;
    TreeSpecies species;
    int variation;
    int stage = -1;
    float age;
    GameObject model;

    public void Init(TreeManager m, TreeSpecies s, int v)
    {
        manager = m; species = s; variation = v;
        SetStage(0, false);
        transform.localScale = Vector3.one * manager.startScale;
        manager.PlayEffect(transform.position);   // planting puff (optional)
    }

    void Update()
    {
        age += Time.deltaTime;
        float total = manager.stageDuration * 3f;
        float progress = Mathf.Clamp01(age / total);

        int newStage = Mathf.Min(2, Mathf.FloorToInt(age / manager.stageDuration));
        if (newStage != stage) SetStage(newStage, true);

        transform.localScale = Vector3.one * Mathf.Lerp(manager.startScale, manager.endScale, progress);

        if (progress >= 1f) enabled = false;       // fully grown, stop updating
    }

    void SetStage(int newStage, bool effect)
    {
        stage = newStage;
        if (model != null) Destroy(model);
        var prefab = species.ForStage(stage)[variation];
        model = Instantiate(prefab, transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        if (effect) manager.PlayEffect(transform.position);
    }
}