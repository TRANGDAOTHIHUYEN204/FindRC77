using System;
using System.Collections.Generic;
using Char.Haptic;
using UnityEngine;

public class HandlePlayerCollisionItem : MonoBehaviour
{
    [SerializeField] private RectTransform spawnParent;
    [SerializeField] private Transform catchPoint;
    [SerializeField] private float catchHalfWidth = 150f;
    [SerializeField] private float catchHalfHeight = 30f;
    [SerializeField] private AudioClip catchTrue;
    [SerializeField] private AudioClip catchFalse;
    [Range(0f, 1f)] [SerializeField] private float catchVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float falseVolume = 1f;
    [SerializeField] private List<ItemType> catchFalseItems;
    [SerializeField] private FactoryTextScore textFactory;
    [SerializeField] private int maxTextCount = 3;
    [SerializeField] private float textDuration = 0.5f;
    [SerializeField] private float textOffsetX = 0f;
    [SerializeField] private float textOffsetY = 20f;
    public event Action<Item> Caught;
    public event Action<Item> Missed;

    private readonly List<Item> active = new List<Item>(100);
    private readonly List<TextScoreBehaviour> texts = new List<TextScoreBehaviour>();
    private HashSet<ItemType> catchFalseSet;

    private void Awake()
    {
        catchFalseSet = new HashSet<ItemType>(catchFalseItems);
    }

    private void OnDestroy() => ClearTexts();

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

            if (catchFalseSet.Contains(it.Type))
            {
                PlaySfx(catchFalse, falseVolume);
                Haptics.Heavy();
            }
            else
            {
                PlaySfx(catchTrue, catchVolume);
                Haptics.Light();
            }

            Caught?.Invoke(it);
            ShowScoreText(it);
            it.ReturnToPool();
        }
    }

    private void ShowScoreText(Item item)
    {
        if (textFactory == null) return;

        TextScoreBehaviour t = GetText();
        if (t == null) return;

        texts.Remove(t);
        texts.Add(t);

        t.ShowScore(item.Score, new Vector2(item.X + textOffsetX, item.Y + textOffsetY), textDuration);
    }

    private TextScoreBehaviour GetText()
    {
        for (int i = 0; i < texts.Count; i++)
            if (!texts[i].IsPlaying) return texts[i];
        if (texts.Count < maxTextCount)
        {
            TextScoreBehaviour t = textFactory.Create(spawnParent);
            if (t != null) texts.Add(t);
            return t;
        }
        return texts[0];
    }

    public void ClearTexts()
    {
        if (textFactory != null)
            for (int i = texts.Count - 1; i >= 0; i--)
                textFactory.Release(texts[i]);
        texts.Clear();
    }

    private void PlaySfx(AudioClip clip, float volume)
    {
        if (clip is null || SoundManager.Instance is null) return;
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