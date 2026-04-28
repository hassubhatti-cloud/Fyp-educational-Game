using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class SceneButtonManager : MonoBehaviour
{
    public GameObject mainMenuWindow;
    public GameObject levelsWindow;
    public GameObject MenuScreenUI;

    public GameObject credits;
    

    
    [Header("Level Complete High Score")]
    public GameObject levelCompletePanel;
    public TextMeshProUGUI levelCompleteScoreText;
    public TextMeshProUGUI levelCompleteHighScoreText;
    public TextMeshProUGUI levelCompleteMessageText;
    
    // Reference to LoginManager
    private LoginManager loginManager;
    private bool isNewHighScore = false;
    private int newHighScoreValue = 0;
    
    void Start()
    {
        // Find LoginManager
        loginManager = FindObjectOfType<LoginManager>();
        
        if (loginManager == null)
        {
            Debug.LogWarning("LoginManager not found! Using PlayerPrefs fallback.");
            LoadAndDisplayHighScores(); // Fallback to old system
        }
        else
        {
            // Display scores from logged-in user
           // DisplayUserHighScores();
        }
    }
    
    void OnEnable()
    {
        // Refresh high scores whenever this panel becomes active
        if (loginManager != null && LoginManager.CurrentUser != null)
        {
            //DisplayUserHighScores();
        }
    }
    
   
        void LoadAndDisplayHighScores()
    {
        Debug.Log("Using PlayerPrefs fallback (no user logged in)");

    }
   public void DeactivateWindows()
    {
       
       StartCoroutine(DeactivateExtraWindowsonLevel());

    }
     IEnumerator DeactivateExtraWindowsonLevel()
    {
        yield return new WaitForSeconds(0.01f);
        levelsWindow.SetActive(false);
        MenuScreenUI.SetActive(false);

}
  
    public void OnLevelCompleted(int levelNumber, int score, int totalQuestions, bool isWin)
    {
        if (!isWin) return;
        
        if (loginManager != null && LoginManager.CurrentUser != null)
        {
            // Score is already saved in MCQManager, just show the panel
            string levelName = $"Level{levelNumber}";
            int percentage = (score * 100) / (totalQuestions * 10);
            int previousHigh = LoginManager.CurrentUser.GetLevelScore(levelName);
            
            isNewHighScore = (percentage > previousHigh);
            newHighScoreValue = percentage;
            
            // Show level complete panel
            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(true);
                
                if (levelCompleteScoreText != null)
                    levelCompleteScoreText.text = $"Your Score: {percentage}% ({score} points)";
                
                if (isNewHighScore)
                {
                    if (levelCompleteHighScoreText != null)
                    {
                        levelCompleteHighScoreText.text = $"NEW HIGH SCORE!\n{percentage}%!";
                        levelCompleteHighScoreText.color = Color.yellow;
                    }
                    
                    if (levelCompleteMessageText != null)
                        levelCompleteMessageText.text = "Amazing! New Personal Best!";
                }
                else
                {
                    if (levelCompleteHighScoreText != null)
                    {
                        levelCompleteHighScoreText.text = $"High Score: {previousHigh}%";
                        levelCompleteHighScoreText.color = Color.white;
                    }
                    
                    if (levelCompleteMessageText != null)
                        levelCompleteMessageText.text = "Level Complete!";
                }
            }
            
            // Refresh the high score display
            //DisplayUserHighScores();
        }
        else
        {
            // Fallback to old PlayerPrefs system
            Debug.Log("No user logged in, using PlayerPrefs fallback");
            SaveHighScoreLegacy(levelNumber, score);
        }
    }
    
    private const string LEVEL1_HIGH_SCORE = "Level1_HighScore";
    private const string LEVEL2_HIGH_SCORE = "Level2_HighScore";
    private const string LEVEL3_HIGH_SCORE = "Level3_HighScore";
    
    void SaveHighScoreLegacy(int levelNumber, int score)
    {
        string key = "";
        switch (levelNumber)
        {
            case 1: key = LEVEL1_HIGH_SCORE; break;
            case 2: key = LEVEL2_HIGH_SCORE; break;
            case 3: key = LEVEL3_HIGH_SCORE; break;
            default: return;
        }
        
        int currentHighScore = PlayerPrefs.GetInt(key, 0);
        if (score > currentHighScore)
        {
            PlayerPrefs.SetInt(key, score);
            PlayerPrefs.Save();
            Debug.Log($"Legacy high score saved: {key} = {score}");
        }
    }
    
    // Helper method to get current user's high score
    public int GetUserHighScore(int levelNumber)
    {
        if (loginManager != null && LoginManager.CurrentUser != null)
        {
            string levelName = $"Level{levelNumber}";
            return LoginManager.CurrentUser.GetLevelScore(levelName);
        }
        
        // Fallback to PlayerPrefs
        string key = "";
        switch (levelNumber)
        {
            case 1: key = LEVEL1_HIGH_SCORE; break;
            case 2: key = LEVEL2_HIGH_SCORE; break;
            case 3: key = LEVEL3_HIGH_SCORE; break;
            default: return 0;
        }
        return PlayerPrefs.GetInt(key, 0);
    }
    
    // ---------- EXISTING BUTTON METHODS ----------
    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
    
    public void LoadLevel(int levelNumber)
    {
        Time.timeScale = 1f;
        
        // Hide high score panel if showing
        if (loginManager != null)
        {
            loginManager.HideHighScores();
        }
        
        switch (levelNumber)
        {
            case 1:
                SceneManager.LoadScene("Level1");
                break;
            case 2:
                SceneManager.LoadScene("Level2");
                break;
            case 3:
                SceneManager.LoadScene("Level3");
                break;
            default:
                Debug.LogWarning($"Unknown level number: {levelNumber}");
                break;
        }
    }
    
    public void LevelsButton()
    {
        mainMenuWindow.SetActive(false);
        levelsWindow.SetActive(true);
        
        // Refresh high scores when showing levels window
        if (loginManager != null && LoginManager.CurrentUser != null)
        {
            //DisplayUserHighScores();
        }
    }
    
    public void CreditsButton()
    {
        mainMenuWindow.SetActive(false);
        levelsWindow.SetActive(false);
        credits.SetActive(true);
    }
    
    public void BackButton()
    {
        mainMenuWindow.SetActive(true);
        levelsWindow.SetActive(false);
    }
    
    public void ShowMenuWindow()
    {
        mainMenuWindow.SetActive(true);
    }
    
    public void CloseMenuWindow()
    {
        mainMenuWindow.SetActive(false);
    }
    
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    // Reset high scores for current user
    public void ResetUserHighScores()
    {
        if (loginManager != null && LoginManager.CurrentUser != null)
        {
            // Clear all level scores
            foreach (var levelScore in LoginManager.CurrentUser.levelScores)
            {
                levelScore.score = 0;
            }
            
            // Reset total score
            LoginManager.CurrentUser.totalScore = 0;
            
            // Save the changes
            loginManager.SaveUserDatabase();
            
            // Refresh the display
            //DisplayUserHighScores();
            
            Debug.Log($"Reset all high scores for {LoginManager.CurrentUser.username}");
        }
    }
}