using UnityEngine;

public class PlayerInputBlocker : MonoBehaviour
{
    [Header("State")]
    [Tooltip("Is input currently blocked for this player.")]
    [SerializeField] private bool inputBlocked;

    public bool IsBlocked
    {
        get
        {
            return inputBlocked;
        }
    }

    public void SetBlocked(bool value)
    {
        inputBlocked = value;
    }

    public void BlockInput()
    {
        inputBlocked = true;
    }

    public void UnblockInput()
    {
        inputBlocked = false;
    }
}