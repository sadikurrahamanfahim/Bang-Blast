using UnityEngine;
using TMPro;
using System.Collections;

public class ComboUI : MonoBehaviour
{
    [SerializeField] private GridLogic gridLogic;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private float showDuration = 1.5f;
    [SerializeField] private float fadeDuration = 0.5f;

    private Coroutine hideRoutine;

    private void Start()
    {
        if (gridLogic != null)
            gridLogic.OnLinesCleared += HandleLinesCleared;

        if (comboText != null)
            comboText.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (gridLogic != null)
            gridLogic.OnLinesCleared -= HandleLinesCleared;
    }

    private void HandleLinesCleared(int cleared, int combo)
    {
        if (combo >= 2)
        {
            comboText.gameObject.SetActive(true);
            comboText.text = $"Combo x{combo}!";

            Color c = comboText.color;
            comboText.color = new Color(c.r, c.g, c.b, 1f);

            if (hideRoutine != null) StopCoroutine(hideRoutine);
            hideRoutine = StartCoroutine(HideAfterDelay());
        }
        else
        {
            comboText.gameObject.SetActive(false);
        }
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(showDuration);

        float t = 0f;
        Color c = comboText.color;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
            comboText.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        comboText.gameObject.SetActive(false);
    }
}
