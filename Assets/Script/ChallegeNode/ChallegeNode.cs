using UnityEngine;

public class ChallengeNode : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public void Init(Sprite sprite)
    {
        _spriteRenderer.sprite = sprite;
 
    }
}