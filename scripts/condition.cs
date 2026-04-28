using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class WinConditionChecker : MonoBehaviour
{
    public MCQManager mcq;
    public Animator Endgameanimator;
    public GameObject EndgameMenuUI;
    
    [Header("Buttons")]
    public Button nextlevelButton;
    public Button retryButton;
    public Button quitButton;
    public Button mainMenuButton;

    public TextMeshProUGUI endMessageText;

    private bool gameEnded = false;
    private int currentLevelNumber = 1;
    
    // LoginManager reference
    private LoginManager loginManager;
    
    private const string LEVEL1_HIGH_SCORE = "Level1_HighScore";
    private const string LEVEL2_HIGH_SCORE = "Level2_HighScore";
    private const string LEVEL3_HIGH_SCORE = "Level3_HighScore";

    void Start()
    {
        if (mcq == null) mcq = FindObjectOfType<MCQManager>();
        
        // Find LoginManager
        loginManager = FindObjectOfType<LoginManager>();
        if (loginManager == null)
        {
            Debug.LogWarning("LoginManager not found! Using PlayerPrefs fallback.");
        }
        else
        {
            Debug.Log("LoginManager found! Scores will be saved per user.");
        }
        
        if (EndgameMenuUI != null) 
            EndgameMenuUI.SetActive(false);
        
        DetermineCurrentLevel();
        SetupButtons();
        
        Debug.Log($"WinConditionChecker Started - Level: {currentLevelNumber}");
    }

    void DetermineCurrentLevel()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        
        if (sceneName.Contains("1"))
            currentLevelNumber = 1;
        else if (sceneName.Contains("2"))
            currentLevelNumber = 2;
        else if (sceneName.Contains("3"))
            currentLevelNumber = 3;
        else
            currentLevelNumber = 1;
        
        Debug.Log($"Current level: {currentLevelNumber}");
    }

    void SetupButtons()
    {
        if (nextlevelButton != null)
            nextlevelButton.onClick.AddListener(LoadNextLevel);
        
        if (retryButton != null)
            retryButton.onClick.AddListener(RetryLevel);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
        
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(LoadMainMenu);
    }

    public void ShowEndMenuAfterMCQs(int score, int totalQuestions)
    {
        if (gameEnded) return;
        
        Debug.Log($"ShowEndMenuAfterMCQs called! Score: {score}, Total Questions: {totalQuestions}");
        EvaluateWinCondition(score, totalQuestions);
    }

    void EvaluateWinCondition(int score, int totalQuestions)
    {
        int currentScene = SceneManager.GetActiveScene().buildIndex;
        bool isWin = false;
        int maxScore = totalQuestions * 10;
        int minScoreRequired = 20;

        if (currentScene == 1 || currentLevelNumber == 1)
        {
            isWin = (score >= minScoreRequired && score <= maxScore);
        }
        else if (currentScene == 2 || currentLevelNumber == 2)
        {
            isWin = (score >= minScoreRequired && score <= maxScore);
        }
        else if (currentScene == 3 || currentLevelNumber == 3)
        {
            isWin = (score >= minScoreRequired && score <= maxScore);
        }
        else
        {
            isWin = (score >= minScoreRequired);
        }

        Debug.Log($"Is Win: {isWin}");

        // Save high score if player won
        if (isWin)
        {
            SaveHighScore(score, totalQuestions);
        }

        // Show appropriate end menu
        if (isWin)
        {
            ShowEndMenu(true, $"You Win!\nScore: {score}/{maxScore}\nCorrect Answers: {score/10}/{totalQuestions}");
        }
        else
        {
            ShowEndMenu(false, $"Level Complete - Score too low!\nScore: {score}/{maxScore}\nRequired: {minScoreRequired}-{maxScore}");
        }
    }

    void SaveHighScore(int score, int totalQuestions)
    {
        // Method 1: Using LoginManager 
        if (loginManager != null && LoginManager.CurrentUser != null)
        {
            string levelName = $"Level{currentLevelNumber}";
            Debug.Log($"Saving score for {LoginManager.CurrentUser.username} on {levelName}");
            
            loginManager.SaveScoreForLevel(levelName, score, totalQuestions);
            Debug.Log($"✅ Score saved using LoginManager");
        }
        // Method 2: Fallback to PlayerPrefs (if no user logged in)
        else
        {
            Debug.Log("No user logged in, using PlayerPrefs fallback");
            SaveHighScoreToPlayerPrefs(score);
        }
    }

    void SaveHighScoreToPlayerPrefs(int score)
    {
        string key = "";
        
        switch (currentLevelNumber)
        {
            case 1:
                key = LEVEL1_HIGH_SCORE;
                break;
            case 2:
                key = LEVEL2_HIGH_SCORE;
                break;
            case 3:
                key = LEVEL3_HIGH_SCORE;
                break;
            default:
                Debug.LogWarning($"Unknown level number: {currentLevelNumber}");
                return;
        }
        
        int currentHighScore = PlayerPrefs.GetInt(key, 0);
        Debug.Log($"Current high score for {key}: {currentHighScore}");
        
        if (score > currentHighScore)
        {
            PlayerPrefs.SetInt(key, score);
            PlayerPrefs.Save();
            Debug.Log($"✅ NEW HIGH SCORE SAVED to PlayerPrefs! Level {currentLevelNumber}: {score}");
        }
        else
        {
            Debug.Log($"Score {score} did not beat high score {currentHighScore}");
        }
    }

    void ShowEndMenu(bool showNextLevel, string message)
    {
        if (gameEnded) return;
        
        // Close quiz panel safely
        if (mcq != null && mcq.quizPanel != null)
        {
            mcq.quizPanel.SetActive(false);
        }
        
        gameEnded = true;
        
        if (EndgameMenuUI != null) 
            EndgameMenuUI.SetActive(true);
        
        StartCoroutine(PlayEndGameAnimation());
        
        if (nextlevelButton != null)
            nextlevelButton.gameObject.SetActive(showNextLevel);

        if (endMessageText != null)
            endMessageText.text = message;

        Time.timeScale = 0f;
        
        Debug.Log($"End Menu Shown - Next Level Button: {showNextLevel}");
    }

    IEnumerator PlayEndGameAnimation()
    {
        yield return null;
        
        if (Endgameanimator != null)
        {
            Endgameanimator.SetTrigger("open");
            Debug.Log("Endgame animator triggered 'open' successfully");
        }
    }

    public void RetryLevel()
    {
        Debug.Log("RetryLevel called");
        Time.timeScale = 1f;
        
        if (mcq != null)
        {
            mcq.ResetLevel();
        }
        
        gameEnded = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Debug.Log("LoadMainMenu called");
        Time.timeScale = 1f;
        gameEnded = false;
        SceneManager.LoadScene(0); 
    }

    public void LoadNextLevel()
    {
        Debug.Log("LoadNextLevel called");
        Time.timeScale = 1f;
        gameEnded = false;
        
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        
        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            Debug.Log($"Loading next scene: {nextSceneIndex}");
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("No more levels! Loading main menu...");
            SceneManager.LoadScene(0);
        }
    }

    public void QuitGame()
    {
        Debug.Log("QuitGame called");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}