using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Animator))]
public class AnimationManager : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Animator animator;
    

    private void Awake()
    {
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        button.onClick.AddListener(PlayBounce);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(PlayBounce);
    }

    private void PlayBounce()
    {
        if (!isActiveAndEnabled) return;
        animator.Play("ButtonBounce", 0, 0f);
    }
}