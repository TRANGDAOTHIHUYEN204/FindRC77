using UnityEngine;
using System;

public class Wallet
{
    private readonly string _key;
    public event Action<int> OnChanged;

    public Wallet(string key) => _key = key;

    public int Amount => PlayerPrefs.GetInt(_key, 0);

    public void Add(int n)
    {
        if (n <= 0) return;
        Set(Amount + n);
    }

    public bool TrySpend(int n)
    {
        if (n <= 0 || Amount < n) return false;
        Set(Amount - n);
        return true;
    }

    public void Reset() => Set(0);

    private void Set(int v)
    {
        PlayerPrefs.SetInt(_key, v);
        PlayerPrefs.Save();
        OnChanged?.Invoke(v);
    }
}