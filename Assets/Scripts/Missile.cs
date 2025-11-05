using UnityEngine;

public class Missile : MonoBehaviour
{
    [Header("Missile Properties")]
    public MissileType missileType = MissileType.RadarGuided;
    public float maxSpeed = 800f; // km/h
    public float acceleration = 50f;
    public float turnRate = 3f; // How quickly it can turn
    public float maxLifetime = 30f;
    public float proximityDetonation = 5f; // Explodes within this distance
    public float fuelDuration = 15f; // Powered flight time
    
    [Header("Guidance")]
    public float lockStrength = 1f; // 0-1, how good the lock is
    public float guidanceDelay = 0.5f; // Delay before guidance starts
    public float maxTurnAngle = 180f; // Max angle per second
    
    [Header("Effects")]
    public GameObject explosionPrefab;
    public ParticleSystem smokeTrail;
    public Light missileLight;
    public float explosionRadius = 20f;
    
    [Header("Audio")]
    public AudioClip launchSound;
    public AudioClip flyingSound;
    public AudioClip explosionSound;
    
    private Transform target;
    private Rigidbody rb;
    private float currentSpeed;
    private float launchTime;
    private bool isGuiding;
    private bool hasFuel = true;
    private TrailRenderer trail;
    private AudioSource audioSource;
    private Vector3 lastTargetPosition;
    private bool targetLost = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        trail = GetComponent<TrailRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.maxDistance = 1000f;
    }
    
    
    public void Initialize(Transform launchPoint, Transform targetTransform, float lockQuality, Vector3 launchVelocity)
    {
        target = targetTransform;
        lockStrength = lockQuality;
        launchTime = Time.time;
        
        if (target != null)
        {
            lastTargetPosition = target.position;
        }
        
        transform.position = launchPoint.position;
        transform.rotation = launchPoint.rotation;
        
        // Calculate initial velocity: aircraft velocity + forward separation boost
        Vector3 initialVelocity = launchVelocity + launchPoint.forward * 30f;
        rb.linearVelocity = initialVelocity;
        
        // Set currentSpeed to match the initial velocity magnitude so FixedUpdate doesn't reset it
        currentSpeed = initialVelocity.magnitude;
        
        // Enable effects
        if (smokeTrail != null)
        {
            smokeTrail.Play();
        }
        
        if (missileLight != null)
        {
            missileLight.enabled = true;
        }
        
        // Play sounds
        if (launchSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(launchSound);
        }
        
        if (flyingSound != null && audioSource != null)
        {
            audioSource.clip = flyingSound;
            audioSource.loop = true;
            audioSource.Play();
        }
        
        Invoke(nameof(EnableGuidance), guidanceDelay);
        Invoke(nameof(SelfDestruct), maxLifetime);
    }
    
    private void EnableGuidance()
    {
        isGuiding = true;
    }
    
    private void FixedUpdate()
    {
        float timeSinceLaunch = Time.time - launchTime;
        
        // Fuel and propulsion logic
        if (timeSinceLaunch < fuelDuration)
        {
            hasFuel = true;
            currentSpeed += acceleration * Time.fixedDeltaTime;
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed / 3.6f); // Convert km/h to m/s
            
            // Keep smoke trail active
            if (smokeTrail != null && !smokeTrail.isPlaying)
            {
                smokeTrail.Play();
            }
        }
        else
        {
            // Out of fuel - coast mode
            if (hasFuel)
            {
                hasFuel = false;
                if (smokeTrail != null)
                {
                    smokeTrail.Stop();
                }
            }
            
            // Gradual deceleration from air resistance
            currentSpeed -= 8f * Time.fixedDeltaTime;
            currentSpeed = Mathf.Max(currentSpeed, 50f); // Minimum glide speed
        }
        
        // Guidance logic
        if (isGuiding)
        {
            Vector3 targetDirection;
            
            // Check if target still exists
            if (target != null)
            {
                lastTargetPosition = target.position;
                targetDirection = (target.position - transform.position).normalized;
                targetLost = false;
            }
            else if (!targetLost)
            {
                // Target lost, continue to last known position
                targetDirection = (lastTargetPosition - transform.position).normalized;
                targetLost = true;
            }
            else
            {
                // No target, maintain course
                targetDirection = transform.forward;
            }
            
            // Calculate lead for moving targets
            if (target != null)
            {
                Rigidbody targetRb = target.GetComponent<Rigidbody>();
                if (targetRb != null)
                {
                    float timeToIntercept = Vector3.Distance(transform.position, target.position) / currentSpeed;
                    Vector3 predictedPosition = target.position + targetRb.linearVelocity * timeToIntercept;
                    targetDirection = (predictedPosition - transform.position).normalized;
                }
            }
            
            // Proportional navigation guidance
            float maxTurnRateRadians = (turnRate * lockStrength * maxTurnAngle) * Mathf.Deg2Rad;
            Vector3 newDirection = Vector3.RotateTowards(
                transform.forward,
                targetDirection,
                maxTurnRateRadians * Time.fixedDeltaTime,
                0f
            );
            
            transform.rotation = Quaternion.LookRotation(newDirection);
            
            // Proximity check
            if (target != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.position);
                if (distanceToTarget < proximityDetonation)
                {
                    Detonate(true);
                    return;
                }
            }
        }
        
        // Apply velocity with slight gravity effect when out of fuel
        Vector3 velocity = transform.forward * currentSpeed;
        if (!hasFuel)
        {
            velocity += Physics.gravity * Time.fixedDeltaTime * 0.5f;
        }
        
        rb.linearVelocity = velocity;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        Detonate(collision.gameObject.CompareTag("Target"));
    }
    
    private void Detonate(bool hitTarget)
    {
        // Create explosion effect
        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, 5f);
        }
        
        // Play explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, 1f);
        }
        
        // Apply damage in radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hitCollider in hitColliders)
        {
            // Calculate damage based on distance
            float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
            float damageFalloff = 1f - (distance / explosionRadius);
            
            // Try to apply damage (if target has health component)
            // You can implement your own health system here
            if (hitTarget && hitCollider.transform == target)
            {
                Debug.Log($"Missile direct hit on {target.name}! Target Destroyed");
                Destroy(target.gameObject);
            }
          
          
        }
        
        Destroy(gameObject);
    }
    
    private void SelfDestruct()
    {
        Detonate(false);
    }
}

public enum MissileType
{
    HeatSeeking,
    RadarGuided
}
