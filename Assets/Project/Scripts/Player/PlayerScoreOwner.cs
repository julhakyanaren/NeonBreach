using UnityEngine;

public class PlayerScoreOwner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the score manager used by this player.")]
    [SerializeField] private ScoreManager scoreManager;

    public ScoreManager ScoreManager
    {
        get
        {
            return scoreManager;
        }
    }

    private void Awake()
    {
        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }
    }

    public void AddScore(float amount)
    {
        if (scoreManager == null)
        {
            return;
        }

        scoreManager.AddScore(amount);
    }
}