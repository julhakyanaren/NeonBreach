using System.Collections;
using UnityEngine;

public class SpawnPlatformController : MonoBehaviour
{
    [Header("Spawn Point")]
    [Tooltip("Exact transform used as player spawn point for this platform.")]
    [SerializeField] private Transform spawnPoint;

    [Header("Animation")]
    [Tooltip("Chamber animator.")]
    [SerializeField] private Animator spawnerAnimator;

    [Tooltip("Spawner deactivate trigger parameter name.")]
    [SerializeField] private string deactivateParameter = "Deactivate";

    [Tooltip("Spawner speed float parameter name.")]
    [SerializeField] private string deactivateSpeedParameter = "SpawnerSpeed";

    [Tooltip("Animation playback speed during platform deactivation.")]
    [Range(0.1f, 3f)]
    [SerializeField] private float deactivateSpeedValue = 1f;

    [Tooltip("Delay before platform deactivation animation starts.")]
    [Range(0f, 10f)]
    [SerializeField] private float deactivationDelay = 5f;

    [Tooltip("Base duration of deactivation animation.")]
    [Range(0.1f, 30f)]
    [SerializeField] private float deactivationBaseDuration = 5f;

    [Header("Particle System")]
    [Tooltip("Circle particle system.")]
    [SerializeField] private ParticleSystem circleParticleSystem;

    [Tooltip("Main particle system.")]
    [SerializeField] private ParticleSystem mainParticleSystem;

    private bool initialized;
    private Coroutine deactivateRoutine;

    public bool Deactivated { get; private set; }

    public Transform SpawnPoint
    {
        get
        {
            return spawnPoint;
        }
    }

    private void Awake()
    {
        initialized = ValidateReferences();
    }

    private bool ValidateReferences()
    {
        if (spawnPoint == null)
        {
            Debug.LogError("SpawnPlatformController: spawnPoint is null.", this);
            return false;
        }

        if (spawnerAnimator == null)
        {
            Debug.LogError("SpawnPlatformController: spawnerAnimator is null.", this);
            return false;
        }

        if (circleParticleSystem == null)
        {
            Debug.LogError("SpawnPlatformController: circleParticleSystem is null.", this);
            return false;
        }

        if (mainParticleSystem == null)
        {
            Debug.LogError("SpawnPlatformController: mainParticleSystem is null.", this);
            return false;
        }

        if (string.IsNullOrEmpty(deactivateParameter))
        {
            Debug.LogError("SpawnPlatformController: deactivateParameter is null or empty.", this);
            return false;
        }

        if (string.IsNullOrEmpty(deactivateSpeedParameter))
        {
            Debug.LogError("SpawnPlatformController: deactivateSpeedParameter is null or empty.", this);
            return false;
        }

        if (deactivateSpeedValue <= 0f)
        {
            Debug.LogWarning("SpawnPlatformController: deactivateSpeedValue is invalid. Value changed to 1.", this);
            deactivateSpeedValue = 1f;
        }

        if (deactivationBaseDuration <= 0f)
        {
            Debug.LogWarning("SpawnPlatformController: deactivationBaseDuration is invalid. Value changed to 1.", this);
            deactivationBaseDuration = 1f;
        }

        return true;
    }

    public bool CanBeUsedForSpawn()
    {
        if (!initialized)
        {
            Debug.LogWarning("SpawnPlatformController: Platform is not initialized.", this);
            return false;
        }

        if (Deactivated)
        {
            Debug.LogWarning("SpawnPlatformController: Platform is already deactivated.", this);
            return false;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("SpawnPlatformController: spawnPoint is null.", this);
            return false;
        }

        return true;
    }

    public void DeactivatePlatform()
    {
        if (!initialized)
        {
            return;
        }

        if (Deactivated)
        {
            return;
        }

        Deactivated = true;

        if (deactivateRoutine != null)
        {
            StopCoroutine(deactivateRoutine);
        }

        deactivateRoutine = StartCoroutine(DeactivatePlatformRoutine());
    }

    public void DisableCircleParticleSystem()
    {
        if (circleParticleSystem != null)
        {
            circleParticleSystem.Stop();
        }
    }

    public void DisableMainParticleSystem()
    {
        if (mainParticleSystem != null)
        {
            mainParticleSystem.Stop();
        }
    }

    public void ResetPlatformState()
    {
        Deactivated = false;
    }

    private IEnumerator DeactivatePlatformRoutine()
    {
        yield return new WaitForSecondsRealtime(deactivationDelay);

        if (spawnerAnimator != null)
        {
            spawnerAnimator.SetFloat(deactivateSpeedParameter, deactivateSpeedValue);
            spawnerAnimator.SetTrigger(deactivateParameter);
        }

        float animationDuration = deactivationBaseDuration / deactivateSpeedValue;
        yield return new WaitForSecondsRealtime(animationDuration);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        deactivateRoutine = null;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        deactivateRoutine = null;
    }
}