using UnityEngine;
using Fusion;

/// <summary>
/// GameController is responsible for managing the game state and handling player interactions.
/// </summary>
public class GameController: MonoBehaviour
{
    const int MAX_PLAYERS = 2; // Maximum number of players allowed in the game

    /// <summary>
    /// external references to GameLauncher and ItemManager components.
    /// </summary>
    [SerializeField]private GameLauncher gameLauncher;
    [SerializeField]private ItemManager itemManager;
    /// <summary>
    /// GameState enum defines the possible states of the game.
    /// </summary>
    public enum GameState
    {
        WaitingForPlayers,
        InGame,
        GameOver
    }

    private GameState currentGameState  = GameState.WaitingForPlayers;
    public GameState CurrentGameState
    {
        get { return currentGameState; }
        set
        {
            if (currentGameState == value)
            {
                // ��Ԃ��ς��Ȃ��ꍇ�͉������Ȃ�
                return;
            }
            else
            {
                Debug.Log("Game State Changed: " + currentGameState);
                OnChangeState(value);
                currentGameState = value;
            }

        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        gameLauncher.OnJoinedMasterClient += OnJoinedMasterClient;
        gameLauncher.OnJoindClient += OnjoindClient;
    }

    private void OnJoinedMasterClient(NetworkRunner runner)
    {
        // �}�X�^�[�N���C�A���g�ɎQ�������Ƃ��̏����������ɋL�q���܂�
        Debug.Log("CATCH Joined Master Client");
        itemManager.SpawnItem(runner, 0);
    }
    private void OnjoindClient(NetworkRunner runner, PlayerRef player)
    {
        // �N���C�A���g�ɎQ�������Ƃ��̏����������ɋL�q���܂�
        Debug.Log("CATCH Joined Client: " + player);
        if (runner.IsSharedModeMasterClient)
        {
            // �}�X�^�[�N���C�A���g�i�z�X�g�j�̂݃A�C�e�����X�|�[��
            itemManager.SpawnItem(runner, 0);
        }
    }
    private void OnChangeState(GameState newState)
    {
        // ��Ԃ��ς�����Ƃ��̏����������ɋL�q���܂�
        switch (newState)
        {
            case GameState.WaitingForPlayers:
                Debug.Log("Waiting for players to join...");
                break;
            case GameState.InGame:
                Debug.Log("Game is now in progress.");
                break;
            case GameState.GameOver:
                Debug.Log("Game Over!");
                break;
        }
    }
}
