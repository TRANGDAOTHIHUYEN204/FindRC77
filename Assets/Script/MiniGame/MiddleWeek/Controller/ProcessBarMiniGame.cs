using UnityEngine;

public class ProcessBarMiniGame : MonoBehaviour
{
    [SerializeField] private MiniGameManager gameManager;
    [SerializeField] private ProcessBar bar;
    
    private void OnEnable()
    {
        if (gameManager == null || bar == null) return;

        gameManager.OnProgress += HandleProgress;

        var state = gameManager.State;
        bar.SetProgress(state.FoundCount, state.Total, false);
    }

    private void OnDisable()
    {
        if (gameManager != null)
            gameManager.OnProgress -= HandleProgress;
    }

    private void HandleProgress(int found, int total) => bar.SetProgress(found, total, true);
}

