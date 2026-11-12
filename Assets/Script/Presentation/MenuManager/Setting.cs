using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Setting : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private HintPresenter hintPresenter;
    [SerializeField] private GameObject normalHintObject;
    [SerializeField] private GameObject adsHintObject;
    [SerializeField] private TextMeshProUGUI countHint;  

    private void Awake()
    {
        resumeButton.onClick.AddListener(OnResumeGame);
    }

    private void OnEnable()
    {
        if (hintPresenter != null)
        {
            hintPresenter.OnHintChanged += UpdateHintUI;
            UpdateHintUI(hintPresenter.CountHint);
        }
    }

    private void OnDisable()
    {
        if (hintPresenter != null)
            hintPresenter.OnHintChanged -= UpdateHintUI;
    }

    private void OnDestroy()
    {
        resumeButton.onClick.RemoveListener(OnResumeGame);
    }

    public void OpenSetting()
    {
        StateGame.Instance.PauseGame();
    }

    private void OnResumeGame()
    {
        Debug.Log("OnResumeGame");
        StateGame.Instance.ResumeGame();
    }

    private void UpdateHintUI(int amount)
    {
        bool hasHint = amount > 0;

        if (normalHintObject != null)
            normalHintObject.SetActive(hasHint);

        if (adsHintObject != null)
            adsHintObject.SetActive(!hasHint);
        if (countHint != null && hasHint)
            countHint.text = amount.ToString();
    }
}