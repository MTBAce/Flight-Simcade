using UnityEngine;
using System.Collections.Generic;

public class MissileSystem : MonoBehaviour
{
    [Header("Missile Configuration")]
    public GameObject missilePrefab;
    public Transform[] missileHardpoints; // Launch positions
    public int maxMissiles = 6;
    public MissileType defaultMissileType = MissileType.RadarGuided;
    
    [Header("Targeting")]
    public float maxLockRange = 5000f; // meters
    public float lockOnTime = 2f; // seconds to achieve full lock
    public float lockAngle = 30f; // degrees from nose
    public float lockBreakAngle = 45f; // Break lock if target moves outside this angle
    public LayerMask targetLayer;
    public KeyCode cycleMissileKey = KeyCode.N;
    public KeyCode cycleTargetKey = KeyCode.T;
    public KeyCode fireMissileKey = KeyCode.F;
    
    [Header("Audio")]
    public AudioClip lockingSound;
    public AudioClip lockedSound;
    public AudioClip launchSound;
    public AudioClip noLockSound;
    public AudioClip lockBreakSound;
    
    private int currentMissileCount;
    private Transform currentTarget;
    private float lockTimer;
    private bool isLocking;
    private bool isLocked;
    private int nextHardpointIndex;
    private AudioSource audioSource;
    private List<Transform> availableTargets = new List<Transform>();
    private int currentTargetIndex = 0;
    private FlightController flightController;
    private float lockingSoundTimer;
    private MissileType currentMissileType;
    
    private void Awake()
    {
        currentMissileCount = maxMissiles;
        currentMissileType = defaultMissileType;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // 2D sound for cockpit
        flightController = GetComponent<FlightController>();
    }
    
    private void Update()
    {
        UpdateTargeting();
        HandleMissileFiring();
        HandleTargetCycling();
        HandleMissileTypeSelection();
    }
    
    private void UpdateTargeting()
    {
        // Find potential targets in front of aircraft
        UpdateAvailableTargets();
        Transform bestTarget = FindBestTarget();
        
        // Auto-acquire target if none selected
        if (currentTarget == null && availableTargets.Count > 0)
        {
            currentTarget = FindBestTarget();
            currentTargetIndex = availableTargets.IndexOf(currentTarget);
            lockTimer = 0f;
            isLocking = true;
            isLocked = false;
        }
        
        // Check if current target is still valid
        if (currentTarget != null)
        {
            Vector3 directionToTarget = currentTarget.position - transform.position;
            float angle = Vector3.Angle(transform.forward, directionToTarget);
            float distance = directionToTarget.magnitude;
            
            // Break lock if target outside parameters
            if (angle > lockBreakAngle || distance > maxLockRange || !availableTargets.Contains(currentTarget))
            {
                // Lost target
                PlaySound(lockBreakSound);
                Debug.Log($"Lock broken on {currentTarget.name}");
                currentTarget = null;
                lockTimer = 0f;
                isLocking = false;
                isLocked = false;
            }
            else
            {
                // Continue locking on current target
                lockTimer += Time.deltaTime;
                
                if (lockTimer >= lockOnTime && isLocking)
                {
                    // Full lock achieved
                    isLocking = false;
                    isLocked = true;
                    PlaySound(lockedSound);
                    Debug.Log($"Target locked: {currentTarget.name}");
                }
                else if (isLocking)
                {
                    // Still locking
                    PlayLockingSound();
                }
            }
        }
    }

    private void UpdateAvailableTargets()
    {
        availableTargets.Clear();
        
        Collider[] potentialTargets = Physics.OverlapSphere(
            transform.position,
            maxLockRange,
            targetLayer
        );
        
        foreach (Collider col in potentialTargets)
        {
            if (col.transform != transform) // Don't target self
            {
                availableTargets.Add(col.transform);
            }
        }
    }
    
    private Transform FindBestTarget()
    {
        Collider[] potentialTargets = Physics.OverlapSphere(
            transform.position, 
            maxLockRange, 
            targetLayer
        );
        
        Transform bestTarget = null;
        float bestScore = float.MaxValue;
        
        foreach (Collider col in potentialTargets)
        {
            if (col.transform == transform) continue; // Skip self
            
            Vector3 directionToTarget = col.transform.position - transform.position;
            float angle = Vector3.Angle(transform.forward, directionToTarget);
            float distance = directionToTarget.magnitude;
            
            // Only consider targets within lock angle
            if (angle > lockAngle) continue;
            
            // Score based on angle and distance (prefer close and centered)
            float score = angle * 10f + distance * 0.1f;
            
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = col.transform;
            }
        }
        
