using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private float startTime = 30f;

    [Header("UI Reference")]
    [SerializeField] private TMP_Text timerText;
    
    [SerializeField] private TMP_Text time_upText;
    
    [SerializeField] private AudioClip gameOverSfx;

    private float currentTime;
    private bool isRunning = false;
    
    [SerializeField] private Image redBlinkImage;
    [SerializeField] private float fadeSpeed = 4f;

    private float blinkTimer;
    private bool isBlinkOn;

    [SerializeField] private GridLogic gridLogic;

    private void Start()
    {
        currentTime = startTime;
        UpdateTimerUI();
        StartTimer();
        time_upText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isRunning) return;

        currentTime -= Time.deltaTime;

        if (currentTime <= 0f)
        {
            currentTime = 0f;
            isRunning = false;
            UpdateTimerUI();
            OnTimeOver();
            return;
        }

        UpdateTimerUI();
        HandleRedBlink();
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        timerText.text = $"{minutes}:{seconds:D2}";
    }


    public void StartTimer()
    {
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer(float newTime)
    {
        startTime = newTime;
        currentTime = startTime;
        UpdateTimerUI();
    }

    private void OnTimeOver()
    {
        time_upText.gameObject.SetActive(true);
        MusicManager.PlaySFX(gameOverSfx);
        
        time_upText.transform.localScale = Vector3.zero;

        StartCoroutine(PopupAndEnd());
    }

    private IEnumerator PopupAndEnd()
    {
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            time_upText.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t / 0.2f);
            yield return null;
        }

        yield return new WaitForSeconds(2f);
        time_upText.gameObject.SetActive(false);
        gridLogic.OnGameOver?.Invoke();
    }
    
    private void HandleRedBlink()
    {
        if (currentTime > 10f)
        {
            var c = redBlinkImage.color;
            c.a = 0f;
            redBlinkImage.color = c;
            return;
        }

        float alpha = Mathf.PingPong(Time.time * fadeSpeed, 1f);
        var color = redBlinkImage.color;
        color.a = alpha;
        redBlinkImage.color = color;
    }
}