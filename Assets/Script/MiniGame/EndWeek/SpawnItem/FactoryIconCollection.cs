using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FactoryIconCollection : MonoBehaviour
{
    [SerializeField] private IconCollectionBehaviour prefab;
    [SerializeField] private RectTransform container;
    [SerializeField] private ScreenBoudaries boundaries;

    [SerializeField] private bool logSpawnPosition = true;
    [SerializeField] private bool forcePositionNextFrame = true;

    private FactoryManager factory;
    private readonly List<IconCollectionBehaviour> activeIcons = new();

    private void Awake()
    {
        factory = new FactoryManager();
    }

    public IconCollectionBehaviour Spawn(ItemEntry entry, Vector3 worldPos, float delay = 0f)
    {
        var icon = factory.Create(prefab);
        if (icon == null) return null;

        icon.transform.SetParent(container, false);
        icon.gameObject.SetActive(true);

        icon.Setup(entry.Type, entry.Sprite, delay);
        icon.transform.position = worldPos;

        var drag = icon.GetComponent<DragHandler2D>();
        if (drag != null)
        {
            drag.Boundaries = boundaries;
        }

        icon.OnCompleted -= Despawn;
        icon.OnCompleted += Despawn;
        
        icon.Begin();

        icon.transform.position = worldPos;

        activeIcons.Add(icon);

        if (logSpawnPosition || forcePositionNextFrame)
            StartCoroutine(CheckPosition(icon, worldPos));

        return icon;
    }

    private IEnumerator CheckPosition(IconCollectionBehaviour icon, Vector3 target)
    {
        if (logSpawnPosition)
            Debug.Log($"[Spawn] target={target} afterBegin={icon.transform.position}", icon);

        yield return null;

        if (icon == null || !icon.gameObject.activeInHierarchy) yield break;

        if (logSpawnPosition)
            Debug.Log($"[Spawn] nextFrame={icon.transform.position} (target={target})", icon);

        if (forcePositionNextFrame &&
            (icon.transform.position - target).sqrMagnitude > 0.0001f)
        {
            icon.transform.position = target;
        }
    }

    public void Despawn(IconCollectionBehaviour icon)
    {
        activeIcons.Remove(icon);
        factory.Release(icon.gameObject);
    }

    public void DespawnAll()
    {
        for (int i = activeIcons.Count - 1; i >= 0; i--)
            factory.Release(activeIcons[i].gameObject);
        activeIcons.Clear();
    }
}