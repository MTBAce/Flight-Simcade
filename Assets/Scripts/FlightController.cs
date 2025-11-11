using System;
using UnityEngine;
using TMPro;
public class FlightController : MonoBehaviour
{
    public float throttleIncrement = 0.1f;
    public float maxThrust = 200f;
    public float responsiveness = 10f;
    public float lift = 135;
    
    [Header("Realistic Throttle Settings")]
    public float engineSpoolTime = 2f; // Time for engine to reach full power
    public float dragCoefficient = 0.5f; // Air resistance

    [Header("Control Surfaces")]
    public Transform leftAileron;
    public Transform rightAileron;
    public Transform leftElevator;
    public Transform rightElevator;
    public Transform rudder;
    public Transform leftFlap;
    public Transform rightFlap;
    
    [Header("Control Surface Angles")]
    public float maxAileronAngle = 25f;
    public float maxElevatorAngle = 30f;
    public float maxRudderAngle = 30f;
    public float maxFlapAngle = 40f;
    public float controlSurfaceSpeed = 5f;
    
    [Header("Engine Sound")]
    [Tooltip("AudioSource component for engine sound (auto-found if not assigned)")]
    public AudioSource engineSound;
    
    [Tooltip("Minimum pitch at idle throttle (0%)")]
    public float minEnginePitch = 0.8f;
    
    [Tooltip("Maximum pitch at full throttle (100%)")]
    public float maxEnginePitch = 2.0f;
    
    [Tooltip("How quickly the pitch changes")]
    public float pitchChangeSpeed = 2f;

    private float throttle;
    private float currentThrust; // Actual engine thrust (lags behind throttle)
    private float roll;
    private float pitch;
    private float yaw;

    private float responseModifier
    {
        get
        {
            return (rb.mass / 10f) * responsiveness;
        } 
    }   
    
    Rigidbody rb;
    [SerializeField] private TextMeshProUGUI hud;
    private MissileSystem missileSystem;

    private void Awake()
    {
        currentThrust = 100f;
        rb = GetComponent<Rigidbody>();
        missileSystem = GetComponent<MissileSystem>();
        
        // Find engine sound AudioSource if not assigned
        if (engineSound == null)
        {
            engineSound = GetComponent<AudioSource>();
            if (engineSound == null)
            {
                Debug.LogWarning("[FlightController] No AudioSource found for engine sound. Please add one to enable engine sound pitch control.");
            }
        }
    }

    private void HandleInput()
    {
        roll = Input.GetAxis("Horizontal");
        pitch = Input.GetAxis("Vertical");
        yaw = Input.GetAxis("Yaw");
        
        if (Input.GetKey(KeyCode.Space)) throttle += throttleIncrement;
        else if (Input.GetKey(KeyCode.LeftControl)) throttle -= throttleIncrement;
        throttle = Mathf.Clamp(throttle, 0f, 100f);
    }

    private void Update()
    {
        HandleInput();
        UpdateHUD();
        AnimateControlSurfaces();
        UpdateEngineSoundPitch();
    }

    private void FixedUpdate()
    {
        // Smoothly interpolate current thrust towards target throttle (engine spool)
        float targetThrust = throttle;
        currentThrust = Mathf.Lerp(currentThrust, targetThrust, Time.fixedDeltaTime / engineSpoolTime);
        
        // Apply thrust based on actual engine power
        rb.AddForce(transform.forward * (maxThrust * currentThrust));
        
        // Apply drag force (opposes velocity)
        Vector3 dragForce = -rb.linearVelocity * dragCoefficient * rb.linearVelocity.magnitude;
        rb.AddForce(dragForce);
        
        rb.AddTorque(transform.up * (yaw * responseModifier));
        rb.AddTorque(transform.right * (pitch * responseModifier));
        rb.AddTorque(-transform.forward * (roll * responseModifier));

        rb.AddForce(Vector3.up * (rb.linearVelocity.magnitude * lift));
    }

