using UnityEngine;

public class JaggernautAnimationEvents : MonoBehaviour
{
    [Header("SFX Controller")]
    [SerializeField] private WwiseEnemySFXController enemySfxController;

    [Header("Source")]
    [SerializeField] private GameObject sfxSource;

    private void Awake()
    {
        if (enemySfxController == null)
        {
            enemySfxController = GetComponentInParent<WwiseEnemySFXController>();
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject;
        }
    }

    public void PlayAttack()
    {
        Debug.Log("Jaggernaut PlayAttack event fired", this);

        if (enemySfxController == null)
        {
            Debug.LogWarning("enemySfxController is null", this);
            return;
        }

        enemySfxController.Post(
            EnemySfxType.JaggernautAttack,
            WwiseEventsType.Play,
            sfxSource);
    }

    public void StopAttack()
    {
        if (enemySfxController == null)
        {
            return;
        }

        enemySfxController.Post(
            EnemySfxType.JaggernautAttack,
            WwiseEventsType.Stop,
            sfxSource);
    }
}