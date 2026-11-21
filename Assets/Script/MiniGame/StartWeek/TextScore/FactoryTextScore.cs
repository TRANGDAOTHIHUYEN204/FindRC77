using UnityEngine;

public class FactoryTextScore : MonoBehaviour
{
    [SerializeField] private TextScoreBehaviour prefab;
    private readonly FactoryManager factory = new FactoryManager();

    public TextScoreBehaviour Create(RectTransform parent)
    {
        TextScoreBehaviour t = factory.Create(prefab);
        if (t == null) return null;

        t.transform.SetParent(parent, false);
        t.Init();
        return t;
    }

    public void Release(TextScoreBehaviour t)
    {
        if (t != null) factory.Release(t.gameObject);
    }
}