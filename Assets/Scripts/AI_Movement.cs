using UnityEngine;

public class AI_Movement : MonoBehaviour
{
    [Header("Flight Settings")]
    [Tooltip("Base speed of the AI aircraft")]
    public float baseSpeed = 80f;
    
    [Tooltip("How fast the AI can turn")]
    public float turnSpeed = 2f;
    
    [Tooltip("Random speed variation range")]
    public float speedVariation = 20f;
    
    [Header("Waypoint Settings")]
    [Tooltip("Minimum distance from current position for new waypoint")]
    public float minWaypointDistance = 200f;
    
    [Tooltip("Maximum distance from current position for new waypoint")]
    public float maxWaypointDistance = 500f;
    
    [Tooltip("Distance from waypoint before selecting a new one")]
    public float waypointReachThreshold = 50f;
    
    [Tooltip("Minimum altitude the AI will fly at")]
    public float minAltitude = 100f;
    
    [Tooltip("Maximum altitude the AI will fly at")]
    public float maxAltitude = 500f;
    
    [Header("Boundary Settings")]
    [Tooltip("Keep AI within this distance from spawn point (0 = no boundary)")]
    public float boundaryRadius = 2000f;
    
    [Header("Combat Settings")]
    [Tooltip("Should this AI track and attack the player?")]
    public bool enableCombatMode = false;
    
    [Tooltip("Detection range for player")]
    public float detectionRange = 1000f;
    
    [Tooltip("Reference to player transform (auto-detected if not set)")]
    public Transform playerTarget;
    
    private Vector3 currentWaypoint;
    private Rigidbody rb;
    private float currentSpeed;
    private Vector3 spawnPosition;
    private bool hasWaypoint = false;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // If no rigidbody, add one and configure it
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.mass = 10f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 2f;
        }
        
        spawnPosition = transform.position;
        currentSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);
        
        // Auto-detect player if not assigned and combat mode is enabled
        if (enableCombatMode && playerTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTarget = player.transform;
            }
        }
        
        GenerateNewWaypoint();
    }
    
    void Update()
    {
        // Check if we need a new waypoint
        if (hasWaypoint && Vector3.Distance(transform.position, currentWaypoint) < waypointReachThreshold)
        {
            GenerateNewWaypoint();
        }
        
        // If in combat mode and player is in range, target them instead
        if (enableCombatMode && playerTarget != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
            if (distanceToPlayer < detectionRange)
            {
                // Set player position as waypoint, but slightly ahead to intercept
                Vector3 interceptPoint = playerTarget.position + playerTarget.forward * 100f;
                currentWaypoint = new Vector3(
                    interceptPoint.x,
                    Mathf.Clamp(interceptPoint.y, minAltitude, maxAltitude),
                    interceptPoint.z
                );
            }
        }
    }
    
    void FixedUpdate()
    {
        if (!hasWaypoint) return;
        
        // Calculate direction to waypoint
        Vector3 directionToWaypoint = (currentWaypoint - transform.position).normalized;
        
        // Smoothly rotate towards waypoint
        Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
        
        // Apply forward thrust
        rb.AddForce(transform.forward * currentSpeed);
        
        // Apply lift based on speed (simple lift model)
        float liftForce = rb.linearVelocity.magnitude * 0.5f;
        rb.AddForce(Vector3.up * liftForce);
        
        // Limit max velocity
        if (rb.linearVelocity.magnitude > currentSpeed * 2f)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * currentSpeed * 2f;
        }
    }
    
    void GenerateNewWaypoint()
    {
        Vector3 randomDirection = Random.onUnitSphere;
        float distance = Random.Range(minWaypointDistance, maxWaypointDistance);
        Vector3 potentialWaypoint = transform.position + randomDirection * distance;
        
        // Clamp altitude
        potentialWaypoint.y = Mathf.Clamp(potentialWaypoint.y, minAltitude, maxAltitude);
        
        // If boundary is set, keep waypoint within boundary from spawn point
        if (boundaryRadius > 0)
        {
            Vector3 directionFromSpawn = potentialWaypoint - spawnPosition;
            if (directionFromSpawn.magnitude > boundaryRadius)
            {
                // Pull waypoint back inside boundary
                potentialWaypoint = spawnPosition + directionFromSpawn.normalized * (boundaryRadius * 0.8f);
                potentialWaypoint.y = Mathf.Clamp(potentialWaypoint.y, minAltitude, maxAltitude);
            }
        }
        
        currentWaypoint = potentialWaypoint;
        hasWaypoint = true;
        
        // Randomly vary speed for each new waypoint
        currentSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);
    }
    
    // Optional: Visualize waypoint in editor
    void OnDrawGizmos()
    {
        if (hasWaypoint)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentWaypoint, 20f);
            Gizmos.DrawLine(transform.position, currentWaypoint);
        }
        
        if (boundaryRadius > 0)
        {
            Gizmos.color = Color.red;
            Vector3 center = Application.isPlaying ? spawnPosition : transform.position;
            Gizmos.DrawWireSphere(center, boundaryRadius);
        }
        
        if (enableCombatMode && detectionRange > 0)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
    
    // Public methods for external control
    public void SetTarget(Vector3 target)
    {
        currentWaypoint = target;
        hasWaypoint = true;
    }
    
    public void SetCombatMode(bool enabled)
    {
        enableCombatMode = enabled;
    }
}
