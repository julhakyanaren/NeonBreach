using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("Score Settings")]
    [Tooltip("If enabled, score display value will be rounded down from the internal float value.")]
    [SerializeField] private bool useFloorRounding = true;

    [Header("Debug")]
    [Tooltip("Logs score changes to the console for debugging.")]
    [SerializeField] private bool enableDebugLogs = false;

    [Tooltip("Amount used for custom debug add.")]
    [SerializeField] private float debugAddAmount = 100f;

    [ContextMenu("Add Custom Score")]
    private void DebugAddCustomScore()
    {
        AddScore(debugAddAmount);
    }

    private float currentScoreRaw;
    private int currentScore;

    public event Action<int> OnScoreChanged;

    public int CurrentScore
    {
        get
        {
            return currentScore;
        }
    }

    public float CurrentScoreRaw
    {
        get
        {
            return currentScoreRaw;
        }
    }

    public void ResetScore()
    {
        currentScoreRaw = 0f;
        currentScore = 0;

        if (enableDebugLogs)
        {
            Debug.Log("ScoreManager: Score reset.", this);
        }

        OnScoreChanged?.Invoke(currentScore);

        GameSessionStats stats = GameSessionStats.Instance;

        if (stats != null)
        {
            stats.ResetScore();
        }
    }

    public void AddScore(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentScoreRaw += amount;

        int newScore = CalculateDisplayScore(currentScoreRaw);

        if (newScore == currentScore)
        {
            return;
        }

        currentScore = newScore;

        if (enableDebugLogs)
        {
            Debug.Log("ScoreManager: Score changed to " + currentScore, this);
        }

        OnScoreChanged?.Invoke(currentScore);

        GameSessionStats stats = GameSessionStats.Instance;

        if (stats != null)
        {
            stats.AddScore(amount);
        }
    }

    private int CalculateDisplayScore(float rawScore)
    {
        if (useFloorRounding)
        {
            return Mathf.FloorToInt(rawScore);
        }

        return Mathf.RoundToInt(rawScore);
    }
}