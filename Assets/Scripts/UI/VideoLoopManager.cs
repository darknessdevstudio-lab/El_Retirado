using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class VideoLoopManager : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public VideoClip introClip;
    public VideoClip loopClip;

    public GameObject pressText;
    public float delay = 3f;

    private bool canPress = false;

    void Start()
    {
        // VIDEO
        videoPlayer.clip = introClip;
        videoPlayer.isLooping = false;
        videoPlayer.Play();
        videoPlayer.loopPointReached += OnIntroFinished;

        // TEXTO
        pressText.SetActive(false);
        Invoke("ShowPressText", delay);
    }

    void OnIntroFinished(VideoPlayer vp)
    {
        videoPlayer.clip = loopClip;
        videoPlayer.isLooping = true;
        videoPlayer.Play();
    }

    void ShowPressText()
    {
        pressText.SetActive(true);
        canPress = true;
    }

    void Update()
    {
        if (pressText.activeSelf)
        {
            float alpha = Mathf.PingPong(Time.time, 1);
            pressText.GetComponent<CanvasGroup>().alpha = alpha;
        }

        if (canPress && Input.GetKeyDown(KeyCode.Space))
        {
            clickSound.Play();
            StartCoroutine(FadeOutAndLoad());
        }
    }
    IEnumerator FadeOutAndLoad()
    {
        yield return new WaitForSeconds(0.2f); // deja sonar el click

        float startVolume = musicSource.volume;
        float t = 0;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0, t / fadeDuration);
            blackFade.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
            yield return null;
        }

        LoadMenu();
    }
    public CanvasGroup blackFade;

    public void LoadMenu()
    {
        SceneManager.LoadScene("Menu_Ini");
    }

    IEnumerator LoadSceneFade()
    {
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime;
            blackFade.alpha = t;
            yield return null;
        }

        SceneManager.LoadScene("Menu_Ini");
    }

    public AudioSource musicSource;
    public float fadeDuration = 2f;

    public AudioSource clickSound;

}

