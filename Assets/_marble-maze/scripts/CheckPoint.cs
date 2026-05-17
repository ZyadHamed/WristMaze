using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Firebase.Database;
using UnityEngine.SceneManagement;

public class CheckPoint : MonoBehaviour
{
    public float checkpointPercentage = 0.0f;
    bool hit;

    private Renderer checkpointRenderer;
    private Color originalColor;

    [Header("UI References (TextMeshPro)")]
    public TMP_Text completionTimeText;
    public TMP_Text retriesText;

    [Header("End Screen UI References")]
    public GameObject endScreenPanel; // Drag your Panel here
    public TMP_Text endScreenTitle;   // Drag your TitleText here
    public TMP_Text endScreenStats;   // Drag your StatsText here 


    [Header("Level Info")]
    public string customLevelName = "";

    // 1. CREATE THE STATIC VARIABLE
    // This is shared across ALL checkpoints and survives scene reloads.
    public static float maxPercentageReached = 0f;

    public void Reset()
    {
        hit = false;

        if (checkpointRenderer != null)
        {
            checkpointRenderer.material.color = originalColor;
        }
    }

    void Start()
    {
        checkpointRenderer = GetComponent<Renderer>();

        if (checkpointRenderer != null)
        {
            originalColor = checkpointRenderer.material.color;
        }

        Reset();

        // Optional: If this is the VERY FIRST checkpoint (0%), reset the static tracker.
        // This ensures that when they start a brand new game, the percentage goes back to 0.
        if (checkpointPercentage == 0f)
        {
            maxPercentageReached = 0f;
            Hole.fallCount = 0;
            SendLevelStartToFirebase();
        }
    }

    private void SendLevelStartToFirebase()
    {
        string levelNameToSend = string.IsNullOrEmpty(customLevelName)
            ? SceneManager.GetActiveScene().name
            : customLevelName;

        DatabaseReference levelRef = FirebaseDatabase.DefaultInstance.GetReference("gameLevel");
        levelRef.SetValueAsync(levelNameToSend);

        Debug.Log($"Level Started! Sent '{levelNameToSend}' to Firebase Realtime Database.");
    }

    private void OnTriggerEnter(Collider other)
    {
        string otherName = other.gameObject.transform.name;
        if (otherName.Equals("Marble"))
        {
            if (!hit)
            {
                hit = true;
                Debug.Log(otherName + " hit " + transform.name);

                // ADD THIS LINE: Overwrite the hole's respawn position to this checkpoint!
                Hole.currentRespawnPosition = transform.position;

                if (checkpointRenderer != null)
                {
                    checkpointRenderer.material.color = Color.green;
                }

                if (checkpointPercentage > maxPercentageReached)
                {
                    maxPercentageReached = checkpointPercentage;
                }
            }

            if (transform.name.Equals("FINISH"))
            {
                maxPercentageReached = 100f;
                SendStatsToFirebase();

                TriggerEndScreen(true);
            }
        }
    }

    private void SendStatsToFirebase()
    {
        string timeString = completionTimeText.text;
        string triesString = retriesText.text;

        int triesCount = 0;
        int.TryParse(triesString, out triesCount);

        double totalSeconds = 0;
        if (TimeSpan.TryParse(timeString, out TimeSpan parsedTime))
        {
            totalSeconds = parsedTime.TotalSeconds;
        }
        else
        {
            Debug.LogError("Could not parse time string. Make sure it is strictly hh:mm:ss format.");
        }

        DatabaseReference statsRef = FirebaseDatabase.DefaultInstance.GetReference("game_stats").Push();

        statsRef.Child("completionTimeSeconds").SetValueAsync(totalSeconds);
        statsRef.Child("retries").SetValueAsync(triesCount);

        // Bonus: You can also send their highest percentage to Firebase here if they lose!
        statsRef.Child("percentageCompleted").SetValueAsync(maxPercentageReached);

        Debug.Log($"Game Finished! Uploaded: {totalSeconds} seconds, {triesCount} retries, {maxPercentageReached}% completed.");
    }

    public void TriggerEndScreen(bool playerWon)
    {
        // 1. Show the Panel
        endScreenPanel.SetActive(true);

        // 2. Set the Title Text and Color
        if (playerWon)
        {
            endScreenTitle.text = "YOU WON!";
            endScreenTitle.color = Color.green;
        }
        else
        {
            endScreenTitle.text = "YOU LOST!";
            endScreenTitle.color = Color.red;
        }

        // 3. Dump all the stats into the Stats text box using \n for new lines
        endScreenStats.text = $"Time: {completionTimeText.text}\n" +
                              $"Retries: {(playerWon? retriesText.text : 3)}\n" +
                              $"Completion: {maxPercentageReached}%";

        // 4. Freeze the game so the ball stops rolling in the background
        Time.timeScale = 0f;
    }


}