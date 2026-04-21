using System;
using UnityEngine;

public class EnemyDeathNotifier : MonoBehaviour
{
    public event Action<EnemyDeathNotifier> Died;

    [Header("Runtime")]
    [Tooltip("Prevents duplicate death notifications.")]
    [SerializeField] private bool hasNotifiedDeath;

    public void NotifyDeath()
    {
        if (hasNotifiedDeath)
        {
            return;
        }

        hasNotifiedDeath = true;

        if (Died != null)
        {
            Died.Invoke(this);
        }
    }

    public void ResetState()
    {
        hasNotifiedDeath = false;
    }
}