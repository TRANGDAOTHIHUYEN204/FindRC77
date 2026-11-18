using TMPro;
using UnityEngine;

public class CleanUpProcess : MonoBehaviour
{
    [SerializeField] private CompletionCounter counter;
    [SerializeField] private ProcessBar processBar;
    [SerializeField] private TMP_Text countText;  

    private void OnEnable()
    {
        counter.OnChanged += Refresh;
        Refresh(counter.Completed, counter.Total);   
    }

    private void OnDisable()
    {
        counter.OnChanged -= Refresh;
    }

    private void Refresh(int done, int total)
    {
        processBar.SetProgress(done, total, animate: done > 0);

        if (countText != null)
            countText.text = $"{done}/{total}";
    }
}