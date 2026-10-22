using System;
using System.Collections;
using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private float duration = 0.6f;
    [SerializeField, Range(0.1f, 1f)] private float startScale = 0.5f; 
    private RectTransform _rt;
    private Vector3 _baseScale;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        _baseScale = _rt.localScale;
    }

    public void Fly(Vector3 from, Vector3 to, float delay, Action<Coin> onArrive)
    {
        _rt.position = from;
        _rt.localScale = Vector3.zero;  
        StartCoroutine(Routine(from, to, delay, onArrive));
    }

    private IEnumerator Routine(Vector3 from, Vector3 to, float delay, Action<Coin> onArrive)
    {
        if (delay > 0) yield return new WaitForSecondsRealtime(delay);

        _rt.localScale = _baseScale * startScale;   

        for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / duration)
        {
            float e = 1f - (1f - t) * (1f - t);               
            _rt.position = Vector3.Lerp(from, to, e);
            _rt.localScale = _baseScale * Mathf.Lerp(startScale, 1f, t);   
            yield return null;
        }

        _rt.position = to;
        _rt.localScale = _baseScale;
        onArrive?.Invoke(this);
    }
}