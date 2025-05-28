using UnityEngine;

/// <summary>
/// Defines a zone that can modify the AISplineFollow parameters of a vehicle that enters it.
/// Attach this to a GameObject with a Collider set to 'Is Trigger'.
/// </summary>
public class AISplineModifierZone : MonoBehaviour
{
    [Header("Config - Check 'Override' to apply new value")]

    public bool overrideTargetSpeed = false;
    [Tooltip("New target speed in m/s.")]
    [Range(0f, 50f)] public float newTargetSpeed = 10.0f;

    public bool overrideLookAheadDistance = false;
    [Tooltip("New look ahead distance on the spline.")]
    [Range(0.5f, 20f)] public float newLookAheadDistance = 5.0f;

    public bool overrideSteerSensitivity = false;
    [Tooltip("New steering sensitivity.")]
    [Range(0.1f, 2f)] public float newSteerSensitivity = 0.8f;

    public bool overrideThrottleSensitivity = false;
    [Tooltip("New throttle sensitivity.")]
    [Range(0.1f, 2f)] public float newThrottleSensitivity = 0.5f;

    public bool overrideBrakeSensitivity = false;
    [Tooltip("New brake sensitivity.")]
    [Range(0.1f, 2f)] public float newBrakeSensitivity = 1.0f;

    public bool overrideMaxSteerAngleAtSpeed = false;
    [Tooltip("New maximum steer angle when at targetSpeed.")]
    [Range(1f, 90f)] public float newMaxSteerAngleAtSpeed = 15f;

    public bool overrideMinSteerAngleAtSpeed = false;
    [Tooltip("New minimum steer angle for slight corrections at speed.")]
    [Range(0.5f, 30f)] public float newMinSteerAngleAtSpeed = 5f;

    public bool overrideStoppingDistance = false;
    [Tooltip("New distance from end of spline to start braking (if not looping).")]
    [Range(0.5f, 20f)] public float newStoppingDistance = 3.0f;

    void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("AISplineModifierZone requires a Collider component.", this);
            enabled = false;
            return;
        }
        if (!col.isTrigger)
        {
            Debug.LogWarning($"Collider on AISplineModifierZone '{gameObject.name}' is not set to 'Is Trigger'. Forcing it to true.", this);
            col.isTrigger = true;
        }
    }

    void OnDrawGizmos()
    {
        // Draw a semi-transparent box or sphere in the editor to visualize the zone
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.3f); // Light green, semi-transparent
            if (col is BoxCollider boxCollider)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            }
            else if (col is SphereCollider sphereCollider)
            {
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawSphere(sphereCollider.center, sphereCollider.radius);
            }
            else
            {
                // Fallback for other collider types if needed, or just draw a small sphere at the center
                Gizmos.DrawSphere(transform.position, 0.5f);
            }
        }
    }
}