using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

public class BoardTilter : MonoBehaviour
{
    private DatabaseReference sensorRef;
    private DatabaseReference isActiveRef;

    // Variables to hold your real-time data
    public float sensorX;
    public float sensorY;
    public float sensorZ;
    public string lastUpdateTime;

    public float sensitivity = 1.5f;

    public Rigidbody ball;
    public float responsiveness = 50f;

    // Determines how aggressively it fights inertia (higher = snappier)
    public float accelerationRate = 10f;

    // Optional: How fast the board smooths to the new angle (higher = snappier, lower = smoother)
    public float tiltSmoothness = 15f;

    public bool isActive;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }

    void InitializeFirebase()
    {
        sensorRef = FirebaseDatabase.DefaultInstance.GetReference("sensor");

        isActiveRef = FirebaseDatabase.DefaultInstance.GetReference("gameState").Child("isActive");

        isActiveRef.OnDisconnect().SetValue(false);
        isActiveRef.SetValueAsync(true);

        sensorRef.ValueChanged += HandleValueChanged;
        Debug.Log("Firebase Initialized and listening to 'sensor' node.");
    }

    void HandleValueChanged(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        DataSnapshot snapshot = args.Snapshot;

        // Make sure all data exists before parsing to avoid null errors
        if (snapshot != null && snapshot.Exists && snapshot.HasChild("x") && snapshot.HasChild("y") && snapshot.HasChild("z"))
        {
            string rollStr = snapshot.Child("x").Value.ToString();
            string pitchStr = snapshot.Child("y").Value.ToString();
            string yawStr = snapshot.Child("z").Value.ToString();

            if (snapshot.HasChild("lastUpdate"))
            {
                lastUpdateTime = snapshot.Child("lastUpdate").Value.ToString();
            }

            // 1. Parse the phone's native orientation
            float rawRoll = float.Parse(rollStr, System.Globalization.CultureInfo.InvariantCulture);
            float rawPitch = float.Parse(pitchStr, System.Globalization.CultureInfo.InvariantCulture);
            float rawYaw = float.Parse(yawStr, System.Globalization.CultureInfo.InvariantCulture);

            // 2. Map Phone Axes to Unity Axes
            // Pitch (forward/back) -> Unity X axis
            sensorX = -1 * Mathf.Clamp(rawPitch, -60f, 60f) / 10f * sensitivity;

            // Yaw (compass spin) -> Unity Y axis 
            // Note: If you don't want the board to spin horizontally like a steering wheel, set this to 0f
            sensorY = 0f;

            // Roll (left/right) -> Unity Z axis
            // Note: Phones and Unity often have inverted Z axes. If tilting left makes the board tilt right, change this to: -rawRoll
            sensorZ = Mathf.Clamp(rawRoll, -60f, 60f) / 10f * sensitivity;
        }
    }

    // Update handles Visuals and Input (runs every frame)
    void Update()
    {
        Quaternion targetRotation = Quaternion.Euler(sensorX, sensorY, sensorZ);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * tiltSmoothness);
    }

    // FixedUpdate handles all Physics calculations (runs in sync with the physics engine)
    void FixedUpdate()
    {
        if (ball != null)
        {
            // 1. Get the downhill direction based on the board's tilt
            Vector3 slopeDirection = new Vector3(transform.up.x, 0, transform.up.z);

            // 2. Calculate the exact, instant velocity. 
            // We keep ball.linearVelocity.y so gravity still pulls it downwards perfectly.
            Vector3 instantVelocity = new Vector3(
                slopeDirection.x * responsiveness,
                ball.linearVelocity.y,
                slopeDirection.z * responsiveness
            );

            // 3. Force the velocity. No forces, no acceleration, no inertia.
            ball.linearVelocity = instantVelocity;
        }
    }

    void OnApplicationQuit()
    {
        if (isActiveRef != null)
        {
            isActiveRef.OnDisconnect().Cancel();
            isActiveRef.SetValueAsync(false);
        }
    }

    void OnDestroy()
    {
        if (sensorRef != null)
        {
            sensorRef.ValueChanged -= HandleValueChanged;
        }
    }
}