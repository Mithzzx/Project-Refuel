using UnityEngine;
using System.Collections.Generic;
using EVP;

[ExecuteInEditMode]
public class ObstacleDeduction : MonoBehaviour
{
    [Header("Obstacle Detection Settings")]
    [Tooltip("Tag used to identify dynamic obstacles.")]
    [SerializeField] private string obstacleTag = "Obstacle";

    [Tooltip("How far ahead the AI looks for obstacles.")]
    [SerializeField] private float detectionDistance = 10.0f;

    [Tooltip("Offset from the vehicle's pivot point for the raycast origin.")]
    [SerializeField] private Vector3 raycastOriginOffset = new Vector3(0, 0.5f, 0.5f);

    [Tooltip("Optional custom transform to use as the ray origin instead of this object.")]
    [SerializeField] private Transform customRayOrigin;

    [Header("Multi-Ray Configuration")]
    [Tooltip("Number of rays to cast. 1 = single forward ray only.")]
    [Range(1, 10)]
    [SerializeField] private int numberOfRays = 1;

    [Tooltip("Total angular spread of the rays in degrees (only applies when using multiple rays).")]
    [Range(0, 180)]
    [SerializeField] private float raySpreadAngle = 45f;

    [Tooltip("Layers that the raycast should interact with.")]
    [SerializeField] private LayerMask obstacleLayerMask;
    

    private bool isObstacleCurrentlyDetected;
    private Transform lastDetectedObstacleTransform;
    private List<RaycastHit> currentHits = new List<RaycastHit>();

    void Start()
    {
        if (string.IsNullOrEmpty(obstacleTag))
        {
            Debug.LogError("Obstacle Tag is not set in ObstacleDeduction. Obstacle detection will not function.", this);
            enabled = false;
            return;
        }

        if (obstacleLayerMask.value == 0)
        {
            Debug.LogWarning("Obstacle Layer Mask in ObstacleDeduction is set to 'Nothing'. Raycast will not hit any obstacles.", this);
        }
        else if (obstacleLayerMask.value == -1)
        {
            Debug.LogWarning("Obstacle Layer Mask in ObstacleDeduction is set to 'Everything'. This is functional but could be inefficient.", this);
        }

        PerformDetectionLogic();
    }

    void Update()
    {
        PerformDetectionLogic();
    }

    private void PerformDetectionLogic()
    {
        bool obstacleDetected = false;
        Transform detectedObstacle = null;
        currentHits.Clear();

        // Get the ray origin position
        Transform originTransform = customRayOrigin != null ? customRayOrigin : transform;
        Vector3 worldRayOrigin = originTransform.position + originTransform.TransformDirection(raycastOriginOffset);

        if (numberOfRays == 1)
        {
            // Single ray case
            RaycastHit hit;
            if (Physics.Raycast(worldRayOrigin, originTransform.forward, out hit, detectionDistance, obstacleLayerMask))
            {
                if (hit.collider.CompareTag(obstacleTag))
                {
                    VehicleController hitVehicle = hit.collider.GetComponent<VehicleController>();
                    if(hitVehicle)
                    {
                       if (hitVehicle.speed >= 0.1f) obstacleDetected = true;
                    }
                    
                    detectedObstacle = hit.transform;
                    currentHits.Add(hit);
                }
            }
        }
        else
        {
            // Multiple rays case
            for (int i = 0; i < numberOfRays; i++)
            {
                // Calculate ray angle based on spread configuration
                float angleStep = raySpreadAngle / (numberOfRays - 1);
                float currentAngle = -raySpreadAngle / 2 + (i * angleStep);

                // Create the ray direction based on spread mode
                Vector3 rayDirection = originTransform.forward;
                
                rayDirection = Quaternion.AngleAxis(currentAngle, originTransform.up) * rayDirection;

                RaycastHit hit;
                if (Physics.Raycast(worldRayOrigin, rayDirection, out hit, detectionDistance))
                {
                    Debug.Log(hit.collider.name);
                    if (hit.collider.CompareTag(obstacleTag))
                    {
                        obstacleDetected = true;
                        detectedObstacle = hit.transform;
                        currentHits.Add(hit);
                    }
                }
            }
        }

        // Update internal state
        isObstacleCurrentlyDetected = obstacleDetected;
        lastDetectedObstacleTransform = detectedObstacle;
    }

    public bool IsObstacleInPath()
    {
        // We'll reuse the existing detection state for efficiency
        return isObstacleCurrentlyDetected;
    }

    public RaycastHit[] GetCurrentHits()
    {
        return currentHits.ToArray();
    }

    void OnDrawGizmosSelected()
    {
        Transform originTransform = customRayOrigin != null ? customRayOrigin : transform;
        Vector3 worldRayOrigin = originTransform.position + originTransform.TransformDirection(raycastOriginOffset);

        if (numberOfRays == 1)
        {
            // Single ray visualization
            DrawRayGizmo(worldRayOrigin, originTransform.forward);
        }
        else
        {
            // Multiple rays visualization
            for (int i = 0; i < numberOfRays; i++)
            {
                float angleStep = raySpreadAngle / (numberOfRays - 1);
                float currentAngle = -raySpreadAngle / 2 + (i * angleStep);

                Vector3 rayDirection = originTransform.forward;
                
                rayDirection = Quaternion.AngleAxis(currentAngle, originTransform.up) * rayDirection;

                DrawRayGizmo(worldRayOrigin, rayDirection);
            }
        }
    }

    private void DrawRayGizmo(Vector3 origin, Vector3 direction)
    {
        bool hitObstacle = false;
        float actualDistance = detectionDistance;

        RaycastHit gizmoHit;
        if (Physics.Raycast(origin, direction, out gizmoHit, detectionDistance, obstacleLayerMask))
        {
            if (gizmoHit.collider.CompareTag(obstacleTag))
            {
                hitObstacle = true;
                actualDistance = gizmoHit.distance;
            }
        }

        Gizmos.color = hitObstacle ? Color.red : Color.green;
        Gizmos.DrawLine(origin, origin + direction * actualDistance);
        
        if (hitObstacle)
        {
            Gizmos.DrawSphere(origin + direction * actualDistance, 0.3f);
        }
    }
}