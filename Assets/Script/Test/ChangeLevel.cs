using UnityEngine;

public class ChangeLevel : MonoBehaviour
{
    [SerializeField] private NumberInput numberInput;
    [SerializeField] private MenuHome menuHome;
    [SerializeField] private ButtonHomeManager buttonHomeManager;

    private void Start()
    {
        if (buttonHomeManager.LevelCount > 0)
            numberInput.SetMaxValue(buttonHomeManager.LevelCount);
    }

    public void OnGoClicked()
    {
        if (!numberInput.TryGetNumber(out int number)) return;

        int maxLevel = Mathf.Max(buttonHomeManager.LevelCount, 1);
        SaveGame.CurrentLevel = Mathf.Clamp(number, 1, maxLevel) - 1;
        SaveGame.TutorialCompleted = true;

        menuHome.RefreshLevelText();
        buttonHomeManager.Refresh();
    }
}
