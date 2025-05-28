using UnityEngine;
using UnityEngine.Splines; // Required for SplineContainer and SplineUtility
using EVP; // Required for VehicleController
using Unity.Mathematics; // Required for float3 and math.normalize

[RequireComponent(typeof(VehicleController))]
public class AISplineFollow : MonoBehaviour
{
    [Header("Spline Settings")]
    [SerializeField] public SplineContainer splineContainer;
    [SerializeField] private float lookAheadDistance = 5.0f;
    [SerializeField] private float waypointProximityThreshold = 1.0f;

    [Header("Vehicle Control Settings")]
    [SerializeField] private float targetSpeed = 15.0f;
    [SerializeField] private float steerSensitivity = 0.8f;
    [SerializeField] private float throttleSensitivity = 0.5f;
    [SerializeField] private float brakeSensitivity = 1.0f;
    [SerializeField] private float reverseThrottleMultiplier = 0.3f;

    [Header("AI Behavior")]
    [SerializeField] private bool loopSpline = true;
    [SerializeField] private float maxSteerAngleAtSpeed = 15f;
    [SerializeField] private float minSteerAngleAtSpeed = 5f;
    [SerializeField] private float stoppingDistance = 3.0f;

    private VehicleController vehicleController;
    private ObstacleDeduction obstacleDetector;
    private Spline spline;
    private float currentDistanceOnSpline;
    private float totalSplineLength;
    private bool isReversing;

    // --- NEW: Variables to store original settings ---
    private float originalTargetSpeed;
    private float originalLookAheadDistance;
    private float originalSteerSensitivity;
    private float originalThrottleSensitivity;
    private float originalBrakeSensitivity;
    private float originalMaxSteerAngleAtSpeed;
    private float originalMinSteerAngleAtSpeed;
    private float originalStoppingDistance;
    private bool originalSettingsStored;
    // --- END NEW ---

    void Start()
    {
        vehicleController = GetComponent<VehicleController>();

        // Ensure the vehicle has a Rigidbody for trigger detection
        if (GetComponent<Rigidbody>() == null)
        {
            Debug.LogError("AISplineFollow requires a Rigidbody component on the vehicle for trigger detection.", this);
            enabled = false;
            return;
        }
        // Ensure the vehicle has some form of collider
        if (GetComponent<Collider>() == null)
        {
             Debug.LogWarning("AISplineFollow's GameObject typically has a Collider. Ensure collision layers are set up correctly for triggers if issues occur.", this);
        }


        if (splineContainer == null)
        {
            Debug.LogError("SplineContainer not assigned in AISplineFollow.", this);
            enabled = false;
            return;
        }

        if (splineContainer.Spline == null)
        {
            Debug.LogError("SplineContainer does not contain a valid Spline.", this);
            enabled = false;
            return;
        }
        spline = splineContainer.Spline;
        totalSplineLength = spline.GetLength();

        if (totalSplineLength < 0.1f)
        {
            Debug.LogError("Spline length is too short.", this);
            enabled = false;
            return;
        }
        
        StoreOriginalSettings();
        
        // Initialize position to the start of the spline (optional, if you want to snap it)
        // transform.position = (float3)splineContainer.EvaluatePosition(0);
        // transform.rotation = Quaternion.LookRotation(math.normalize(splineContainer.EvaluateTangent(0)));
        // currentDistanceOnSpline = 0;
        
        CheckIfStartingInZone();
        
        obstacleDetector = GetComponent<ObstacleDeduction>();
        if (obstacleDetector == null)
        {
            Debug.LogWarning("ObstacleDeduction component not found on this GameObject. Obstacle avoidance will not function.", this);
        }
    }

