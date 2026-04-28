using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenuController : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public Button resumeButton;
    public Button pauseButton;
    public Button quitButton;
    public Animator pauseAnimator;

    private bool isPaused = false;

    void Start()
    {
        pauseMenuUI.SetActive(false);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(PauseGame);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
    }


    public void PauseGame()
    {
        isPaused = true;
        pauseMenuUI.SetActive(true);

        if (pauseAnimator != null)
        {
            pauseAnimator.SetTrigger("open");
            StartCoroutine(PauseAfterAnimation());
        }
        else
        {
            FinalizePause();
        }
    }

    public void ResumeGame()
    {
        if (pauseAnimator != null)
        {
           
            pauseAnimator.SetTrigger("close");
            StartCoroutine(ResumeAfterAnimation());
        }
        else
        {
            FinalizeResume();
        }
    }

    private System.Collections.IEnumerator ResumeAfterAnimation()
    {
        float closeDuration = GetAnimationClipLength("close");
        yield return new WaitForSecondsRealtime(closeDuration);
        FinalizeResume();
    }

    private System.Collections.IEnumerator PauseAfterAnimation()
    {
        float openDuration = GetAnimationClipLength("open");
        yield return new WaitForSecondsRealtime(openDuration);
        FinalizePause();
    }

    private void FinalizeResume()
    {
        Time.timeScale = 1f;
        pauseMenuUI.SetActive(false);
        isPaused = false;
    }

    private void FinalizePause()
    {
        Time.timeScale = 0f;
        pauseMenuUI.SetActive(true);
        isPaused = true;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private float GetAnimationClipLength(string clipName)
    {
        if (pauseAnimator == null) return 0.5f;

        foreach (var clip in pauseAnimator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
                return clip.length;
        }

        return 0.5f;
    }
}
