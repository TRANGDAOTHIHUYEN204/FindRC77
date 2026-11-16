using System;
using UnityEngine;

public class ItemMoveMent : MonoBehaviour
{
    public static bool Paused;

    [SerializeField] private float speed = 300f;

    private float currentSpeed;
    private float killY;
    private bool isMoving;
    private Action onReachedBottom;
    public float X { get; private set; }
    public float Y { get; private set; }

    public void Init(float killY, Action onReachedBottom, float speedOverride = -1f)
    {
        this.killY = killY;
        this.onReachedBottom = onReachedBottom;
        currentSpeed = speedOverride > 0f ? speedOverride : speed;

        Vector3 p = transform.localPosition;
        X = p.x; Y = p.y;
        isMoving = true;
    }

    public void Stop()
    {
        isMoving = false;
        onReachedBottom = null;
    }

    private void Update()
    {
        if (!isMoving || Paused) return;

        Vector3 pos = transform.localPosition;
        pos.y -= currentSpeed * Time.deltaTime;
        transform.localPosition = pos;
        X = pos.x; Y = pos.y;

        if (pos.y <= killY)
        {
            isMoving = false;
            var callback = onReachedBottom;
            onReachedBottom = null;
            callback?.Invoke();
        }
    }
}