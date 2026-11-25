using TMPro;
using UnityEngine;

public class NumberInput : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private int maxValue = 15;

    private void Awake()
    {
        inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        ApplyCharacterLimit();
        inputField.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnDestroy()
    {
        inputField.onValueChanged.RemoveListener(OnValueChanged);
    }

    public void SetMaxValue(int value)
    {
        maxValue = Mathf.Max(1, value);
        ApplyCharacterLimit();
        OnValueChanged(inputField.text);
    }

    private void ApplyCharacterLimit()
    {
        inputField.characterLimit = maxValue.ToString().Length;
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
