using UnityEngine;

[CreateAssetMenu(fileName = "FindHiddenData", menuName = "Scriptable Objects/FindHiddenData")]
public class FindHiddenData : ScriptableObject
{
    [SerializeField] private Sprite spriteItem;
    [SerializeField] private Vector3 size;
    [SerializeField] private string targetId;
    public string TargetId => targetId;
    public Sprite SpriteItem => spriteItem;
    public Vector3 Size => size;
}
