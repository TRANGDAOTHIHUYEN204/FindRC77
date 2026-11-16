using UnityEngine;

public enum ItemType
{
    Heart,
    Smile,
    Like,
    Money,
    Angry,
    Dislike
}

[System.Serializable]
public class ItemConfig
{
    public ItemType type;
    public Sprite sprite;
    public int score;
}