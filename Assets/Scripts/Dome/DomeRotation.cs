using UnityEngine;

public class DomeRotation : MonoBehaviour
{
    [Header("Speed")]
    [Tooltip("Degrees per second. Negative turns the other way.")]
    public float degreesPerSecond = 1f;
    public float speedStep = 0.25f;
    public float maxSpeed = 5f;

    [Header("Keys")]
    public KeyCode pauseKey = KeyCode.Space;
    public KeyCode fasterKey = KeyCode.UpArrow;
    public KeyCode slowerKey = KeyCode.DownArrow;
    public KeyCode reverseKey = KeyCode.R;
    public KeyCode resetKey = KeyCode.Backspace;

    [Header("Smoothing")]
    [Tooltip("Seconds to ease between stopped and full speed. Avoids sudden jerks.")]
    public float easeTime = 2f;

    [Header("State (read only)")]
    public bool paused = false;

    float initialSpeed;
    float currentSpeed;   // the eased speed actually applied

    void Start()
    {
        initialSpeed = degreesPerSecond;
    }

    void Update()
    {
        if (Input.GetKeyDown(pauseKey)) paused = !paused;
        if (Input.GetKeyDown(fasterKey)) degreesPerSecond = Mathf.Min(degreesPerSecond + speedStep, maxSpeed);
        if (Input.GetKeyDown(slowerKey)) degreesPerSecond = Mathf.Max(degreesPerSecond - speedStep, -maxSpeed);
        if (Input.GetKeyDown(reverseKey)) degreesPerSecond = -degreesPerSecond;
        if (Input.GetKeyDown(resetKey)) { degreesPerSecond = initialSpeed; paused = false; }

        // Ease towards the target speed (0 when paused), so pausing and
        // changing speed never causes a sudden jolt on the dome.
        float target = paused ? 0f : degreesPerSecond;
        float maxDelta = easeTime > 0f
            ? (maxSpeed / easeTime) * Time.deltaTime
            : float.MaxValue;
        currentSpeed = Mathf.MoveTowards(currentSpeed, target, maxDelta);

        // Space.World keeps the horizon level (see earlier note).
        transform.Rotate(0f, currentSpeed * Time.deltaTime, 0f, Space.World);
    }

    // Optional: call these from other scripts or UI buttons.
    public void SetPaused(bool value) { paused = value; }
    public void TogglePause() { paused = !paused; }
    public void SetSpeed(float dps) { degreesPerSecond = Mathf.Clamp(dps, -maxSpeed, maxSpeed); }
}