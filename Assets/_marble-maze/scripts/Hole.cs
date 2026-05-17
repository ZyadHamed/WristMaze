using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hole : MonoBehaviour
{
    public static int fallCount = 0;

    // A static Vector3 to hold the exact coordinates of where to respawn.
    // Static means ALL holes share this one location.
    public static Vector3 currentRespawnPosition;

    void Start()
    {
        // Set the initial respawn position to the START object
        GameObject startObject = GameObject.Find("START");
        if (startObject != null)
        {
            currentRespawnPosition = startObject.transform.position;
        }
        else
        {
            Debug.LogError("Could not find an object named 'START'! Please create one.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        string otherName = other.gameObject.transform.name;
        if (otherName.Equals("Marble"))
        {
            fallCount++;
            Debug.Log($"Marble fell in a hole! Total falls: {fallCount}");

            if (fallCount >= 3)
            {
                Debug.Log("Game Over! 3 strikes reached.");

                CheckPoint checkpointManager = FindObjectOfType<CheckPoint>();
                if (checkpointManager != null)
                {
                    checkpointManager.TriggerEndScreen(false);
                }
            }
            else
            {
                Debug.Log($"Strike {fallCount}! Respawning at last checkpoint...");

                // 1. Move the marble to the active respawn position.
                // We add a tiny bit to the Y-axis (Up) so it drops slightly and doesn't get stuck in the floor.
                other.transform.position = currentRespawnPosition + new Vector3(0f, 0.5f, 0f);

                // 2. Kill all momentum so it doesn't keep falling through the floor
                Rigidbody marbleRb = other.GetComponent<Rigidbody>();
                if (marbleRb != null)
                {
                    marbleRb.linearVelocity = Vector3.zero;
                    marbleRb.angularVelocity = Vector3.zero;
                }
            }
        }
    }
}