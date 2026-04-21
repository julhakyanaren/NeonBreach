using UnityEngine;
using UnityEngine.SceneManagement;

public class GameRestartController : MonoBehaviour
{
    [ContextMenu("Reset Scene")]
    private void ResetScene()
    {
        RestartCurrentRun();
    }

    private bool isRestartInProgress;

    public void RestartCurrentRun()
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