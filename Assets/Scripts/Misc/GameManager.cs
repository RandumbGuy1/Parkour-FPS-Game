using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentGameState { get; private set; }

    public delegate void GameStateChangeHandler(GameState newGameState);
    public event GameStateChangeHandler OnGameStateChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Instance.SetState(GameState.Gameplay);
            DontDestroyOnLoad(gameObject);
            return;
        }

        Instance.SetState(GameState.Gameplay);
        Destroy(gameObject);
    }

    public void SetState(GameState newGameState)
    {
        if (newGameState == CurrentGameState) return;

        CurrentGameState = newGameState;
        OnGameStateChanged?.Invoke(newGameState);

        HandleFreezing(CurrentGameState != GameState.Gameplay);
    }

    private void HandleFreezing(bool pause)
    {
        foreach (ParticleSystem particle in FindObjectsOfType<ParticleSystem>())
        {
            if (!particle.gameObject.activeInHierarchy) continue;

            if (pause)
            {
                particle.Pause();
                continue;
            }

            if (particle.isPaused) particle.Play();
        }
    }
}

public enum GameState
{
    Gameplay,
    Paused
}
