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
        if (s.PlayerHealth.State == UnitState.Dead) return;
        if (s.PlayerInput.Pause) HandlePause(UnitState.Alive);
    }

    private void HandlePause(UnitState newState)
    {
        paused = newState == UnitState.Dead || !paused;

        GameManager.Instance.SetState(paused ? GameState.Paused : GameState.Gameplay);
        s.CameraLook.SetCursorState(!paused);
        pauseMenu.SetActive(paused);
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
