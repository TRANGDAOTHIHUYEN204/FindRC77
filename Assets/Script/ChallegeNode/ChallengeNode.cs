using UnityEngine;
using UnityEngine.UI;
using Coffee.UIEffects;

public class ChallengeNode : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private RectTransform visual;
    [SerializeField] private BounceEffect bounceEffect;
    [SerializeField] private UIEffectTweener shinyTweener;

    public RectTransform Visual => visual;

    public void Init(Sprite sprite) => ChangeSprite(sprite);
    public void PlayWinBounce() => bounceEffect.Play();
    public void ChangeSprite(Sprite sprite) => image.sprite = sprite;
    public void ResetVisual(Sprite defaultSprite) => ChangeSprite(defaultSprite);
    
    public void ChangeSpriteWithFx(Sprite sprite)
    {
        ChangeSprite(sprite);

        if (bounceEffect != null)
            bounceEffect.Play();

        if (shinyTweener != null)
            shinyTweener.PlayForward();
    }
}