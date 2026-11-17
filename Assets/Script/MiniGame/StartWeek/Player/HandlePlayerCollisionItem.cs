using System;
using System.Collections.Generic;
using Char.Haptic;
using UnityEngine;

public class HandlePlayerCollisionItem : MonoBehaviour
{
    [SerializeField] private RectTransform spawnParent;
    [SerializeField] private Transform catchPoint;
    [SerializeField] private float catchHalfWidth = 100f;
    [SerializeField] private float catchHalfHeight = 30f;
    [SerializeField] private AudioClip catchTrue;
    [SerializeField] private AudioClip notCatch;
    [Range(0f, 1f)] [SerializeField] private float catchVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float missVolume = 1f;

    public event Action<Item> Caught;
    public event Action<Item> Missed;

    private readonly List<Item> active = new List<Item>(64);

    public void Register(Item item)
    {
        item.ActiveIndex = active.Count;
        active.Add(item);
    }

    public void Unregister(Item item)
    {
        int i = item.ActiveIndex;
        if (i < 0) return;

        int last = active.Count - 1;
        Item moved = active[last];
        active[i] = moved;
        moved.ActiveIndex = i;
        active.RemoveAt(last);
        item.ActiveIndex = -1;
    }
    public void NotifyMissed(Item item)
    {
        PlaySfx(notCatch, missVolume);
        Haptics.Heavy();
        Missed?.Invoke(item);
    }

    private void LateUpdate()
    {
        if (active.Count == 0) return;

        Vector3 c = spawnParent.InverseTransformPoint(catchPoint.position);
        float minX = c.x - catchHalfWidth, maxX = c.x + catchHalfWidth;
        float minY = c.y - catchHalfHeight, maxY = c.y + catchHalfHeight;

        for (int i = active.Count - 1; i >= 0; i--)
        {
            Item it = active[i];
            float x = it.X, y = it.Y;

            if (x + it.HalfWidth < minX || x - it.HalfWidth > maxX) continue;
            if (y + it.HalfHeight < minY || y - it.HalfHeight > maxY) continue;

            PlaySfx(catchTrue, catchVolume);
            Haptics.Light();
            Caught?.Invoke(it);
            it.ReturnToPool();
        }
    }

    private void PlaySfx(AudioClip clip, float volume)
    {
        if (clip == null || SoundManager.Instance == null) return;
        SoundManager.Instance.PlaySfx(clip, volume);
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnParent == null || catchPoint == null) return;
        Gizmos.color = Color.green;
        Gizmos.matrix = spawnParent.localToWorldMatrix;
        Vector3 c = spawnParent.InverseTransformPoint(catchPoint.position);
        Gizmos.DrawWireCube(c, new Vector3(catchHalfWidth * 2f, catchHalfHeight * 2f, 0f));
    }
}