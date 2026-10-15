using System.Collections.Generic;
using UnityEngine;

public class HeartManager : MonoBehaviour
{
    [SerializeField] private FactoryManager factory;
    [SerializeField] private RectTransform container;   
    [SerializeField] private RectTransform spawnPoint;  
    [SerializeField] private float gap = 0.1f;          
    [SerializeField] private int maxLives = 3;

    private readonly List<HeartLives> _hearts = new List<HeartLives>();
    private int _currentLives;

    public bool SubstractLives()
    {
        if (_currentLives <= 0) return true;

        for (int i = _hearts.Count - 1; i >= 0; i--)
        {
            if (!_hearts[i].IsAlive) continue;

            _hearts[i].Lose();
            _currentLives--;
            break;
        }

        return _currentLives == 0;
    }

    public void ResetLives()
    {
        ClearHearts();

        for (int i = 0; i < maxLives; i++)
        {
            HeartLives heart = factory.CreateHeart();
            if (heart == null) continue;

            RectTransform rt = (RectTransform)heart.transform;
            rt.SetParent(container, false);
            
            float step = rt.rect.width * rt.localScale.x + gap;

            Vector3 start = container.InverseTransformPoint(spawnPoint.position);
            rt.localPosition = start + Vector3.right * step * i;

            heart.Init();
            _hearts.Add(heart);
        }

        _currentLives = _hearts.Count;
    }

    private void ClearHearts()
    {
        for (int i = _hearts.Count - 1; i >= 0; i--)
        {
            if (_hearts[i] != null && _hearts[i].gameObject.activeInHierarchy)
                factory.Release(_hearts[i].gameObject);
        }
        _hearts.Clear();
    }
}