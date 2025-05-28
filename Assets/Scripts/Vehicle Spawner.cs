using UnityEngine;
using UnityEngine.Splines;

public class VehicleSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("Prefab of the vehicle AI to spawn.")]
    [SerializeField] private GameObject vehiclePrefab;

    [Tooltip("List of available spline containers to assign.")]
    [SerializeField] private SplineContainer[] splineContainers;

    [Tooltip("Time interval between spawns (in seconds).")]
    [SerializeField] private float spawnInterval = 5f;

    [Tooltip("Maximum number of vehicles to spawn.")]
    [SerializeField] private int maxVehicles = 10;

    private int currentVehicleCount = 0;

    void Start()
    {
        if (vehiclePrefab == null)
        {
            Debug.LogError("Vehicle prefab is not assigned.", this);
            enabled = false;
            return;
        }

        if (splineContainers == null || splineContainers.Length == 0)
        {
            Debug.LogError("No spline containers assigned.", this);
            enabled = false;
            return;
        }

        // Start spawning vehicles at regular intervals
        InvokeRepeating(nameof(SpawnVehicle), 0f, spawnInterval);
    }

    private void SpawnVehicle()
    {
        if (currentVehicleCount >= maxVehicles)
            return;

        // Instantiate the vehicle at the spawner's position and rotation
        GameObject vehicle = Instantiate(vehiclePrefab, transform.position, transform.rotation);
        currentVehicleCount++;

        // Assign a random spline container to the vehicle
        AISplineFollow aiSplineFollow = vehicle.GetComponent<AISplineFollow>();
        if (aiSplineFollow != null)
        {
            SplineContainer randomSpline = splineContainers[Random.Range(0, splineContainers.Length)];
            aiSplineFollow.splineContainer = randomSpline;
        }
        else
        {
            Debug.LogWarning("Spawned vehicle does not have an AISplineFollow component.", vehicle);
        }
    }
}