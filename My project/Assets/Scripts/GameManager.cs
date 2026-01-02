using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Run Stats")]
    public float startTime;
    public int fumblesOccurred;
    public int enemiesDefeated;
    public int collectiblesPickedUp;
    public float damageTaken;
    public bool hasPackage = true;

    [Header("State Flags")]
    public bool isIntroSequence = false;
    public bool isFumbleMiniGame = false;

    [Header("References")]
    public Package currentPackage; // Reference to the physical package in the scene

    void Awake()
    {
        // Improved Singleton pattern to prevent NullReference on access
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartRun()
    {
        startTime = Time.time;
        isIntroSequence = false;
        hasPackage = true;
    }

    public void TriggerFumble()
    {
        if (isFumbleMiniGame || !hasPackage) return;

        isFumbleMiniGame = true;
        fumblesOccurred++;
        hasPackage = false;

        Debug.Log("FUMBLE! Package is loose!");

        if (currentPackage != null)
        {
            currentPackage.Drop();
        }
        else
        {
            Debug.LogError("GameManager: No Package reference assigned! Assign the Package script in the inspector.");
        }
    }

    public void RecoverPackage()
    {
        isFumbleMiniGame = false;
        hasPackage = true;
        Debug.Log("Package Recovered!");
    }

    public int CalculateFinalScore()
    {
        float timeTaken = Time.time - startTime;
        int score = (enemiesDefeated * 100) + (collectiblesPickedUp * 50);
        score -= (fumblesOccurred * 200);
        int timeBonus = Mathf.Max(0, 5000 - (int)(timeTaken * 10));
        return score + timeBonus;
    }
}