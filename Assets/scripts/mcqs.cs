using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MCQManager : MonoBehaviour
{
    // ---------- SIMPLIFIED JSON STRUCTURE ----------
    [System.Serializable]
    public class APIResponse
    {
        public int response_code;
        public int total_levels;
        public LevelData level1;
        public LevelData level2;
        public LevelData level3;
    }


    [System.Serializable]
    public class LevelData
    {
        public string name;
        public int count;
        public List<QuestionData> questions;
    }

    [System.Serializable]
    public class QuestionData
    {
        public string type;
        public string difficulty;
        public string category;
        public string question;
        public string correct_answer;
        public List<string> incorrect_answers;
    }

    // ---------- Car Controller ----------

  public PrometeoCarController pcc;
    // ---------- VARIABLES ----------

    private List<QuestionData> currentLevelQuestions = new List<QuestionData>();
    private List<QuestionData> usedQuestions = new List<QuestionData>();
    private List<QuestionData> activeMCQs = new List<QuestionData>();
    private int currentMCQIndex = 0;
    private int mcqsPerSet = 2;
    private int totalQuestionsForLevel = 0;
    private int questionsAnsweredInLevel = 0;
    private int score = 0;
    private int correctAnswersCount = 0;

    [Header("Level Settings")]
    public int totalMCQsForThisLevel = 4; // Level 1 = 4, Level 2 = 6, Level 3 = 8

    [Header("UI Components")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI questionsRemainingText;
    public Button[] optionButtons;
    public GameObject quizPanel;
    public Animator ani;

    [Header("Settings")]
    public float timePerMCQ = 10f;
    private float currentTime;
    public bool isTiming;
    public bool isGivingHint = false;

    [HideInInspector] public bool isMCQActive = false;
    [HideInInspector] public bool allMCQsCompleted = false;

    [Header("Audio")]
    public AudioClip correctSound;
    public AudioClip incorrectSound;
    private AudioSource audioSource;

    // Reference to WinConditionChecker to show end menu
    private WinConditionChecker winConditionChecker;
  


    // ---------- UNITY METHODS ----------
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        // Find WinConditionChecker in scene
        winConditionChecker = FindObjectOfType<WinConditionChecker>();
    }
  

private LoginManager loginManager;
void Start()
{
    LoadMCQsFromJSON();
    UpdateScoreUI();
    SetLevelBasedOnScene();
    
    // FIND LOGIN MANAGER 
    loginManager = FindObjectOfType<LoginManager>();
    if (loginManager == null)
    {
        Debug.LogWarning("LoginManager not found! Scores won't be saved.");
    }
}

void AllMCQsCompleted()
{
    allMCQsCompleted = true;
    isMCQActive = false;
    
    Debug.Log($"===== LEVEL COMPLETE! =====");
    Debug.Log($"Final Score: {score}/{totalMCQsForThisLevel * 10}");
    Debug.Log($"Correct Answers: {correctAnswersCount}/{totalMCQsForThisLevel}");
    
    // SAVE HIGH SCORE FOR CURRENT USER
    if (loginManager != null && LoginManager.CurrentUser != null)
    {
        string currentLevel = SceneManager.GetActiveScene().name;
        loginManager.SaveScoreForLevel(currentLevel, score, totalMCQsForThisLevel);
        Debug.Log($"Score saved for {LoginManager.CurrentUser.username} on {currentLevel}");
    }
    else
    {
        if (loginManager == null)
            Debug.LogWarning("Cannot save score: LoginManager not found!");
        else if (LoginManager.CurrentUser == null)
            Debug.LogWarning("Cannot save score: No user logged in!");
    }
    
    // Show end menu
    if (winConditionChecker != null)
    {
        winConditionChecker.ShowEndMenuAfterMCQs(score, totalMCQsForThisLevel);
    }
    else
    {
        Debug.LogError("WinConditionChecker not found! Cannot show end menu.");
    }
}
    void Update()
    {
        if (isTiming)
        {pcc.Brakes();
        pcc.useSounds=false;
            currentTime -= Time.deltaTime;
            timerText.text = "Time: " + Mathf.Ceil(currentTime);

            if (currentTime <= 0)
            {
                isTiming = false;
                HandleTimeOut();
            }
        }
        if (!isTiming)
        {  pcc.useSounds=true;}
    }

    void SetLevelBasedOnScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        // Check scene name to determine number of MCQs
        if (currentSceneName == "Level1" || currentSceneName == "level1" || currentSceneName == "Level 1")
        {
            totalMCQsForThisLevel = 4;
        }
        else if (currentSceneName == "Level2" || currentSceneName == "level2" || currentSceneName == "Level 2")
        {
            totalMCQsForThisLevel = 6;
        }
        else if (currentSceneName == "Level3" || currentSceneName == "level3" || currentSceneName == "Level 3")
        {
            totalMCQsForThisLevel = 4;
        }
        else
        {
            // Try to extract number from scene name 
            int levelNumber = ExtractLevelNumberFromSceneName(currentSceneName);
            if (levelNumber == 1)
                totalMCQsForThisLevel = 4;
            else if (levelNumber == 2)
                totalMCQsForThisLevel = 6;
            else if (levelNumber == 3)
                totalMCQsForThisLevel = 4;
            else
                totalMCQsForThisLevel = 4; 
        }
        
        totalQuestionsForLevel = totalMCQsForThisLevel;
        Debug.Log($"Scene Name: '{currentSceneName}' - Total MCQs required: {totalMCQsForThisLevel}");
        UpdateQuestionsRemainingUI();
    }

    // Helper method to extract level number from scene name
    int ExtractLevelNumberFromSceneName(string sceneName)
    {
        foreach (char c in sceneName)
        {
            if (char.IsDigit(c))
            {
                return int.Parse(c.ToString());
            }
        }
        return 0;
    }

    void HandleTimeOut()
    {
        PlayIncorrect();
        NextMCQInSet();
    }

    // ---------- LOAD JSON ----------
    void LoadMCQsFromJSON()
    {
        TextAsset file = Resources.Load<TextAsset>("mcqs");
        if (file != null)
        {
            Debug.Log("JSON file loaded successfully!");
            
            APIResponse data = JsonUtility.FromJson<APIResponse>(file.text);
            
            if (data != null)
            {
                LoadLevelQuestions(data);
            }
            else
            {
                Debug.LogError("Failed to parse JSON! Check the format.");
            }
        }
        else
        {
            Debug.LogError("MCQs JSON file not found in Resources folder!");
        }
    }

    void LoadLevelQuestions(APIResponse data)
    {
        LevelData selectedLevel = null;
        string currentSceneName = SceneManager.GetActiveScene().name;
        int levelNumber = ExtractLevelNumberFromSceneName(currentSceneName);
        
        // If level number is not found in name, try to determine from scene name string
        if (levelNumber == 0)
        {
            if (currentSceneName.Contains("1") || currentSceneName.ToLower().Contains("one"))
                levelNumber = 1;
            else if (currentSceneName.Contains("2") || currentSceneName.ToLower().Contains("two"))
                levelNumber = 2;
            else if (currentSceneName.Contains("3") || currentSceneName.ToLower().Contains("three"))
                levelNumber = 3;
            else
                levelNumber = 1; 
        }

        switch (levelNumber)
        {
            case 1:
                selectedLevel = data.level1;
                break;
            case 2:
                selectedLevel = data.level2;
                break;
            case 3:
                selectedLevel = data.level3;
                break;
            default:
                selectedLevel = data.level1;
                break;
        }

        if (selectedLevel != null && selectedLevel.questions != null && selectedLevel.questions.Count > 0)
        {
            currentLevelQuestions.Clear();
            currentLevelQuestions.AddRange(selectedLevel.questions);
            
            ResetLevelState();
            
            Debug.Log($"Successfully loaded {currentLevelQuestions.Count} questions for level {levelNumber}: {selectedLevel.name}");
        }
        else
        {
            Debug.LogError($"Failed to load questions for level {levelNumber}.");
        }
    }

    void ResetLevelState()
    {
        usedQuestions.Clear();
        questionsAnsweredInLevel = 0;
        score = 0;
        correctAnswersCount = 0;
        allMCQsCompleted = false;
        UpdateScoreUI();
        UpdateQuestionsRemainingUI();
        Debug.Log($"Level reset. Need to answer {totalMCQsForThisLevel} MCQs total.");
    }

    // ---------- TRIGGER MCQ ----------
    public void TriggerMCQs()
    {
        if (allMCQsCompleted)
        {
            Debug.Log($"Already answered {totalMCQsForThisLevel} MCQs. Level complete!");
            return;
        }

        if (questionsAnsweredInLevel >= totalMCQsForThisLevel)
        {
            Debug.Log($"Already answered {questionsAnsweredInLevel}/{totalMCQsForThisLevel} MCQs. Completing level...");
            AllMCQsCompleted();
            return;
        }

        if (currentLevelQuestions.Count == 0)
        {
            Debug.LogError("No questions loaded!");
            return;
        }

        int remainingQuestionsNeeded = totalMCQsForThisLevel - questionsAnsweredInLevel;
        int questionsToTake = Mathf.Min(mcqsPerSet, remainingQuestionsNeeded);
        
        if (questionsToTake <= 0)
        {
            AllMCQsCompleted();
            return;
        }

        List<QuestionData> unusedQuestions = GetUnusedQuestions();
        
        if (unusedQuestions.Count < questionsToTake)
        {
            Debug.LogWarning($"Not enough unused questions! Resetting question pool.");
            ResetQuestionPool();
            unusedQuestions = GetUnusedQuestions();
        }

        isMCQActive = true;
        quizPanel.SetActive(true);
        
        if (ani != null)
            ani.SetTrigger("popup");
        
        LoadRandomMCQs(unusedQuestions, questionsToTake);
        currentMCQIndex = 0;
        ShowCurrentMCQ();
    }

    List<QuestionData> GetUnusedQuestions()
    {
        List<QuestionData> unused = new List<QuestionData>();
        foreach (QuestionData q in currentLevelQuestions)
        {
            if (!usedQuestions.Contains(q))
            {
                unused.Add(q);
            }
        }
        return unused;
    }

    void ResetQuestionPool()
    {
        usedQuestions.Clear();
        Debug.Log("Question pool reset. All questions available again.");
    }

    void LoadRandomMCQs(List<QuestionData> unusedQuestions, int count)
    {
        activeMCQs.Clear();
        
        ShuffleQuestions(unusedQuestions);
        
        for (int i = 0; i < count && i < unusedQuestions.Count; i++)
        {
            activeMCQs.Add(unusedQuestions[i]);
            usedQuestions.Add(unusedQuestions[i]);
        }
        
        Debug.Log($"Loaded {activeMCQs.Count} MCQs for this session. Answered so far: {questionsAnsweredInLevel}/{totalMCQsForThisLevel}");
    }
  // shuffle Questions
    void ShuffleQuestions(List<QuestionData> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            QuestionData value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    // ---------- SHOW MCQ ----------
    void ShowCurrentMCQ()
    {
        if (currentMCQIndex >= activeMCQs.Count)
        {
            EndQuizSession();
            return;
        }

        QuestionData q = activeMCQs[currentMCQIndex];
        
        questionText.text = System.Net.WebUtility.HtmlDecode(q.question);

        List<string> options = new List<string>();
        options.Add(q.correct_answer);
        options.AddRange(q.incorrect_answers);
        Shuffle(options);

        for (int i = 0; i < optionButtons.Length; i++)
        {
            string optionText = System.Net.WebUtility.HtmlDecode(options[i]);
            optionButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = optionText;
            
            optionButtons[i].image.color = Color.white;
            optionButtons[i].interactable = true;

            optionButtons[i].onClick.RemoveAllListeners();
            string selectedText = options[i];
            optionButtons[i].onClick.AddListener(() => CheckAnswer(selectedText, q.correct_answer));
        }

      
        
        currentTime = timePerMCQ;
        isTiming = true;
    }

    // ---------- CHECK ANSWER ----------
    void CheckAnswer(string selected, string correct)
    {
        isTiming = false;

        string decodedCorrect = System.Net.WebUtility.HtmlDecode(correct);
        string decodedSelected = System.Net.WebUtility.HtmlDecode(selected);
        bool isCorrect = (decodedSelected == decodedCorrect);

        foreach (Button btn in optionButtons)
        {
            btn.interactable = false;
            string buttonText = btn.GetComponentInChildren<TextMeshProUGUI>().text;

            if (buttonText == decodedCorrect)
            {
                btn.image.color = Color.green;
            }
            else if (buttonText == decodedSelected && !isCorrect)
            {
                btn.image.color = Color.red;
            }
        }
        
        if (isCorrect)
        {
            PlayCorrect();
            score += 10;
            correctAnswersCount++;
            UpdateScoreUI();
        }
        else
        {
            PlayIncorrect();
        }

        Invoke(nameof(NextMCQInSet), 1.5f);
    }

    void NextMCQInSet()
    {
        currentMCQIndex++;
        ShowCurrentMCQ();
    }

void EndQuizSession()
{
    isTiming = false;
    isMCQActive = false;
     pcc.useSounds=true;

    // Increment the counter for answered questions
    questionsAnsweredInLevel += activeMCQs.Count;
    
    Debug.Log($"Quiz session ended! Questions answered this session: {activeMCQs.Count}");
    Debug.Log($"Total questions answered in level: {questionsAnsweredInLevel}/{totalMCQsForThisLevel}");
    
    UpdateQuestionsRemainingUI();
    
    // Play close/popout animation before hiding panel
    if (ani != null)
    {
        ani.SetTrigger("popin"); // Trigger the popout/close animation
        Debug.Log("Playing popin animation before closing quiz panel");
        
        // Wait for animation to play before deactivating panel
        StartCoroutine(CloseQuizPanelAfterAnimation());
    }
    else
    {
        quizPanel.SetActive(false);
        
        if (questionsAnsweredInLevel >= totalMCQsForThisLevel)
        {
            Debug.Log($"===== All {totalMCQsForThisLevel} MCQs answered! Showing End Menu... =====");
            AllMCQsCompleted();
        }
        else
        {
            int remaining = totalMCQsForThisLevel - questionsAnsweredInLevel;
            Debug.Log($"Need {remaining} more MCQs. Call TriggerMCQs() again.");
        }
    }
}

IEnumerator CloseQuizPanelAfterAnimation()
{
    // Wait for the animation to finish 
    yield return new WaitForSeconds(1.2f);
    
    //  close the panel
    quizPanel.SetActive(false);
    
    if (questionsAnsweredInLevel >= totalMCQsForThisLevel)
    {
        Debug.Log($"===== All {totalMCQsForThisLevel} MCQs answered! Showing End Menu... =====");
        AllMCQsCompleted();
    }
    else
    {
        int remaining = totalMCQsForThisLevel - questionsAnsweredInLevel;
        Debug.Log($"Need {remaining} more MCQs. Call TriggerMCQs() again.");
    }
}


    void UpdateQuestionsRemainingUI()
    {
        if (questionsRemainingText != null)
        {
            int remaining = totalMCQsForThisLevel - questionsAnsweredInLevel;
            questionsRemainingText.text = $"Questions Left: {remaining}";
            
            if (remaining <= 0)
            {
                questionsRemainingText.text = "Level Complete!";
            }
        }
    }

    // ---------- HINT SYSTEM ----------
   

    // shuffle options
    void Shuffle(List<string> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            string value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    public int GetScore() 
    { 
        return score; 
    }
    
    public int GetTotalQuestionsForLevel()
    {
        return totalMCQsForThisLevel;
    }

    public int GetQuestionsAnsweredInLevel()
    {
        return questionsAnsweredInLevel;
    }

    public int GetCorrectAnswersCount()
    {
        return correctAnswersCount;
    }

    public bool AreAllMCQsCompleted()
    {
        return allMCQsCompleted;
    }
    
    public void ResetLevel()
    {
        ResetLevelState();
        ResetQuestionPool();
    }

    void PlayCorrect()
    {
        if (correctSound != null && audioSource != null)
            audioSource.PlayOneShot(correctSound);
    }

    void PlayIncorrect()
    {
        if (incorrectSound != null && audioSource != null)
            audioSource.PlayOneShot(incorrectSound);
    }
}