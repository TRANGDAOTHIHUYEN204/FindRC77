using UnityEngine;

public class ChangeLevel : MonoBehaviour
{
    [SerializeField] private NumberInput numberInput;
    [SerializeField] private MenuHome menuHome;
    [SerializeField] private ButtonHomeManager buttonHomeManager;

    public void OnGoClicked()
    {
        if (!numberInput.TryGetNumber(out int number)) return;

        SaveGame.CurrentLevel = Mathf.Max(number - 1, 0);
        SaveGame.TutorialCompleted = true;

        menuHome.RefreshLevelText();     
        buttonHomeManager.Refresh(); 
    }
}