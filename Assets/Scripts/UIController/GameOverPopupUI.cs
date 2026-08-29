using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private GridLogic gridLogic;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private Button playAgainButton;

    private void Awake()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);

        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(RestartGame);
    }

    private void Start()
    {
        if (gridLogic != null)
            gridLogic.OnGameOver += HandleGameOver;
    }

    private void OnDestroy()
    {
        if (gridLogic != null)
            gridLogic.OnGameOver -= HandleGameOver;
    }

    private void HandleGameOver()
    {
        int finalScore = gridLogic != null ? gridLogic.GetScore() : 0;
        Show(finalScore);
        MusicManager.StopMusic();
    }

    public void Show(int score)
    {
        if (popupPanel != null)
            popupPanel.SetActive(true);

        if (scoreText != null)
            scoreText.text = "<size=70>" + score + "</size>";
    }
    
    private void RestartGame()
    {
        MusicManager.PlayMusic();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
