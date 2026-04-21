using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class АchievmentsScreenController : MonoBehaviour
{
    [Header("Gameplay Stats Text")]
    [Tooltip("Text field for total kills.")]
    [SerializeField] private TMP_Text totalKillsText;

    [Tooltip("Text field for shots fired.")]
    [SerializeField] private TMP_Text shotsFiredText;

    [Tooltip("Text field for shots hit.")]
    [SerializeField] private TMP_Text shotsHitText;

    [Tooltip("Text field for accuracy percent.")]
    [SerializeField] private TMP_Text accuracyText;

    [Tooltip("Text field for survived waves.")]
    [SerializeField] private TMP_Text wavesSurvivedText;

    [Tooltip("Text field for score.")]
    [SerializeField] private TMP_Text scoreSurvivedText;

    [Tooltip("Text field for survived time.")]
    [SerializeField] private TMP_Text timeSurvivedText;

    [Tooltip("Text field for score time.")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Pickups collected Stats Text")]
    [Tooltip("Text field for collected health pickups.")]
    [SerializeField] private TMP_Text healthPickupsCollectedText;

    [Tooltip("Text field for collected damage pickups.")]
    [SerializeField] private TMP_Text damagePickupsCollectedText;

    [Tooltip("Text field for collected defense pickups.")]
    [SerializeField] private TMP_Text defensePickupsCollectedText;

    [Tooltip("Text field for collected fire rate pickups.")]
    [SerializeField] private TMP_Text fireRatePickupsCollectedText;

    [Tooltip("Text field for collected speed pickups.")]
    [SerializeField] private TMP_Text speedPickupsCollectedText;

    private void UpdateStatsView()
    {
        if (GameSessionStats.Instance == null)
        {
            Debug.LogWarning("DeathScreenController: GameSessionStats instance is missing.", this);
            return;
        }

        GameSessionStats stats = GameSessionStats.Instance;

        if (totalKillsText != null)
        {
            totalKillsText.text = stats.TotalKills.ToString();
        }

        if (shotsFiredText != null)
        {
            shotsFiredText.text = stats.ShotsFired.ToString();
        }

        if (shotsHitText != null)
        {
            shotsHitText.text = stats.ShotsHit.ToString();
        }

        if (accuracyText != null)
        {
            accuracyText.text = stats.GetAccuracy().ToString("F1") + "%";
        }

        if (wavesSurvivedText != null)
        {
            wavesSurvivedText.text = stats.WavesSurvived.ToString();
        }

        if (healthPickupsCollectedText != null)
        {
            healthPickupsCollectedText.text = stats.HealthPickupsCollected.ToString();
        }

        if (damagePickupsCollectedText != null)
        {
            damagePickupsCollectedText.text = stats.DamagePickupsCollected.ToString();
        }

        if (defensePickupsCollectedText != null)
        {
            defensePickupsCollectedText.text = stats.DefensePickupsCollected.ToString();
        }

        if (fireRatePickupsCollectedText != null)
        {
            fireRatePickupsCollectedText.text = stats.FireRatePickupsCollected.ToString();
        }

        if (speedPickupsCollectedText != null)
        {
            speedPickupsCollectedText.text = stats.SpeedPickupsCollected.ToString();
        }

        if (scoreText != null)
        {
            scoreText.text = Mathf.FloorToInt(stats.Score).ToString();
        }

        if (timeSurvivedText != null)
        {
            int survivedSeconds = Mathf.FloorToInt(stats.TimeSurvived);
            int minutes = survivedSeconds / 60;
            int seconds = survivedSeconds % 60;

            timeSurvivedText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
        }
    }
    private void OnEnable()
    {
        UpdateStatsView();
    }
}