    // --- NEW: Method to store original settings ---
    private void StoreOriginalSettings()
    {
        if (originalSettingsStored) return;

        originalTargetSpeed = targetSpeed;
        originalLookAheadDistance = lookAheadDistance;
        originalSteerSensitivity = steerSensitivity;
        originalThrottleSensitivity = throttleSensitivity;
        originalBrakeSensitivity = brakeSensitivity;
        originalMaxSteerAngleAtSpeed = maxSteerAngleAtSpeed;
        originalMinSteerAngleAtSpeed = minSteerAngleAtSpeed;
        originalStoppingDistance = stoppingDistance;
        originalSettingsStored = true;
        // Debug.Log("AI original settings stored.");
    }
    // --- END NEW ---

    // --- NEW: Method to restore original settings ---
    private void RestoreOriginalSettings()
    {
        if (!originalSettingsStored) StoreOriginalSettings(); // Ensure they were stored

        targetSpeed = originalTargetSpeed;
        lookAheadDistance = originalLookAheadDistance;
        steerSensitivity = originalSteerSensitivity;
        throttleSensitivity = originalThrottleSensitivity;
        brakeSensitivity = originalBrakeSensitivity;
        maxSteerAngleAtSpeed = originalMaxSteerAngleAtSpeed;
        minSteerAngleAtSpeed = originalMinSteerAngleAtSpeed;
        stoppingDistance = originalStoppingDistance;
        // Debug.Log("AI settings restored to original.");
    }
    // --- END NEW ---

    // --- NEW: Method to apply settings from a zone ---
    private void ApplyZoneSettings(AISplineModifierZone zone)
    {
        if (zone == null) return;
        if (!originalSettingsStored) StoreOriginalSettings(); // Ensure originals are backed up before overriding

        if (zone.overrideTargetSpeed) targetSpeed = zone.newTargetSpeed;
        if (zone.overrideLookAheadDistance) lookAheadDistance = zone.newLookAheadDistance;
        if (zone.overrideSteerSensitivity) steerSensitivity = zone.newSteerSensitivity;
        if (zone.overrideThrottleSensitivity) throttleSensitivity = zone.newThrottleSensitivity;
        if (zone.overrideBrakeSensitivity) brakeSensitivity = zone.newBrakeSensitivity;
        if (zone.overrideMaxSteerAngleAtSpeed) maxSteerAngleAtSpeed = zone.newMaxSteerAngleAtSpeed;
        if (zone.overrideMinSteerAngleAtSpeed) minSteerAngleAtSpeed = zone.newMinSteerAngleAtSpeed;
        if (zone.overrideStoppingDistance) stoppingDistance = zone.newStoppingDistance;
        // Debug.Log($"AI settings modified by zone: {zone.gameObject.name}");
    }
    // --- END NEW ---

