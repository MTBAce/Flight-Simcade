using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manages UI indicators for missile lock targets. Shows all available targets with visual feedback
/// for their lock state (available, locking, or locked).
/// </summary>
public class MissileLockIndicatorUI : MonoBehaviour
{
    #region Inspector Fields
    
    [Header("References")]
    [Tooltip("Reference to the missile system (auto-found if not assigned)")]
    public MissileSystem missileSystem;
    
    [Tooltip("Canvas to spawn indicators on (auto-found if not assigned)")]
    public Canvas targetCanvas;
    
    [Tooltip("Camera used for world-to-screen conversion (auto-found if not assigned)")]
    public Camera mainCamera;
    
    [Header("Indicator Prefabs")]
    [Tooltip("UI prefab for available targets (not being locked)")]
    public RectTransform availableTargetPrefab;
    
    [Tooltip("UI prefab for target being locked (optional, uses available if not set)")]
    public RectTransform lockingTargetPrefab;
    
    [Tooltip("UI prefab for fully locked target (optional, uses available if not set)")]
    public RectTransform lockedTargetPrefab;
    
    [Header("Visual Settings")]
    [Tooltip("Color for available targets")]
    public Color availableColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
    
    [Tooltip("Color while locking onto target")]
    public Color lockingColor = Color.yellow;
    
    [Tooltip("Color when target is fully locked")]
    public Color lockedColor = Color.red;
    
    [Tooltip("Base scale for all indicators")]
    public float indicatorScale = 1f;
    
    [Header("Animation Settings")]
    [Tooltip("Enable pulsing animation for locking/locked targets")]
    public bool enablePulseEffect = true;
    
    [Tooltip("Speed of pulse animation")]
    public float pulseSpeed = 10f;
    
    [Tooltip("Amount of scale change in pulse (0.1 = 10% scale change)")]
    public float pulseAmount = 0.1f;
    
    [Header("Display Settings")]
    [Tooltip("Show distance to target on indicator")]
    public bool showDistance = true;
    
    [Tooltip("Show target name on indicator")]
    public bool showTargetName = true;
    
    [Header("Screen Edge Behavior")]
    [Tooltip("Keep indicators on screen when targets are off-screen")]
    public bool clampToScreenEdges = true;
    
    [Tooltip("Offset from screen edges in pixels")]
    public float edgeOffset = 50f;
    
    [Tooltip("Show indicators for targets behind the camera")]
    public bool showBehindIndicators = true;

    [Header("Debug")]
    [Tooltip("Show debug information in console")]
    public bool debugMode = false;
    
    [Tooltip("Minimum distance to show indicators (prevents flickering for very close targets)")]
    public float minimumTargetDistance = 10f;
    
    #endregion
    
    #region Private Fields
    
    // Container for organizing all indicators in hierarchy
    private RectTransform indicatorContainer;
    
    // Tracks all active target indicators
    private Dictionary<Transform, TargetIndicatorData> activeIndicators = new Dictionary<Transform, TargetIndicatorData>();
    
    #endregion
    
    #region Nested Classes
    
    /// <summary>
    /// Stores all data and references for a single target indicator
    /// </summary>
    private class TargetIndicatorData
    {
        public GameObject gameObject;
        public RectTransform rectTransform;
        public Image[] images;
        public Text nameText;
        public Text distanceText;
        public CanvasGroup canvasGroup;
    }
    
    /// <summary>
    /// Represents the current state of a target
    /// </summary>
    private enum TargetLockState
    {
        Available,  // Target detected but not being locked
        Locking,    // Currently acquiring lock
        Locked      // Fully locked
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        InitializeReferences();
        CreateIndicatorContainer();
    }
    
    private void Update()
    {
        if (!ValidateReferences())
            return;
        
        UpdateAllIndicators();
    }
    
    private void OnDestroy()
    {
        CleanupAllIndicators();
    }
    
    #endregion
    
    #region Initialization
    
