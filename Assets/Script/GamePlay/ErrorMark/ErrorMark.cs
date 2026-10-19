using System;
using System.Collections;
using UnityEngine;

public class ErrorMark : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private float _scaleDuration = 0.3f;

    private Coroutine _routine;

    public void Init(float size, Sprite sprite, float lifeTime, Action<GameObject> onDone)
    {
        if (_renderer != null) _renderer.sprite = sprite;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(Run(size, lifeTime, onDone));
    }

    private IEnumerator Run(float size, float lifeTime, Action<GameObject> onDone)
    {
        float t = 0f;
        transform.localScale = Vector3.zero;
        while (t < _scaleDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / _scaleDuration);
            k = 1f - (1f - k) * (1f - k);
            transform.localScale = Vector3.one * (size * k);
            yield return null;
        }
        transform.localScale = Vector3.one * size;
        
        float remain = Mathf.Max(0f, lifeTime - _scaleDuration);
        yield return new WaitForSeconds(remain);

        _routine = null;
        onDone?.Invoke(gameObject);
    }

    private void OnDisable()
    {
        _routine = null;
    }
}