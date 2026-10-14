using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public bool ClickedThisFrame
    {
        
        get
        {
            Pointer pointer = Pointer.current;
            if (pointer == null) return false;

            return pointer.press.wasPressedThisFrame;
        }
    }
    
    public Vector2 ClickScreenPosition
    {
        get
        {
            Pointer pointer = Pointer.current;
            if (pointer == null) return Vector2.zero;

            return pointer.position.ReadValue();
        }
    }
}