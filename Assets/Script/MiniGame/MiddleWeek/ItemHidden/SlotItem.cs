using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlot : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private RectTransform flyTarget;
    [SerializeField] private TMP_Text countText;

    public int Id { get; private set; }
    public RectTransform FlyTarget => flyTarget;

    public void Setup(int id, Sprite sprite)
    {
        Id = id;
        icon.sprite = sprite;
        icon.preserveAspect = true;
    }

    public void SetProgress(int found, int total)
        => countText.text = $"{found}/{total}";
}