using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.IO;

public class LoginManager : MonoBehaviour
{
    // Singleton pattern for easy access
    public static LoginManager Instance;
    public SceneButtonManager sbm;
    
    [Header("Login UI")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_InputField registerUsernameInput;
    public TMP_InputField registerPasswordInput;
    public TMP_InputField confirmPasswordInput;
    public TextMeshProUGUI loginMessageText;
    public TextMeshProUGUI registerMessageText;
    
    [Header("Score Display")]
    public GameObject highScorePanel;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI currentUserText;
    
    [Header("Leaderboard UI")]
    public GameObject leaderboardPanel;
    public TextMeshProUGUI leaderboardText;
    public Button leaderboardButton;
    public Button closeLeaderboardButton;
    
    [Header("Settings")]
    public string saveFileName = "userdata.json";
    
    private UserDatabase userDatabase;
    public static UserData CurrentUser { get; private set; }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadUserDatabase();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        ShowLoginPanel();
        
        if (leaderboardButton != null)
            leaderboardButton.onClick.AddListener(ShowLeaderboard);
        
        if (closeLeaderboardButton != null)
            closeLeaderboardButton.onClick.AddListener(CloseLeaderboard);
    }
    
    // ---------- SAVE/LOAD DATABASE ----------
    
   void LoadUserDatabase()
{
    try
    {
        string filePath = Path.Combine(Application.persistentDataPath, saveFileName);
        
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            userDatabase = JsonUtility.FromJson<UserDatabase>(json);
            Debug.Log("User database loaded!");
            EnsureAllUsersHaveLevelScores();
        }
        else
        {
            userDatabase = new UserDatabase();
            UserData demoUser = new UserData("Guest", "123");
            userDatabase.users.Add(demoUser);
            SaveUserDatabase();
            Debug.Log("Created new user database with demo user");
        }
    }
    catch (Exception e)
    {
        Debug.LogError($"Failed to load user database: {e.Message}");
        userDatabase = new UserDatabase();
        UserData demoUser = new UserData("Guest", "123");
        userDatabase.users.Add(demoUser);
    }
}
    
    void EnsureAllUsersHaveLevelScores()
    {
        bool needsSave = false;
        
        foreach (UserData user in userDatabase.users)
        {
            if (user.levelScores == null)
            {
                user.levelScores = new List<LevelScore>();
                needsSave = true;
            }
            
            string[] requiredLevels = { "Level1", "Level2", "Level3" };
            foreach (string level in requiredLevels)
            {
                if (!user.levelScores.Exists(l => l.levelName == level))
                {
                    user.levelScores.Add(new LevelScore(level, 0));
                    needsSave = true;
                    Debug.Log($"Added missing level {level} for user {user.username}");
                }
            }
        }
        
        if (needsSave)
        {
            SaveUserDatabase();
            Debug.Log("User database initialized successfully!");
        }
    }
    public void SaveUserDatabase()
{
    try
    {
        string filePath = Path.Combine(Application.persistentDataPath, saveFileName);
        
        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        string json = JsonUtility.ToJson(userDatabase, true);
        File.WriteAllText(filePath, json);
        Debug.Log($"User database saved to: {filePath}");
    }
    catch (Exception e)
    {
        Debug.LogError($"Failed to save user database: {e.Message}");
        Debug.LogError($"Path: {Path.Combine(Application.persistentDataPath, saveFileName)}");
    }
}
    
    // ---------- LOGIN SYSTEM ----------
    
    public void Login()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text;
        
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            loginMessageText.text = "Please enter username and password!";
            return;
        }
        
        UserData user = userDatabase.users.Find(u => u.username == username);
        
        if (user != null && user.password == password)
        {
            CurrentUser = user;
            loginMessageText.text = "Login successful!";
            loginMessageText.color = Color.green;
            
            SaveUserDatabase();
            StartCoroutine(OnLoginSuccess());
        }
        else if (user == null)
        {
            loginMessageText.text = "User not found! Please register.";
            loginMessageText.color = Color.red;
        }
        else
        {
            loginMessageText.text = "Wrong password!";
            loginMessageText.color = Color.red;
        }
    }
    
    IEnumerator OnLoginSuccess()
    {
        yield return new WaitForSeconds(1f);
        loginPanel.SetActive(false);
        if (sbm != null)
            sbm.ShowMenuWindow();
        
        Debug.Log($"Welcome {CurrentUser.username}! Total Score: {CurrentUser.totalScore}");
    }
    
    // ---------- REGISTRATION SYSTEM ----------
    
    public void ShowRegisterPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        registerMessageText.text = "";
    }
    
    public void ShowLoginPanel()
    {
        registerPanel.SetActive(false);
        loginPanel.SetActive(true);
        if (sbm != null)
            sbm.CloseMenuWindow();
        
        loginMessageText.text = "";
        usernameInput.text = "";
        passwordInput.text = "";
    }
    
    public void Register()
    {
        string username = registerUsernameInput.text.Trim();
        string password = registerPasswordInput.text;
        string confirm = confirmPasswordInput.text;
        
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            registerMessageText.text = "Please fill all fields!";
            return;
        }
        
        if (password != confirm)
        {
            registerMessageText.text = "Passwords do not match!";
            return;
        }
        
        if (password.Length < 3)
        {
            registerMessageText.text = "Password must be at least 3 characters!";
            return;
        }
        
        if (userDatabase.users.Exists(u => u.username == username))
        {
            registerMessageText.text = "Username already exists!";
            return;
        }
        
        UserData newUser = new UserData(username, password);
        userDatabase.users.Add(newUser);
        SaveUserDatabase();
        
        registerMessageText.text = "Registration successful! Please login.";
        registerMessageText.color = Color.green;
        
        StartCoroutine(BackToLoginAfterDelay());
    }
    
    IEnumerator BackToLoginAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        ShowLoginPanel();
        registerUsernameInput.text = "";
        registerPasswordInput.text = "";
        confirmPasswordInput.text = "";
    }
    
    // ---------- HIGH SCORE SYSTEM ----------
    
    public void SaveScoreForLevel(string levelName, int score, int totalQuestions)
    {
        if (CurrentUser == null)
        {
            Debug.LogError("No user logged in!");
            return;
        }
        
        int percentage = (score * 100) / (totalQuestions * 10);
        int currentScore = CurrentUser.GetLevelScore(levelName);
        
        if (percentage > currentScore)
        {
            CurrentUser.SetLevelScore(levelName, percentage);
            Debug.Log($"New high score for {levelName}: {percentage}% (was {currentScore}%)");
        }
        else
        {
            Debug.Log($"Score {percentage}% not higher than record {currentScore}%");
        }
        
        RecalculateTotalScore();
        CurrentUser.lastPlayed = DateTime.Now;
        SaveUserDatabase();
        
        if (highScorePanel != null && highScorePanel.gameObject.scene.isLoaded)
        {
            //ShowHighScores();
        }
    }
    
    void RecalculateTotalScore()
    {
        if (CurrentUser == null) return;
        
        int total = 0;
        foreach (var levelScore in CurrentUser.levelScores)
        {
            total += levelScore.score;
        }
        CurrentUser.totalScore = total;
    }
    
    public void ShowHighScores()
    {
        if (CurrentUser == null) return;
        
        if (highScorePanel == null)
        {
            Debug.LogWarning("HighScorePanel was destroyed (scene changed). Cannot show high scores.");
            return;
        }
        
        if (sbm != null)
            sbm.CloseMenuWindow();
        
        highScorePanel.SetActive(true);
        currentUserText.text = $"Player: {CurrentUser.username}";
        
        string displayText = "=== HIGH SCORES ===\n\n";
        displayText += $"Level1: {CurrentUser.GetLevelScore("Level1")}%\n";
        displayText += $"Level2: {CurrentUser.GetLevelScore("Level2")}%\n";
        displayText += $"Level3: {CurrentUser.GetLevelScore("Level3")}%\n";
        displayText += $"\nTotal Score: {CurrentUser.totalScore}\n";
        displayText += $"Last Played: {CurrentUser.lastPlayed.ToString("MM/dd/yyyy")}";
        
        highScoreText.text = displayText;
    }
    
    public void HideHighScores()
    {
        if (highScorePanel != null)
            highScorePanel.SetActive(false);
        
        if (sbm != null)
            sbm.ShowMenuWindow();
    }
    
    // ---------- LEADERBOARD SYSTEM ----------
    
    public void ShowLeaderboard()
    {
        if (userDatabase == null || userDatabase.users.Count == 0)
        {
            if (leaderboardText != null)
                leaderboardText.text = "No users found!";
            if (leaderboardPanel != null)
                leaderboardPanel.SetActive(true);
            return;
        }
        
        List<UserScoreData> userScores = new List<UserScoreData>();
        
        foreach (UserData user in userDatabase.users)
        {
            int userTotalScore = 0;
            foreach (var levelScore in user.levelScores)
            {
                userTotalScore += levelScore.score;
            }
            
            userScores.Add(new UserScoreData
            {
                username = user.username,
                totalScore = userTotalScore,
                level1Score = user.GetLevelScore("Level1"),
                level2Score = user.GetLevelScore("Level2"),
                level3Score = user.GetLevelScore("Level3")
            });
        }
        
        userScores.Sort((a, b) => b.totalScore.CompareTo(a.totalScore));
        
        string displayText = "<color=yellow>═══════════════════════════════════</color>\n";
        displayText += "<color=yellow>         L E A D E R B O A R D        </color>\n";
        displayText += "<color=yellow>═══════════════════════════════════</color>\n\n";
        displayText += "<color=cyan>Rank  Player              Total   L1   L2   L3</color>\n";
        displayText += "<color=grey>───────────────────────────────────────</color>\n";
        
        int rank = 1;
        foreach (UserScoreData user in userScores)
        {
            string colorTag = (CurrentUser != null && user.username == CurrentUser.username) ? "<color=#00FF00><b>" : "<color=white>";
            string endColor = (CurrentUser != null && user.username == CurrentUser.username) ? "</b></color>" : "</color>";
            
            displayText += $"{colorTag}{rank,2}.  {user.username,-15}  {user.totalScore,6}  {user.level1Score,3}%  {user.level2Score,3}%  {user.level3Score,3}%{endColor}\n";
            
            rank++;
            if (rank > 10) break;
        }
        
        if (CurrentUser != null && rank > 10)
        {
            var currentUserData = userScores.Find(u => u.username == CurrentUser.username);
            if (currentUserData != null)
            {
                int currentUserRank = userScores.FindIndex(u => u.username == CurrentUser.username) + 1;
                displayText += $"\n<color=grey>  ...</color>\n";
                displayText += $"<color=#00FF00><b>{currentUserRank,2}.  {currentUserData.username,-15}  {currentUserData.totalScore,6}  {currentUserData.level1Score,3}%  {currentUserData.level2Score,3}%  {currentUserData.level3Score,3}%</b></color>\n";
            }
        }
        
        displayText += "\n<color=yellow>═══════════════════════════════════</color>";
        
        if (leaderboardText != null)
            leaderboardText.text = displayText;
        
        if (highScorePanel != null)
            highScorePanel.SetActive(false);
        if (loginPanel != null)
            loginPanel.SetActive(false);
        if (registerPanel != null)
            registerPanel.SetActive(false);
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(true);
    }
    
    public void CloseLeaderboard()
    {
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
        
        if (CurrentUser != null)
        {
            ShowHighScores();
        }
        else
        {
            ShowLoginPanel();
        }
    }
    
    // ---------- LOGOUT SYSTEM ----------
    
    public void Logout()
    {
        CurrentUser = null;
        
        if (highScorePanel != null)
            highScorePanel.SetActive(false);
        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);
        if (loginPanel != null)
            loginPanel.SetActive(true);
        if (registerPanel != null)
            registerPanel.SetActive(false);
        if (usernameInput != null)
            usernameInput.text = "";
        if (passwordInput != null)
            passwordInput.text = "";
        if (loginMessageText != null)
            loginMessageText.text = "";
        
        Debug.Log("User logged out");
    }
    
    // ---------- UTILITY METHODS ----------
    
    public static UserData GetCurrentUser()
    {
        return CurrentUser;
    }
    
    public int GetLevelHighScore(int levelNumber)
    {
        if (CurrentUser == null) return 0;
        string levelName = $"Level{levelNumber}";
        return CurrentUser.GetLevelScore(levelName);
    }
    
    public bool IsUserLoggedIn()
    {
        return CurrentUser != null;
    }
    
    public void StartGame()
    {
        if (CurrentUser != null)
        {
            Debug.Log($"Starting game for {CurrentUser.username}");
            HideHighScores();
        }
        else
        {
            Debug.LogWarning("No user logged in!");
        }
    }
}

// Helper class for leaderboard data only
[System.Serializable]
public class UserScoreData
{
    public string username;
    public int totalScore;
    public int level1Score;
    public int level2Score;
    public int level3Score;
}