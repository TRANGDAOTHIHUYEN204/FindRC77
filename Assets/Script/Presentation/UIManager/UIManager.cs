using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private LevelManager leveManager;

    public void NextLevel()
    {
        leveManager.LoadNextLevel();
    }

    public void Replay()
    {
        leveManager.ReloadLevel();
    }
}
