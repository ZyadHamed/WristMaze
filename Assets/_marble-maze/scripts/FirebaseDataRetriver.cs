using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

public class FirebaseDataRetriver : MonoBehaviour
{
    private DatabaseReference sensorRef;

    // Variables to hold your real-time data for use in game logic
    public float sensorX;
    public float sensorY;
    public float sensorZ;
    public string lastUpdateTime;

    void Start()
    {
        // 1. Check dependencies ONCE at the start of the application
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Firebase is ready, initialize the database connection
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
        // 2. Get a reference specifically to the "sensor" node
        sensorRef = FirebaseDatabase.DefaultInstance.GetReference("sensor");

        // 3. Attach a listener. This triggers immediately once, and then every time the data changes.
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

        if (snapshot != null && snapshot.Exists)
        {
            // 4. Extract the string values from the database
            string xStr = snapshot.Child("x").Value.ToString();
            string yStr = snapshot.Child("y").Value.ToString();
            string zStr = snapshot.Child("z").Value.ToString();
            lastUpdateTime = snapshot.Child("lastUpdate").Value.ToString();

            // 5. Parse the strings into floats so you can use them for movement/physics
            // Using InvariantCulture ensures decimals work regardless of the computer's regional settings
            sensorX = float.Parse(xStr, System.Globalization.CultureInfo.InvariantCulture);
            sensorY = float.Parse(yStr, System.Globalization.CultureInfo.InvariantCulture);
            sensorZ = float.Parse(zStr, System.Globalization.CultureInfo.InvariantCulture);

            // Log the formatted output
            Debug.Log($"Real-time Reading -> Time: {lastUpdateTime} | X: {sensorX}, Y: {sensorY}, Z: {sensorZ}");
        }
    }

    void Update()
    {
        // Now you can safely use sensorX, sensorY, and sensorZ here every frame!
        // Example: transform.rotation = Quaternion.Euler(sensorX, sensorY, sensorZ);
    }

    void OnDestroy()
    {
        // 6. Clean up the listener when the object is destroyed to prevent memory leaks
        if (sensorRef != null)
        {
            sensorRef.ValueChanged -= HandleValueChanged;
        }
    }
}