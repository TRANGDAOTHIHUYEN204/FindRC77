using UnityEngine;
using UnityEngine.UI;

public class Setting : MonoBehaviour
{
    [SerializeField] private Button resumeButton;

    private void Awake()
    {
        resumeButton.onClick.AddListener(OnResumeGame);
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
}