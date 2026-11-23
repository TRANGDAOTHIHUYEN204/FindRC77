using UnityEngine;

[CreateAssetMenu(fileName = "RewardWin", menuName = "Scriptable Objects/RewardWin")]
public class RewardWin : ScriptableObject
{
    [SerializeField] private int rewardCoin;
    [SerializeField] private int rewardHint;

    public int RewardCoin => rewardCoin;
    public int RewardHint => rewardHint;
}