    // --- NEW: Check if starting inside a zone ---
    private void CheckIfStartingInZone()
    {
        Collider[] overlappingColliders = Physics.OverlapSphere(transform.position, 0.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        foreach (var col in overlappingColliders)
        {
            AISplineModifierZone zone = col.GetComponent<AISplineModifierZone>();
            // Check if the collider is a trigger and belongs to a zone.
            // This check ensures we are responding to the zone's trigger, not the vehicle's own collider if it were also a trigger.
            if (zone != null && col.isTrigger)
            {
                Debug.Log($"AI started inside modifier zone: {zone.gameObject.name}. Applying initial settings from zone.");
                ApplyZoneSettings(zone);
                break; // Apply first one found and exit
            }
        }
    }
    // --- END NEW ---

    void Update()
    {
        if (spline == null || vehicleController == null || totalSplineLength == 0)
        {
            if (vehicleController != null)
            {
                vehicleController.steerInput = 0;
                vehicleController.throttleInput = 0;
                vehicleController.brakeInput = 1;
            }
            return;
        }
        
        bool isObstacleDirectlyAhead = false;
        if (obstacleDetector != null && obstacleDetector.enabled) // Check if detector exists and is active
        {
            isObstacleDirectlyAhead = obstacleDetector.IsObstacleInPath();
        }

        if (isObstacleDirectlyAhead)
        {
            // Obstacle detected: STOP the vehicle!
            // Debug.Log("AISplineFollow: Obstacle detected by ObstacleDeduction script! Applying brakes.");
            vehicleController.steerInput = 0f; // Stop steering or maintain current steering based on desired behavior. 0f is a safe stop.
            vehicleController.throttleInput = 0f;
            vehicleController.brakeInput = 1.0f; // Apply full brakes
            return; // Skip the rest of the spline following logic for this frame
        }

        // --- Calculate Target on Spline ---
        float lookAheadTargetDistance = currentDistanceOnSpline + lookAheadDistance;

        if (lookAheadTargetDistance >= totalSplineLength)
        {
            if (loopSpline)
            {
                lookAheadTargetDistance %= totalSplineLength;
            }
            else
            {
                lookAheadTargetDistance = totalSplineLength;
                if (totalSplineLength - currentDistanceOnSpline < stoppingDistance)
                {
                    ApplyBrakingToStop();
                    return;
                }
            }
        }
        
        float targetTime = Mathf.Clamp01(lookAheadTargetDistance / totalSplineLength); // Ensure t is between 0 and 1
        Vector3 targetPosition = splineContainer.EvaluatePosition(targetTime);
        float3 tangentFloat3 = splineContainer.EvaluateTangent(targetTime);
        Vector3 targetDirection = math.normalize(tangentFloat3);

        // --- Steering Control ---
        Vector3 directionToTarget = targetPosition - transform.position;
        float angleToTarget = Vector3.SignedAngle(transform.forward, directionToTarget.normalized, transform.up);

        float currentSpeed = vehicleController.speed;
        float speedFactor = Mathf.Clamp01(currentSpeed / Mathf.Max(0.1f, targetSpeed)); // Avoid division by zero if targetSpeed is 0
        float dynamicMaxSteer = Mathf.Lerp(vehicleController.maxSteerAngle, maxSteerAngleAtSpeed, speedFactor);
        
        if (dynamicMaxSteer < 0.01f) dynamicMaxSteer = 0.01f;

        float steerInput = Mathf.Clamp(angleToTarget * steerSensitivity / dynamicMaxSteer, -1.0f, 1.0f);
        
        if (Mathf.Abs(angleToTarget) < minSteerAngleAtSpeed && directionToTarget.magnitude > lookAheadDistance * 0.5f)
        {
             steerInput = Mathf.Clamp(angleToTarget / Mathf.Max(0.1f, minSteerAngleAtSpeed), -1.0f, 1.0f) * steerSensitivity * 0.5f;
        }
        else if (Mathf.Abs(angleToTarget) < 1.0f)
        {
            steerInput *= 0.1f;
        }

        // --- Throttle and Brake Control ---
        float throttleInput;
        float brakeInput;
        float dotProduct = Vector3.Dot(transform.forward, directionToTarget.normalized);

        if (isReversing)
        {
            if (dotProduct > 0.9f && directionToTarget.magnitude < waypointProximityThreshold * 0.5f)
            {
                isReversing = false;
                throttleInput = 0;
                brakeInput = 1;
            }
            else if (dotProduct < -0.5f)
            {
                 throttleInput = -reverseThrottleMultiplier * throttleSensitivity;
                 brakeInput = 0;
            }
            else
            {
                isReversing = false;
                throttleInput = 0;
                brakeInput = 1;
            }
        }
        else
        {
            if (dotProduct < -0.5f && directionToTarget.magnitude > waypointProximityThreshold * 2f)
            {
                brakeInput = 1.0f;
                throttleInput = 0;
                // Simple reverse attempt: if nearly stopped and still facing wrong way badly
                if(currentSpeed < 0.5f && directionToTarget.magnitude > lookAheadDistance * 0.5f)
                {
                    // isReversing = true; // Uncomment this line to enable basic reversing logic
                                       // Be cautious, as it might need more refinement depending on spline complexity
                }
            }
            else if (dotProduct < 0.3f && currentSpeed > targetSpeed * 0.3f)
            {
                throttleInput = 0.0f;
                brakeInput = Mathf.Lerp(0, brakeSensitivity * 0.7f, Mathf.Abs(angleToTarget) / Mathf.Max(0.1f, dynamicMaxSteer));
            }
            else
            {
                if (currentSpeed < targetSpeed)
                {
                    float throttleReduction = Mathf.Clamp01(Mathf.Abs(angleToTarget) / (Mathf.Max(0.1f, dynamicMaxSteer) * 1.5f));
                    throttleInput = (1.0f - throttleReduction) * throttleSensitivity;
                    brakeInput = 0;
                }
                else
                {
                    throttleInput = 0;
                    float overSpeedFactor = Mathf.Clamp01((currentSpeed - targetSpeed) / (Mathf.Max(0.1f, targetSpeed) * 0.2f));
                    float turnBrakeFactor = Mathf.Clamp01(Mathf.Abs(angleToTarget) / Mathf.Max(0.1f, dynamicMaxSteer));
                    brakeInput = Mathf.Max(overSpeedFactor, turnBrakeFactor * 0.5f) * brakeSensitivity;
                }
            }
        }

        vehicleController.steerInput = steerInput;
        vehicleController.throttleInput = throttleInput;
        vehicleController.brakeInput = brakeInput;

        // --- Advance Position on Spline ---
        float actualSpeedAlongSpline = Vector3.Dot(vehicleController.cachedRigidbody.linearVelocity, transform.forward);
        currentDistanceOnSpline += actualSpeedAlongSpline * Time.deltaTime;

        if (currentDistanceOnSpline >= totalSplineLength)
        {
            if (loopSpline)
            {
                currentDistanceOnSpline -= totalSplineLength;
            }
            else
            {
                currentDistanceOnSpline = totalSplineLength;
            }
        }
        else if (currentDistanceOnSpline < 0)
        {
             if (loopSpline)
            {
                currentDistanceOnSpline += totalSplineLength;
            }
            else
            {
                currentDistanceOnSpline = 0;
            }
        }

        Debug.DrawLine(transform.position, targetPosition, Color.green);
        Debug.DrawRay(targetPosition, targetDirection * 2.0f, Color.blue);
    }

    void ApplyBrakingToStop()
    {
        vehicleController.steerInput = 0;
        vehicleController.throttleInput = 0;
        vehicleController.brakeInput = 1.0f;
    }

    // --- NEW: OnTriggerEnter and OnTriggerExit for modifier zones ---
    void OnTriggerEnter(Collider other)
    {
        AISplineModifierZone zone = other.GetComponent<AISplineModifierZone>();
        if (zone != null && other.isTrigger) // Ensure it's a trigger collider from a zone
        {
            Debug.Log($"Vehicle entered AI Modifier Zone: {other.gameObject.name}");
            ApplyZoneSettings(zone);
        }
    }

    void OnTriggerExit(Collider other)
    {
        AISplineModifierZone zone = other.GetComponent<AISplineModifierZone>();
        if (zone != null && other.isTrigger) // Ensure it's a trigger collider from a zone
        {
            Debug.Log($"Vehicle exited AI Modifier Zone: {other.gameObject.name}. Reverting to original settings.");
            RestoreOriginalSettings();
        }
    }
    // --- END NEW ---

    void OnDrawGizmosSelected()
    {
        if (splineContainer != null && splineContainer.Spline != null && enabled && totalSplineLength > 0)
        {
            float lookAheadT = (currentDistanceOnSpline + lookAheadDistance) / totalSplineLength;
            if (loopSpline && lookAheadT >=1f) lookAheadT %=1f;
            lookAheadT = Mathf.Clamp01(lookAheadT);

            Vector3 lookAheadPos = splineContainer.EvaluatePosition(lookAheadT);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(lookAheadPos, 0.5f);

            float currentT = currentDistanceOnSpline / totalSplineLength;
            currentT = Mathf.Clamp01(currentT);
            Vector3 currentPosOnSpline = splineContainer.EvaluatePosition(currentT);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(currentPosOnSpline, 0.4f);
            Gizmos.DrawLine(transform.position, currentPosOnSpline);
        }
    }
}