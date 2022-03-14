using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private PlayerManager s;
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
        if (newState == UnitState.Dead)
        {
            pauseMenu.SetActive(true);
            return;
        }

        paused = !paused;

        GameManager.Instance.SetState(paused ? GameState.Paused : GameState.Gameplay);
        pauseMenu.SetActive(paused);
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
