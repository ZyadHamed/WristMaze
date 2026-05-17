using UnityEngine;

public class RandomBoardTilter : MonoBehaviour
{
    [Header("Random Tilt Settings")]
    [Tooltip("The maximum angle the board can tilt randomly in degrees (X and Z axis).")]
    public float maxTiltAngle = 3.0f; // Keep it low for simulation, e.g. ±3°

    [Tooltip("How quickly the random tilt changes.")]
    public float tiltChangeSpeed = 0.5f;

    // We store these to ensure smooth, continuous motion
    private Vector2 currentRandomOffset;
    private Vector2 targetRandomOffset;
    private float timeSinceLastTargetChange = 0f;
    private float targetChangeInterval = 2.0f; // Change target every 2 seconds

    private Quaternion initialRotation;

    void Start()
    {
        // Store the original local rotation so we always shift relative to 'flat'
        initialRotation = transform.localRotation;
    }

    void Update()
    {
        // 1. Every interval, generate a new random target 'gravity' vector
        timeSinceLastTargetChange += Time.deltaTime;
        if (timeSinceLastTargetChange >= targetChangeInterval)
        {
            // Generate a random vector within a circle, then scale it
            targetRandomOffset = Random.insideUnitCircle.normalized * maxTiltAngle;
            timeSinceLastTargetChange = 0f;
        }

        // 2. Smoothly move our 'current' offset towards the 'target' offset
        currentRandomOffset = Vector2.MoveTowards(currentRandomOffset, targetRandomOffset, tiltChangeSpeed * Time.deltaTime);

        // 3. Create the rotation. We use currentRandomOffset.y for X-tilt (forward/back)
        // and currentRandomOffset.x for Z-tilt (side-to-side), mapping to how a person tilts.
        Quaternion randomTiltRotation = Quaternion.Euler(currentRandomOffset.y, 0f, currentRandomOffset.x);

        // 4. Combine the initial rotation with the random tilt offset
        transform.localRotation = initialRotation * randomTiltRotation;
    }
}