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
    
    [Header("Visual Feedback")]
    public GameObject lockingIndicatorPrefab; // Optional UI element
    public Color lockingColor = Color.yellow;
    public Color lockedColor = Color.red;
    public Canvas targetCanvas; // Assign your UI Canvas here
    public float indicatorScale = 1f;
    
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
    private GameObject currentIndicator;
    private Camera mainCamera;
    
    private void Awake()
    {
        currentMissileCount = maxMissiles;
        currentMissileType = defaultMissileType;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // 2D sound for cockpit
        flightController = GetComponent<FlightController>();
        mainCamera = Camera.main;
        
        // Auto-find canvas if not assigned
        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
        }
    }
    
    private void Update()
    {
        UpdateTargeting();
        HandleMissileFiring();
        HandleTargetCycling();
        HandleMissileTypeSelection();
        UpdateLockingIndicator();
    }
    
    private void OnDestroy()
    {
        // Clean up indicator when destroyed
        if (currentIndicator != null)
        {
            Destroy(currentIndicator);
        }
    }
    
    private void UpdateTargeting()
    {
        // Refresh available targets list
        availableTargets.Clear();
        Collider[] potentialTargets = Physics.OverlapSphere(transform.position, maxLockRange, targetLayer);
        
        foreach (Collider col in potentialTargets)
        {
            Vector3 directionToTarget = col.transform.position - transform.position;
            float angle = Vector3.Angle(transform.forward, directionToTarget);
            
            if (angle < lockAngle)
            {
                availableTargets.Add(col.transform);
            }
        }
        
        // Auto-acquire target if none selected
        if (currentTarget == null && availableTargets.Count > 0)
        {
            currentTarget = FindBestTarget();
            currentTargetIndex = availableTargets.IndexOf(currentTarget);
            lockTimer = 0f;
            isLocking = true;
            isLocked = false;
            CreateLockingIndicator();
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
                if (isLocked || isLocking)
                {
                    PlaySound(lockBreakSound);
                    Debug.Log("Lock broken!");
                }
                DestroyLockingIndicator();
                currentTarget = null;
                lockTimer = 0f;
                isLocking = false;
                isLocked = false;
                return;
            }
            
            // Continue locking
            if (!isLocked)
            {
                lockTimer += Time.deltaTime;
                
                if (lockTimer >= lockOnTime)
                {
                    // Full lock achieved
                    isLocking = false;
                    isLocked = true;
                    PlaySound(lockedSound);
                    Debug.Log($"Target locked: {currentTarget.name}");
                }
                else if (isLocking)
                {
                    // Still locking - play periodic tone
                    PlayLockingSound();
                }
            }
        }
        else
        {
            lockTimer = 0f;
            isLocking = false;
            isLocked = false;
            DestroyLockingIndicator();
        }
    }
    
    private void CreateLockingIndicator()
    {
        if (lockingIndicatorPrefab == null || targetCanvas == null)
            return;
            
        // Destroy existing indicator if any
        DestroyLockingIndicator();
        
        // Create new indicator as child of canvas
        currentIndicator = Instantiate(lockingIndicatorPrefab, targetCanvas.transform);
        
        // Set initial scale
        currentIndicator.transform.localScale = Vector3.one * indicatorScale;
        
        // Set initial color to locking color
        UpdateIndicatorColor(lockingColor);
    }
    
    private void DestroyLockingIndicator()
    {
        if (currentIndicator != null)
        {
            Destroy(currentIndicator);
            currentIndicator = null;
        }
    }
    
    private void UpdateLockingIndicator()
    {
        if (currentIndicator == null || currentTarget == null || mainCamera == null)
            return;
        
        // Convert target world position to screen space
        Vector3 screenPos = mainCamera.WorldToScreenPoint(currentTarget.position);
        
        // Check if target is in front of camera
        if (screenPos.z > 0)
        {
            // Get RectTransform for proper UI positioning
            RectTransform rectTransform = currentIndicator.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // For Screen Space - Overlay, directly use screen position
                if (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    rectTransform.position = screenPos;
                }
                // For Screen Space - Camera, convert to local canvas position
                else if (targetCanvas.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        targetCanvas.GetComponent<RectTransform>(),
                        screenPos,
                        targetCanvas.worldCamera,
                        out localPoint
                    );
                    rectTransform.localPosition = localPoint;
                }
            }
            else
            {
                // Fallback if no RectTransform (shouldn't happen)
                currentIndicator.transform.position = screenPos;
            }
            
            // Update color based on lock status
            if (isLocked)
            {
                UpdateIndicatorColor(lockedColor);
            }
            else if (isLocking)
            {
                // Lerp between locking and locked color based on progress
                Color currentColor = Color.Lerp(lockingColor, lockedColor, GetLockProgress());
                UpdateIndicatorColor(currentColor);
            }
            
            // Optional: Add pulsing effect while locking
            if (isLocking && !isLocked)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.1f;
                currentIndicator.transform.localScale = Vector3.one * indicatorScale * pulse;
            }
            else
            {
                currentIndicator.transform.localScale = Vector3.one * indicatorScale;
            }
            
            // Show indicator
            currentIndicator.SetActive(true);
        }
        else
        {
            // Hide indicator if target is behind camera
            currentIndicator.SetActive(false);
        }
    }
    
    private void UpdateIndicatorColor(Color color)
    {
        if (currentIndicator == null)
            return;
        
        // Try to find Image component
        UnityEngine.UI.Image image = currentIndicator.GetComponentInChildren<UnityEngine.UI.Image>();
        if (image != null)
        {
            image.color = color;
        }
        
        // Also try to update any other UI components that might have color
        UnityEngine.UI.RawImage rawImage = currentIndicator.GetComponentInChildren<UnityEngine.UI.RawImage>();
        if (rawImage != null)
        {
            rawImage.color = color;
        }
    }
    
    private Transform FindBestTarget()
    {
        if (availableTargets.Count == 0)
            return null;
        
        Transform bestTarget = null;
        float bestScore = float.MaxValue;
        
        foreach (Transform target in availableTargets)
        {
            Vector3 directionToTarget = target.position - transform.position;
            float angle = Vector3.Angle(transform.forward, directionToTarget);
            float distance = directionToTarget.magnitude;
            
            // Score based on angle and distance (prefer close and centered)
            float score = angle * 10f + distance * 0.1f;
            
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = target;
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
            CreateLockingIndicator();
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
        if (Input.GetKeyDown(fireMissileKey) || Input.GetKeyDown(KeyCode.Mouse0))
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
        if (missileHardpoints.Length == 0)
        {
            Debug.LogWarning("No missile hardpoints configured!");
            return;
        }
        
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
        if (flightController != null)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                aircraftVelocity = rb.linearVelocity;
            }
        }
        
        float lockQuality = Mathf.Clamp01(lockTimer / lockOnTime);
        missile.Initialize(launchPoint, currentTarget, lockQuality, aircraftVelocity);
        
        currentMissileCount--;
        PlaySound(launchSound);
        
        Debug.Log($"Missile launched! Remaining: {currentMissileCount}, Type: {currentMissileType}, Lock Quality: {lockQuality * 100f:F0}%");
        
        // Keep lock for potential follow-up shots
        // Reset lock after a short delay or after target is destroyed
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
        // Play beeping sound that gets faster as lock progresses
        lockingSoundTimer += Time.deltaTime;
        float beepInterval = Mathf.Lerp(0.5f, 0.1f, GetLockProgress());
        
        if (lockingSoundTimer >= beepInterval)
        {
            lockingSoundTimer = 0f;
            PlaySound(lockingSound);
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
    
    public MissileType GetCurrentMissileType()
    {
        return currentMissileType;
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