    private void AnimateControlSurfaces()
    {
        float deltaTime = Time.deltaTime * controlSurfaceSpeed;
        
        // Ailerons (roll control) - move in opposite directions
        if (leftAileron != null)
        {
            float targetAngle = -roll * maxAileronAngle;
            Quaternion targetRotation = Quaternion.Euler(targetAngle, 0, 0);
            leftAileron.localRotation = Quaternion.Lerp(leftAileron.localRotation, targetRotation, deltaTime);
        }
        
        if (rightAileron != null)
        {
            float targetAngle = roll * maxAileronAngle;
            Quaternion targetRotation = Quaternion.Euler(targetAngle, 0, 0);
            rightAileron.localRotation = Quaternion.Lerp(rightAileron.localRotation, targetRotation, deltaTime);
        }
        
        // Elevators (pitch control) - move together
        if (leftElevator != null)
        {
            float targetAngle = pitch * maxElevatorAngle;
            Quaternion targetRotation = Quaternion.Euler(-targetAngle, 0, 0);
            leftElevator.localRotation = Quaternion.Lerp(leftElevator.localRotation, targetRotation, deltaTime);
        }
        
        if (rightElevator != null)
        {
            float targetAngle = pitch * maxElevatorAngle;
            Quaternion targetRotation = Quaternion.Euler(-targetAngle, 0, 0);
            rightElevator.localRotation = Quaternion.Lerp(rightElevator.localRotation, targetRotation, deltaTime);
        }
        
        // Rudder (yaw control)
        if (rudder != null)
        {
            float targetAngle = yaw * maxRudderAngle;
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
            rudder.localRotation = Quaternion.Lerp(rudder.localRotation, targetRotation, deltaTime);
        }
        
        // Flaps (extend at lower speeds/higher throttle for takeoff/landing)
        float airspeed = rb.linearVelocity.magnitude * 3.6f;
        float flapDeployment = 0f;
        
        // Deploy flaps at low speeds (below 200 km/h)
        if (airspeed < 200f)
        {
            flapDeployment = Mathf.Clamp01((200f - airspeed) / 200f);
        }
        
        if (leftFlap != null)
        {
            float targetAngle = flapDeployment * maxFlapAngle;
            Quaternion targetRotation = Quaternion.Euler(targetAngle, 0, 0);
            leftFlap.localRotation = Quaternion.Lerp(leftFlap.localRotation, targetRotation, deltaTime);
        }
        
        if (rightFlap != null)
        {
            float targetAngle = flapDeployment * maxFlapAngle;
            Quaternion targetRotation = Quaternion.Euler(targetAngle, 0, 0);
            rightFlap.localRotation = Quaternion.Lerp(rightFlap.localRotation, targetRotation, deltaTime);
        }
    }

    private void UpdateEngineSoundPitch()
    {
        if (engineSound == null)
            return;
        
        // Calculate target pitch based on current thrust (0-100 scale)
        // Use currentThrust instead of throttle for smoother, more realistic sound
        float thrustPercent = currentThrust / 100f;
        float targetPitch = Mathf.Lerp(minEnginePitch, maxEnginePitch, thrustPercent);
        
        // Smoothly interpolate to target pitch
        engineSound.pitch = Mathf.Lerp(engineSound.pitch, targetPitch, Time.deltaTime * pitchChangeSpeed);
    }

    private void UpdateHUD()
    {
        hud.text = "=== F-16 FLIGHT DATA ===\n";
        hud.text += "Throttle: " + throttle.ToString("F0") + "%\n";
        hud.text += "Engine Power: " + currentThrust.ToString("F0") + "%\n";
        hud.text += "Altitude: " + transform.position.y.ToString("F0") + " m\n";
        hud.text += "Airspeed: " + (rb.linearVelocity.magnitude * 3.6f).ToString("F0") + " km/h\n";
        
        // Add missile system info
        if (missileSystem != null)
        {
            hud.text += "\n=== WEAPONS ===\n";
            hud.text += "Missiles: " + missileSystem.GetMissileCount() + "\n";
            hud.text += "Type: " + missileSystem.GetCurrentMissileType().ToString() + "\n";
            hud.text += "\n" + missileSystem.GetTargetingInfo() + "\n";
            
            // Display lock indicator
            if (missileSystem.HasTarget())
            {
                float lockProgress = missileSystem.GetLockProgress();
                string lockBar = "[";
                int barLength = 20;
                int filled = (int)(lockProgress * barLength);
                
                for (int i = 0; i < barLength; i++)
                {
                    lockBar += i < filled ? "=" : "-";
                }
                lockBar += "]";
                
                hud.text += lockBar + " " + (lockProgress * 100f).ToString("F0") + "%\n";
                
                if (missileSystem.IsTargetLocked())
                {
                    hud.text += "<color=red>*** TARGET LOCKED ***</color>\n";
                }
            }
            
            hud.text += "\n=== CONTROLS ===\n";
            hud.text += "F/Mouse: Fire Missile\n";
            hud.text += "T: Cycle Target\n";
            hud.text += "N: Change Missile Type\n";
        }
    }
}
