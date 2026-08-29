using UnityEngine;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class ScorePanelController : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GridLogic gridLogic;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private bool logDebug = false;
    [SerializeField] private float padding = 100f;
    [SerializeField] private float scoreOffsetY = 300f;
    
    [SerializeField] private bool controlTextLayout = false;

    private RectTransform rt;
    private int highScore;

    void Awake()
    {
        rt = GetComponent<RectTransform>();

        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = Vector2.zero;

        highScore = PlayerPrefs.GetInt("HighScore", 0);

        if (highScoreText != null)
        {
            RectTransform hrt = highScoreText.GetComponent<RectTransform>();
            hrt.anchorMin = hrt.anchorMax = new Vector2(0, 1);
            hrt.pivot = new Vector2(0, 1);
            hrt.anchoredPosition = new Vector2(padding, -padding);
        }

        if (controlTextLayout && scoreText != null)
        {
            RectTransform srt = scoreText.GetComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 1);
            srt.pivot = new Vector2(0.5f, 1);
            srt.anchoredPosition = new Vector2(0, -(padding + scoreOffsetY));
        }
    }

    private void Start()
    {
        if (gridManager != null)
        {
            gridManager.OnGridResized += ResizePanel;
            ResizePanel();
        }
        else
        {
            Debug.LogWarning("[ScorePanelController] GridManager not assigned.");
        }

        if (gridLogic != null)
        {
            gridLogic.OnScoreChanged += UpdateScore;
            UpdateScore(gridLogic.GetScore());
        }
        else
        {
            Debug.LogWarning("[ScorePanelController] GridLogic not assigned.");
            UpdateScore(0);
        }
    }

    private void OnDestroy()
    {
        if (gridManager != null)
            gridManager.OnGridResized -= ResizePanel;
        if (gridLogic != null)
            gridLogic.OnScoreChanged -= UpdateScore;
    }

    void OnRectTransformDimensionsChange()
    {
        if (Application.isPlaying) ResizePanel();
    }

    private void ResizePanel()
    {
        if (rt == null || rt.parent == null || gridManager == null) return;

        RectTransform parent = rt.parent as RectTransform;

        float panelHeight = gridManager.GetBoardTopLocal(parent);
        if (panelHeight < 0) panelHeight = 0;

        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, panelHeight);

        if (logDebug)
        {
            Debug.Log($"[ScorePanel] parentHeight={parent.rect.height}, panelHeight={panelHeight}, rtHeight={rt.rect.height}");
        }
    }

    private void UpdateScore(int score)
    {
        if (scoreText != null) scoreText.text = $"{score}";

        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }

        if (highScoreText != null) highScoreText.text = $"High Score: {highScore}";
    }
}
