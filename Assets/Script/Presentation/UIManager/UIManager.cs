using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private LevelManager leveManager;
    [SerializeField] private HintPresenter hintPresenter;
    public void NextLevel()
    {
        leveManager.LoadNextLevel();
    }

    public void Replay()
    {
        leveManager.ReloadLevel();
    }

    public void Hint()
    {
        hintPresenter.ShowHint();
    }
}
