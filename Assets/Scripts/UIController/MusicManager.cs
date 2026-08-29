using UnityEngine;

public class MusicManager : MonoBehaviour
{
    private static MusicManager instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Safety fallback
            if (musicSource == null)
                musicSource = GetComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 🎵 Background Music
    public static void PlayMusic()
    {
        if (instance != null && instance.musicSource != null && !instance.musicSource.isPlaying)
        {
            instance.musicSource.Play();
        }
    }

    public static void StopMusic()
    {
        if (instance != null && instance.musicSource != null && instance.musicSource.isPlaying)
        {
            instance.musicSource.Stop();
        }
    }

    // 🔊 SFX (one-shot)
    public static void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (instance == null || instance.sfxSource == null || clip == null)
            return;

        instance.sfxSource.PlayOneShot(clip, volume);
    }
}