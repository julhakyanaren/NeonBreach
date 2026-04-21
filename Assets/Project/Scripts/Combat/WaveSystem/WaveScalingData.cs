[System.Serializable]
public struct WaveScalingData
{
    public float HealthMultiplier;
    public float DamageMultiplier;
    public float FireRateMultiplier;
    public float ScoreMultiplier;

    public WaveScalingData(
        float healthMultiplier,
        float damageMultiplier,
        float fireRateMultiplier,
        float scoreMultiplier)
    {
        HealthMultiplier = healthMultiplier;
        DamageMultiplier = damageMultiplier;
        FireRateMultiplier = fireRateMultiplier;
        ScoreMultiplier = scoreMultiplier;
    }
}