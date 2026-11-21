using UnityEngine;
using TMPro;
using DG.Tweening;

public class TextScoreBehaviour : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private float showTime = 0.3f;
    [SerializeField] private float hideTime = 0.3f;
    [SerializeField] private Ease ease = Ease.InOutCirc;

    private RectTransform rect;
    private Sequence seq;

    public bool IsPlaying => gameObject.activeSelf;

    public void Init()
    {
        rect = (RectTransform)transform;
        if (scoreText == null) scoreText = GetComponent<TMP_Text>();
        gameObject.SetActive(false);
    }

    public void ShowScore(int score, Vector2 pos, float duration)
    {
        seq?.Kill();

        gameObject.SetActive(true);
        scoreText.text = score > 0 ? $"+{score}" : score.ToString();
        rect.localPosition = new Vector3(pos.x, pos.y, 0f);
        rect.localScale = Vector3.zero;
        scoreText.alpha = 0f;

        seq = DOTween.Sequence();
        seq.Append(rect.DOScale(1f, showTime).SetEase(ease));
        seq.Join(scoreText.DOFade(1f, showTime));
        seq.AppendInterval(duration);
        seq.Append(rect.DOScale(0f, hideTime).SetEase(ease));
        seq.Join(scoreText.DOFade(0f, hideTime));
        seq.OnComplete(() => gameObject.SetActive(false));
    }

    private void OnDisable() => seq?.Kill();
    private void OnDestroy() => seq?.Kill();
}