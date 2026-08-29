using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class SplashScreenLoader : MonoBehaviour
{
    [Header("UI References")]
    public Image loadingFillImage;

    [Header("Settings")]
    public float splashDuration = 3f; // Total time of splash screen

    private void OnEnable()
    {
        loadingFillImage.fillAmount = 0f;
        StartCoroutine(LoadingRoutine());
    }

    IEnumerator LoadingRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < splashDuration)
        {
            elapsedTime += Time.deltaTime;
            loadingFillImage.fillAmount = elapsedTime / splashDuration;
            yield return null;
        }

        loadingFillImage.fillAmount = 1f;

        // Disable splash panel
        gameObject.SetActive(false);
    }
}
