using System.Collections;
using UnityEngine;

public interface IDrawShape
{
    IEnumerator Draw(Vector2 center, Vector2 size, float lineWidth, Color color, int segments, float duration);
}