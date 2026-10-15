using UnityEngine;
using UnityEngine.UI;

public class HeartLives : MonoBehaviour
{
    [SerializeField] private Image image;

    public bool IsAlive { get; private set; }

    public void Init()
    {
        IsAlive = true;
        image.enabled = true;
    }

    public void Lose()
    {
        IsAlive = false;
        image.enabled = false;
    }
}