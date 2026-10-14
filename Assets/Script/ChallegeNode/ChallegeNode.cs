using UnityEngine;

public class ChallengeNode : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public void Init(Sprite sprite, Vector3 position)
    {
        _spriteRenderer.sprite = sprite;
        transform.position = position;
    }
}