    /// <summary>
    /// Finds and assigns required references if not set in inspector
    /// </summary>
    private void InitializeReferences()
    {
        // Find missile system
        if (missileSystem == null)
        {
            missileSystem = GetComponent<MissileSystem>();
            if (missileSystem == null)
            {
                Debug.LogError("[MissileLockIndicatorUI] No MissileSystem component found! Please assign one.", this);
            }
        }
        
        // Find main camera
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[MissileLockIndicatorUI] No main camera found! Please assign one.", this);
            }
        }
        
        // Find canvas
        if (targetCanvas == null)
        {
            targetCanvas = FindFirstObjectByType<Canvas>();
            if (targetCanvas == null)
            {
                Debug.LogError("[MissileLockIndicatorUI] No Canvas found in scene! Please create one.", this);
            }
        }
        
        // Validate prefabs
        if (availableTargetPrefab == null)
        {
            Debug.LogWarning("[MissileLockIndicatorUI] No availableTargetPrefab assigned! Indicators will not be created.", this);
        }
    }
    
    /// <summary>
    /// Creates a container object to organize all indicators
    /// </summary>
    private void CreateIndicatorContainer()
    {
        if (targetCanvas == null)
            return;
        
        GameObject containerObj = new GameObject("TargetIndicators");
        containerObj.transform.SetParent(targetCanvas.transform, false);
        
        indicatorContainer = containerObj.AddComponent<RectTransform>();
        
        // Set container to fill entire canvas
        indicatorContainer.anchorMin = Vector2.zero;
        indicatorContainer.anchorMax = Vector2.one;
        indicatorContainer.sizeDelta = Vector2.zero;
        indicatorContainer.anchoredPosition = Vector2.zero;
        
        // Ensure container renders on top of other UI elements
        indicatorContainer.SetAsLastSibling();
    }
    
    /// <summary>
    /// Validates that all required references are available
    /// </summary>
    private bool ValidateReferences()
    {
        return missileSystem != null && mainCamera != null && targetCanvas != null && indicatorContainer != null;
    }
    
    #endregion
    
    #region Indicator Management
    
    /// <summary>
    /// Updates all target indicators each frame
    /// </summary>
    private void UpdateAllIndicators()
    {
        List<Transform> availableTargets = missileSystem.GetAvailableTargets();
        Transform currentTarget = missileSystem.GetCurrentTarget();
        
        // Remove indicators for targets that no longer exist
        RemoveStaleIndicators(availableTargets);
        
        // Update or create indicators for all available targets
        foreach (Transform target in availableTargets)
        {
            if (target == null)
                continue;
            
            TargetLockState state = DetermineTargetState(target, currentTarget);
            
            if (activeIndicators.ContainsKey(target))
            {
                UpdateExistingIndicator(target, state);
            }
            else
            {
                CreateNewIndicator(target, state);
            }
        }
    }
    
    /// <summary>
    /// Determines the lock state for a specific target
    /// </summary>
    private TargetLockState DetermineTargetState(Transform target, Transform currentTarget)
    {
        if (target != currentTarget)
            return TargetLockState.Available;
        
        if (missileSystem.IsTargetLocked())
            return TargetLockState.Locked;
        
        if (missileSystem.IsTargetLocking())
            return TargetLockState.Locking;
        
        return TargetLockState.Available;
    }
    
    /// <summary>
    /// Creates a new indicator for a target
    /// </summary>
    private void CreateNewIndicator(Transform target, TargetLockState state)
    {
        RectTransform prefab = GetPrefabForState(state);
        if (prefab == null)
            return;
        
        // Instantiate the prefab
        RectTransform indicatorRect = Instantiate(prefab, indicatorContainer);
        indicatorRect.SetAsLastSibling();
        
        // Create data object
        TargetIndicatorData data = new TargetIndicatorData
        {
            gameObject = indicatorRect.gameObject,
            rectTransform = indicatorRect,
            images = indicatorRect.GetComponentsInChildren<Image>(),
            canvasGroup = indicatorRect.GetComponent<CanvasGroup>()
        };
        
        // Add canvas group if missing
        if (data.canvasGroup == null)
        {
            data.canvasGroup = indicatorRect.gameObject.AddComponent<CanvasGroup>();
        }
        
        // Configure canvas group
        data.canvasGroup.blocksRaycasts = false;
        data.canvasGroup.interactable = false;
        
        // Find text components
        Text[] texts = indicatorRect.GetComponentsInChildren<Text>();
        foreach (Text text in texts)
        {
            string textName = text.name.ToLower();
            if (textName.Contains("name"))
                data.nameText = text;
            else if (textName.Contains("distance") || textName.Contains("range"))
                data.distanceText = text;
        }
        
        // Set initial scale
        indicatorRect.localScale = Vector3.one * indicatorScale;
        
        // Store the indicator
        activeIndicators[target] = data;
        
        // Initial update
        UpdateExistingIndicator(target, state);
    }
    
    /// <summary>
    /// Updates an existing indicator's position and appearance
    /// </summary>
    private void UpdateExistingIndicator(Transform target, TargetLockState state)
    {
        if (!activeIndicators.ContainsKey(target))
            return;
        
        TargetIndicatorData data = activeIndicators[target];
        if (data.gameObject == null)
        {
            activeIndicators.Remove(target);
            return;
        }
        
        // Check minimum distance
        float distanceToTarget = Vector3.Distance(mainCamera.transform.position, target.position);
        if (distanceToTarget < minimumTargetDistance)
        {
            data.gameObject.SetActive(false);
            return;
        }
        
        // Calculate screen position
        Vector3 worldPos = target.position;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        bool isBehindCamera = screenPos.z <= 0;
        
        if (debugMode)
        {
            Debug.Log($"[MissileLockIndicatorUI] Target: {target.name}, ScreenPos: {screenPos}, Behind: {isBehindCamera}, Distance: {distanceToTarget:F1}");
        }
        
        // Hide indicators behind camera if not enabled
        if (isBehindCamera && !showBehindIndicators)
        {
            data.gameObject.SetActive(false);
            return;
        }
        
        // Handle behind camera
        if (isBehindCamera)
        {
            if (clampToScreenEdges)
            {
                // Flip position for targets behind camera
                screenPos.x = Screen.width - screenPos.x;
                screenPos.y = Screen.height - screenPos.y;
                screenPos = ClampPositionToScreenEdges(screenPos);
                data.canvasGroup.alpha = 0.5f;
            }
            else
            {
                data.gameObject.SetActive(false);
                return;
            }
        }
        else
        {
            // Target is in front of camera
            bool isOffScreen = IsPositionOffScreen(screenPos);
            
            if (isOffScreen)
            {
                if (clampToScreenEdges)
                {
                    screenPos = ClampPositionToScreenEdges(screenPos);
                    data.canvasGroup.alpha = 0.7f;
                }
                else
                {
                    data.gameObject.SetActive(false);
                    return;
                }
            }
            else
            {
                // On screen and in front - full visibility
                data.canvasGroup.alpha = 1f;
            }
        }
        
        // Position the indicator
        PositionIndicator(data, screenPos);
        
        // Update visual appearance
        UpdateIndicatorVisuals(data, target, state);
        
        // Update text information
        UpdateIndicatorText(data, target);
        
        // Ensure indicator is visible
        data.gameObject.SetActive(true);
    }
    
    /// <summary>
    /// Handles positioning and visibility for off-screen indicators
    /// </summary>
    private bool HandleOffScreenIndicator(TargetIndicatorData data, Vector3 screenPos, bool isBehindCamera)
    {
        if (!clampToScreenEdges)
            return false;
        
        if (isBehindCamera)
        {
            if (!showBehindIndicators)
                return false;
            
            // Flip position for targets behind camera
            screenPos.x = Screen.width - screenPos.x;
            screenPos.y = Screen.height - screenPos.y;
            screenPos.z = Mathf.Abs(screenPos.z);
            
            data.canvasGroup.alpha = 0.5f;
        }
        else
        {
            data.canvasGroup.alpha = 0.7f;
        }
        
        // Clamp to screen edges
        screenPos = ClampPositionToScreenEdges(screenPos);
        return true;
    }
    
    /// <summary>
    /// Positions an indicator at the specified screen position
    /// </summary>
    private void PositionIndicator(TargetIndicatorData data, Vector3 screenPos)
    {
        RectTransform canvasRect = targetCanvas.GetComponent<RectTransform>();
        Vector2 localPoint = Vector2.zero;
        bool success = false;
        
        if (targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // For overlay mode, pass null as the camera parameter
            success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                null,
                out localPoint
            );
        }
        else if (targetCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            // Convert screen position to canvas local position using the canvas camera
            success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                targetCanvas.worldCamera,
                out localPoint
            );
        }
        else if (targetCanvas.renderMode == RenderMode.WorldSpace)
        {
            // For world space canvas, use the main camera
            success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPos,
                mainCamera,
                out localPoint
            );
        }
        
        if (success)
        {
            data.rectTransform.localPosition = localPoint;
        }
    }
    
    /// <summary>
    /// Updates the visual appearance of an indicator based on its state
    /// </summary>
    private void UpdateIndicatorVisuals(TargetIndicatorData data, Transform target, TargetLockState state)
    {
        Color targetColor = GetColorForState(state);
        float targetScale = indicatorScale;
        
        // Apply pulse effect for locking and locked states
        if (enablePulseEffect && state != TargetLockState.Available)
        {
            float pulseMultiplier = (state == TargetLockState.Locking) ? 1f : 0.5f;
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed * pulseMultiplier) * pulseAmount;
            targetScale *= pulse;
        }
        
        // Update all images with the target color
        if (data.images != null)
        {
            foreach (Image img in data.images)
            {
                if (img != null)
                    img.color = targetColor;
            }
        }
        
        // Update scale
        data.rectTransform.localScale = Vector3.one * targetScale;
    }
    
    /// <summary>
    /// Updates the text information displayed on an indicator
    /// </summary>
    private void UpdateIndicatorText(TargetIndicatorData data, Transform target)
    {
        // Update name text
        if (data.nameText != null)
        {
            if (showTargetName)
            {
                data.nameText.text = target.name;
                data.nameText.gameObject.SetActive(true);
            }
            else
            {
                data.nameText.gameObject.SetActive(false);
            }
        }
        
        // Update distance text
        if (data.distanceText != null)
        {
            if (showDistance)
            {
                float distance = Vector3.Distance(missileSystem.transform.position, target.position);
                data.distanceText.text = $"{distance:F0}m";
                data.distanceText.gameObject.SetActive(true);
            }
            else
            {
                data.distanceText.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Removes indicators for targets that no longer exist
    /// </summary>
    private void RemoveStaleIndicators(List<Transform> currentTargets)
    {
        List<Transform> targetsToRemove = new List<Transform>();
        
        foreach (var kvp in activeIndicators)
        {
            if (kvp.Key == null || !currentTargets.Contains(kvp.Key))
            {
                targetsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (Transform target in targetsToRemove)
        {
            RemoveIndicator(target);
        }
    }
    
    /// <summary>
    /// Removes a specific indicator
    /// </summary>
    private void RemoveIndicator(Transform target)
    {
        if (activeIndicators.ContainsKey(target))
        {
            if (activeIndicators[target].gameObject != null)
            {
                Destroy(activeIndicators[target].gameObject);
            }
            activeIndicators.Remove(target);
        }
    }
    
    /// <summary>
    /// Removes all indicators and cleans up
    /// </summary>
    private void CleanupAllIndicators()
    {
        foreach (var kvp in activeIndicators)
        {
            if (kvp.Value.gameObject != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }
        
        activeIndicators.Clear();
        
        if (indicatorContainer != null)
        {
            Destroy(indicatorContainer.gameObject);
        }
    }
    
    #endregion
    
    #region Helper Methods
    
    /// <summary>
    /// Gets the appropriate prefab for a given target state
    /// </summary>
    private RectTransform GetPrefabForState(TargetLockState state)
    {
        switch (state)
        {
            case TargetLockState.Locking:
                return lockingTargetPrefab != null ? lockingTargetPrefab : availableTargetPrefab;
            case TargetLockState.Locked:
                return lockedTargetPrefab != null ? lockedTargetPrefab : availableTargetPrefab;
            default:
                return availableTargetPrefab;
        }
    }
    
    /// <summary>
    /// Gets the appropriate color for a given target state
    /// </summary>
    private Color GetColorForState(TargetLockState state)
    {
        switch (state)
        {
            case TargetLockState.Locking:
                float lockProgress = missileSystem.GetLockProgress();
                return Color.Lerp(lockingColor, lockedColor, lockProgress);
            case TargetLockState.Locked:
                return lockedColor;
            default:
                return availableColor;
        }
    }
    
    /// <summary>
    /// Checks if a screen position is outside the visible screen area
    /// </summary>
    private bool IsPositionOffScreen(Vector3 screenPos)
    {
        // Add a small margin to prevent flickering at edges
        float margin = 10f;
        return screenPos.x < -margin || 
               screenPos.x > Screen.width + margin ||
               screenPos.y < -margin || 
               screenPos.y > Screen.height + margin;
    }
    
    /// <summary>
    /// Clamps a screen position to the screen edges with offset
    /// </summary>
    private Vector3 ClampPositionToScreenEdges(Vector3 screenPos)
    {
        screenPos.x = Mathf.Clamp(screenPos.x, edgeOffset, Screen.width - edgeOffset);
        screenPos.y = Mathf.Clamp(screenPos.y, edgeOffset, Screen.height - edgeOffset);
        screenPos.z = Mathf.Max(screenPos.z, 0.1f); // Ensure positive z value
        return screenPos;
    }
    
    #endregion
}
