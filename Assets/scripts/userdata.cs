using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UserData
{
    public string username;
    public string password;
    public List<LevelScore> levelScores = new List<LevelScore>();  // ✓ This is public
    public int totalScore;
    public DateTime lastPlayed;
    
    public UserData(string name, string pass)
    {
        username = name;
        password = pass;
        totalScore = 0;
        lastPlayed = DateTime.Now;
        levelScores = new List<LevelScore>();
        
        levelScores.Add(new LevelScore("Level1", 0));
        levelScores.Add(new LevelScore("Level2", 0));
        levelScores.Add(new LevelScore("Level3", 0));
    }
    
    public int GetLevelScore(string levelName)
    {
        LevelScore found = levelScores.Find(l => l.levelName == levelName);
        return found != null ? found.score : 0;
    }
    
    public void SetLevelScore(string levelName, int score)
    {
        LevelScore found = levelScores.Find(l => l.levelName == levelName);
        if (found != null)
        {
            found.score = score;
        }
        else
        {
            levelScores.Add(new LevelScore(levelName, score));
        }
    }
}

[System.Serializable]
public class LevelScore
{
    public string levelName;
    public int score;
    
    public LevelScore(string name, int scoreValue)
    {
        levelName = name;
        score = scoreValue;
    }
}

[System.Serializable]
public class UserDatabase
{
    public List<UserData> users = new List<UserData>();
}