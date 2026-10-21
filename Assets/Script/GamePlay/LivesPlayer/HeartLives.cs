using UnityEngine;
using UnityEngine.UI;

public class HeartLives : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Sprite aliveSprite;
    [SerializeField] private Sprite deadSprite;

    public bool IsAlive { get; private set; }

    public void Init()
    {
        IsAlive = true;
        image.sprite = aliveSprite;
    }

    public void Lose()
    {
        IsAlive = false;
        image.sprite = deadSprite;
    }
}