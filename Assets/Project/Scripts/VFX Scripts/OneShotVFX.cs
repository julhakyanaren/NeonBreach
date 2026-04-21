using UnityEngine;

public class OneShotVFX : MonoBehaviour
{
    [Header("Particle Systems")]
    [Tooltip("All particle systems that belong to this VFX object.")]
    [SerializeField] private ParticleSystem[] particleSystems;

    [Header("Destroy Settings")]
    [Tooltip("Additional delay before destroying the VFX object.")]
    [SerializeField] private float destroyDelay = 0.05f;

    private float lifeTimer;

    private void Reset()
    {
        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void Awake()
    {
        if (particleSystems == null || particleSystems.Length == 0)
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }
    }

    private void OnEnable()
    {
        if (particleSystems == null || particleSystems.Length == 0)
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        StopAndClearAll();
        PlayAll();
        lifeTimer = GetLongestDuration() + destroyDelay;
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;

        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void StopAndClearAll()
    {
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem currentSystem = particleSystems[i];

            if (currentSystem == null)
            {
                continue;
            }

            currentSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void PlayAll()
    {
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem currentSystem = particleSystems[i];

            if (currentSystem == null)
            {
                continue;
            }

            currentSystem.Play(true);
        }
    }

    private float GetLongestDuration()
    {
        float longestDuration = 0f;

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem currentSystem = particleSystems[i];

            if (currentSystem == null)
            {
                continue;
            }

            ParticleSystem.MainModule mainModule = currentSystem.main;
            float currentDuration = mainModule.duration;

            if (mainModule.startLifetime.mode == ParticleSystemCurveMode.Constant)
            {
                currentDuration += mainModule.startLifetime.constant;
            }
            else if (mainModule.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
            {
                currentDuration += mainModule.startLifetime.constantMax;
            }
            else
            {
                currentDuration += 1f;
            }

            if (currentDuration > longestDuration)
            {
                longestDuration = currentDuration;
            }
        }

        return longestDuration;
    }
}