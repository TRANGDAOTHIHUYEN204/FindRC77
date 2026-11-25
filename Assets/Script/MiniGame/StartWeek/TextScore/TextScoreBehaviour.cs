using UnityEngine;
using TMPro;
using DG.Tweening;

public class TextScoreBehaviour : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private float showTime = 0.3f;
    [SerializeField] private float hideTime = 0.3f;
    [SerializeField] private Ease ease = Ease.InOutCirc;

    [Header("Bay lên + phóng to rồi mờ dần")]
    [SerializeField] private float riseDistance = 160f;
    [SerializeField] private float startScale = 0.4f;
    [SerializeField] private float popScale = 1.35f;
    [SerializeField] private float endScale = 1.7f;

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
        rect.localScale = Vector3.one * startScale;
        scoreText.alpha = 1f;

        float rest = duration + hideTime;

        // Nảy to ra + bay lên nhanh, rồi tiếp tục bay lên, to dần và mờ dần.
        seq = DOTween.Sequence();
        seq.Append(rect.DOScale(popScale, showTime).SetEase(Ease.OutBack));
        seq.Join(rect.DOLocalMoveY(pos.y + riseDistance * 0.35f, showTime).SetEase(Ease.OutQuad));
        seq.Append(rect.DOLocalMoveY(pos.y + riseDistance, rest).SetEase(Ease.OutSine));
        seq.Join(rect.DOScale(endScale, rest).SetEase(Ease.OutSine));
        seq.Insert(showTime + duration * 0.5f, scoreText.DOFade(0f, rest - duration * 0.5f).SetEase(Ease.InQuad));
        seq.OnComplete(() => gameObject.SetActive(false));
    }

    private void OnDisable() => seq?.Kill();
    private void OnDestroy() => seq?.Kill();
}