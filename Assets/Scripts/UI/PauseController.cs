using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private ScriptManager s;
    private bool paused = false;

    void Awake() => s.PlayerHealth.OnPlayerStateChanged += HandlePause;
    void OnDestroy() => s.PlayerHealth.OnPlayerStateChanged -= HandlePause;

    void Update()
    {
        if (s.PlayerHealth.State == PlayerState.Dead) return;
        if (s.PlayerInput.Pause) HandlePause(PlayerState.Dead);
    }

    private void HandlePause(PlayerState newState)
    {
        if (newState != PlayerState.Dead) return;

        paused = !paused;

        GameManager.Instance.SetState(paused ? GameState.Paused : GameState.Gameplay);
        s.CameraLook.SetCursorState(!paused);
        pauseMenu.SetActive(paused);
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
