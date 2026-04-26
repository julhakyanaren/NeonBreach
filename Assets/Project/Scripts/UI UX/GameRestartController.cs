using UnityEngine;
using UnityEngine.SceneManagement;

public class GameRestartController : MonoBehaviour
{

    private bool isRestartInProgress;

    public void RespawnPlayerMP()
    {
        if (RuntimeOptions.MultiplayerMode)
        {
            PlayerMultiplayerRespawnController.RespawnLocalPlayerFromUI(isAlive: false);
            return;
        }
        else
        {
            ResetPlayerSP();
        }
    }
    public void ResetPlayerMP()
    {
        if (RuntimeOptions.MultiplayerMode)
        {
            PlayerMultiplayerRespawnController.RespawnLocalPlayerFromUI(isAlive: true);
            return;
        }
        else
        {
            ResetPlayerSP();
        }
    }

    private void ResetPlayerSP()
    {
        if (isRestartInProgress)
        {
            return;
        }
        isRestartInProgress = true;

        Time.timeScale = 1f;
        RuntimeOptions.InputBlocked = false;

        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }
}