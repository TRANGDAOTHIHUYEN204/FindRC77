using UnityEngine;
using UnityEngine.UI;

public class ChallengeNode : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private RectTransform visual; 
    [SerializeField] private BounceEffect bounceEffect;

    public RectTransform Visual => visual;

    public void Init(Sprite sprite) => ChangeSprite(sprite);
    public void PlayWinBounce() => bounceEffect.Play();
    public void ChangeSprite(Sprite sprite) => image.sprite = sprite;
}