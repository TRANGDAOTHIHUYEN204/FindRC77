using UnityEngine;
using System;

public class Wallet
{
    [Serializable]
    private class WalletState
    {
        public int amount;
        public int pending;
    }

    private readonly string _key;
    private readonly string _pendingKey;
    private readonly string _stateKey;
    public event Action<int> OnChanged;

    public Wallet(string key)
    {
        _key = key;
        _pendingKey = $"pending_{key}";
        _stateKey = $"wallet_state_{key}";
    }

    public int Amount => LoadState().amount;
    public int PendingAmount => LoadState().pending;

    public void Add(int n)
    {
        if (n <= 0) return;

        var state = LoadState();
        state.amount += n;
        SaveState(state);
        OnChanged?.Invoke(state.amount);
    }

    public bool TrySpend(int n)
    {
        if (n <= 0) return false;

        var state = LoadState();
        if (state.amount < n) return false;

        state.amount -= n;
        SaveState(state);
        OnChanged?.Invoke(state.amount);
        return true;
    }
    
    public void AddPending(int n)
    {
        if (n <= 0) return;

        var state = LoadState();
        state.pending += n;
        SaveState(state);
    }

    public void ClearPending()
    {
        var state = LoadState();
        state.pending = 0;
        SaveState(state);
    }

    public int ClaimPending(int n)
    {
        if (n <= 0) return 0;

        var state = LoadState();
        int claimed = Math.Min(n, state.pending);
        if (claimed <= 0) return 0;

        state.pending -= claimed;
        state.amount += claimed;
        SaveState(state);
        OnChanged?.Invoke(state.amount);
        return claimed;
    }

    private WalletState LoadState()
    {
        if (PlayerPrefs.HasKey(_stateKey))
        {
            var state = JsonUtility.FromJson<WalletState>(PlayerPrefs.GetString(_stateKey));
            if (state == null)
                throw new InvalidOperationException($"Could not load wallet state '{_stateKey}'.");

            return state;
        }

        var migrated = new WalletState
        {
            amount = PlayerPrefs.GetInt(_key, 0),
            pending = PlayerPrefs.GetInt(_pendingKey, 0)
        };
        SaveState(migrated);
        PlayerPrefs.DeleteKey(_key);
        PlayerPrefs.DeleteKey(_pendingKey);
        PlayerPrefs.Save();
        return migrated;
    }

    private void SaveState(WalletState state)
    {
        PlayerPrefs.SetString(_stateKey, JsonUtility.ToJson(state));
        PlayerPrefs.Save();
    }
    public void Reset()
    {
        SaveState(new WalletState());  
        OnChanged?.Invoke(0);
    }
}