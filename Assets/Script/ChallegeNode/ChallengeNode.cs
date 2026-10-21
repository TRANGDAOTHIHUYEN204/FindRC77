using UnityEngine;

public class ChallengeNode : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private BounceEffect bounceEffect;
    public void Init(Sprite sprite)
    {
        ChangeSprite(sprite);

    }
    public void PlayWinBounce()
    {
        bounceEffect.Play();
    }
    public void ChangeSprite(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
    }
}