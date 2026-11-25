using TMPro;
using UnityEngine;

public class NumberInput : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private int maxValue = 15;

    private void Awake()
    {
        inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        inputField.characterLimit = 2;
        inputField.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnDestroy()
    {
        inputField.onValueChanged.RemoveListener(OnValueChanged);
    }

    private void OnValueChanged(string value)
    {
        if (int.TryParse(value, out int number) && number > maxValue)
            inputField.SetTextWithoutNotify(maxValue.ToString());
    }
    
    public bool TryGetNumber(out int number)
    {
        return int.TryParse(inputField.text, out number);
    }
}