        return bestTarget;
    }
    
    private void HandleTargetCycling()
    {
        if (Input.GetKeyDown(cycleTargetKey) && availableTargets.Count > 0)
        {
            currentTargetIndex = (currentTargetIndex + 1) % availableTargets.Count;
            currentTarget = availableTargets[currentTargetIndex];
            lockTimer = 0f;
            isLocking = true;
            isLocked = false;
            Debug.Log($"Cycling to target: {currentTarget.name}");
        }
    }
    
    private void HandleMissileTypeSelection()
    {
        if (Input.GetKeyDown(cycleMissileKey))
        {
            // Cycle between missile types
            if (currentMissileType == MissileType.RadarGuided)
                currentMissileType = MissileType.HeatSeeking;
            else
                currentMissileType = MissileType.RadarGuided;
            
            Debug.Log($"Missile type: {currentMissileType}");
        }
    }
    
    private void HandleMissileFiring()
    {
        // Fire missile with a key (e.g., left mouse button or a specific key)
        if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.F))
        {
            if (currentMissileCount <= 0)
            {
                Debug.Log("No missiles remaining!");
                PlaySound(noLockSound);
                return;
            }
            
            if (currentTarget == null)
            {
                Debug.Log("No target selected!");
                PlaySound(noLockSound);
                return;
            }
            
            if (!isLocked)
            {
                Debug.Log($"Target not locked! Lock progress: {GetLockProgress() * 100f:F0}%");
                PlaySound(noLockSound);
                return;
            }
            
            LaunchMissile();
        }
    }
    
    private void LaunchMissile()
    {
        if (missileHardpoints.Length == 0) return;
        
        // Get next available hardpoint
        Transform launchPoint = missileHardpoints[nextHardpointIndex % missileHardpoints.Length];
        nextHardpointIndex++;
        
        // Instantiate and initialize missile
        GameObject missileObj = Instantiate(missilePrefab, launchPoint.position, launchPoint.rotation);
        Missile missile = missileObj.GetComponent<Missile>();
        
        if (missile == null)
        {
            Debug.LogError("Missile prefab does not have Missile component!");
            Destroy(missileObj);
            return;
        }
        
        // Set missile type
        missile.missileType = currentMissileType;
        
        // Get aircraft velocity for proper launch
        Vector3 aircraftVelocity = Vector3.zero;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            aircraftVelocity = rb.linearVelocity;
        }
        
        float lockQuality = Mathf.Clamp01(lockTimer / lockOnTime);
        missile.Initialize(launchPoint, currentTarget, lockQuality, aircraftVelocity);
        
        currentMissileCount--;
        PlaySound(launchSound);
        
        // Reset lock after firing
        lockTimer = 0f;
        isLocking = false;
        isLocked = false;
        
        Debug.Log($"Missile launched! Remaining: {currentMissileCount}");
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    private void PlayLockingSound()
    {
        if (lockingSound != null && !audioSource.isPlaying)
        {
            audioSource.clip = lockingSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }
    
    public float GetLockProgress()
    {
        return Mathf.Clamp01(lockTimer / lockOnTime);
    }
    
    public bool HasTarget()
    {
        return currentTarget != null;
    }
    
    public int GetMissileCount()
    {
        return currentMissileCount;
    }
    
    public Transform GetCurrentTarget()
    {
        return currentTarget;
    }
    
    public bool IsTargetLocked()
    {
        return isLocked;
    }
    
    public bool IsTargetLocking()
    {
        return isLocking;
    }
    
    public MissileType GetCurrentMissileType()
    {
        return currentMissileType;
    }

    public List<Transform> GetAvailableTargets()
    {
        return new List<Transform>(availableTargets);
    }
    
    public string GetTargetingInfo()
    {
        if (currentTarget == null)
            return "NO TARGET";
        
        float distance = Vector3.Distance(transform.position, currentTarget.position);
        string lockStatus = isLocked ? "LOCKED" : $"LOCKING {GetLockProgress() * 100f:F0}%";
        
        return $"TGT: {currentTarget.name}\nRNG: {distance:F0}m\n{lockStatus}";
    }
}
