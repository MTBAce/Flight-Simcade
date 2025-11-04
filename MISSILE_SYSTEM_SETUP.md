# F-16 Missile System Setup Guide

## Overview
This missile system provides a complete air-to-air combat capability for your F16 flight simulator, featuring:
- Radar-guided and heat-seeking missile types
- Progressive target locking system
- Visual and audio feedback
- Realistic missile physics with fuel and guidance
- Integrated HUD display
- Multiple hardpoint support

## Components

### 1. Missile.cs
Core missile behavior including:
- Proportional navigation guidance
- Target prediction for moving objects
- Fuel consumption and propulsion
- Proximity detonation
- Splash damage system
- Visual effects (smoke trails, explosions)

### 2. MissileSystem.cs
Manages missile targeting and launching:
- Auto-target acquisition
- Target cycling
- Progressive lock-on system
- Missile type selection
- Audio feedback (locking tones, warnings)
- Launch management from hardpoints

### 3. FlightController.cs (Enhanced)
Now includes integrated HUD display showing:
- Missile count and type
- Target information and range
- Lock progress bar
- Visual lock status
- Control hints

## Setup Instructions

### Step 1: Prepare Your F-16 GameObject

1. Add the **MissileSystem** component to your F-16 GameObject (if not already present)
2. The **FlightController** should already be integrated with the HUD updates

### Step 2: Create Missile Hardpoints

1. Create empty GameObjects as children of your F-16 for missile launch points
2. Position them under the wings where missiles would be mounted
3. Typical positions (adjust based on your model):
   - Left wing: Position (-2, -0.5, 0) relative to aircraft
   - Right wing: Position (2, -0.5, 0) relative to aircraft
4. Name them clearly: "Hardpoint_Left_1", "Hardpoint_Right_1", etc.

### Step 3: Configure the Missile Prefab

1. Create a new GameObject for the missile
2. Add these components:
   - **Rigidbody** (uncheck "Use Gravity", set Mass to 0.1)
   - **Capsule Collider** (or appropriate shape, adjust size)
   - **Missile** component (the script)
   - **TrailRenderer** (optional, for visual trail)
   - **AudioSource** (for sounds)
   - **ParticleSystem** (for smoke trail, assign to Missile.smokeTrail)
   - **Light** (optional, for visual effect, assign to Missile.missileLight)

3. Configure the Missile component:
   ```
   Missile Type: RadarGuided
   Max Speed: 800 (km/h)
   Acceleration: 50
   Turn Rate: 3
   Max Lifetime: 30 seconds
   Proximity Detonation: 5 meters
   Fuel Duration: 15 seconds
   Max Turn Angle: 180
   Explosion Radius: 20 meters
   Explosion Damage: 100
   ```

4. Create an explosion prefab:
   - Add a ParticleSystem with an explosion effect
   - Assign to Missile.explosionPrefab

5. Save as Prefab: Name it "AIM120" or similar

### Step 4: Configure MissileSystem Component

In the Inspector for your F-16's MissileSystem:

**Missile Configuration:**
- **Missile Prefab**: Drag your created missile prefab here
- **Missile Hardpoints**: Add all your hardpoint transforms
- **Max Missiles**: 6 (or your desired count)
- **Default Missile Type**: RadarGuided

**Targeting:**
- **Max Lock Range**: 5000 meters
- **Lock On Time**: 2 seconds
- **Lock Angle**: 30 degrees (cone in front of aircraft)
- **Lock Break Angle**: 45 degrees
- **Target Layer**: Create a layer called "Target" and assign it here

**Key Bindings (can be changed):**
- **Cycle Missile Key**: N (switch between missile types)
- **Cycle Target Key**: T (cycle through available targets)
- **Fire Missile Key**: F (launch missile)

**Audio (optional):**
- **Locking Sound**: Short beep sound
- **Locked Sound**: Sustained tone
- **Launch Sound**: Whoosh/launch effect
- **No Lock Sound**: Error beep
- **Lock Break Sound**: Warning tone

### Step 5: Create Target Objects

1. Create GameObjects for enemy aircraft/targets
2. Add them to the "Target" layer you created
3. Add a **Collider** component (for missile collision detection)
4. Add a **Rigidbody** if targets should move/react to explosions
5. Optionally add AI movement scripts

### Step 6: Configure Layers and Physics

1. Go to Edit → Project Settings → Tags and Layers
2. Create a new layer called "Target"
3. Assign all enemy/target objects to this layer
4. In MissileSystem, set the Target Layer to this layer

### Step 7: Test the System

1. Enter Play Mode
2. Position your F-16 facing a target within 5000m
3. The system should auto-acquire the target
4. Watch the HUD for lock progress
5. Once locked (red "TARGET LOCKED" message), press F to fire

## Controls Reference

| Key | Action |
|-----|--------|
| F or Mouse Button | Fire Missile (when locked) |
| T | Cycle through available targets |
| N | Change missile type (Radar/Heat-Seeking) |
| WASD | Pitch/Roll control |
| Q/E | Yaw control |
| Space | Increase throttle |
| Left Ctrl | Decrease throttle |

## HUD Information

The HUD displays:
```
=== F-16 FLIGHT DATA ===
Throttle: XX%
Engine Power: XX%
Altitude: XXX m
Airspeed: XXX km/h

=== WEAPONS ===
Missiles: X
Type: RadarGuided

TGT: [Target Name]
RNG: XXXm
LOCKING XX%

[====================] 100%
*** TARGET LOCKED ***

=== CONTROLS ===
F/Mouse: Fire Missile
T: Cycle Target
N: Change Missile Type
```

## Advanced Configuration

### Missile Types

**Radar Guided (Default):**
- Better at long range
- More accurate guidance
- Works in all conditions

**Heat Seeking:**
- Better against targets with hot engines
- Can be fooled by flares (if implemented)
- Shorter effective range

### Tuning Lock Parameters

- **Lock On Time**: Increase for harder difficulty
- **Lock Angle**: Smaller = harder to acquire targets
- **Max Lock Range**: Increase for beyond-visual-range combat
- **Lock Break Angle**: How easily the lock breaks when target moves

### Missile Performance Tuning

In Missile.cs, adjust:
- **maxSpeed**: Higher = faster missiles
- **turnRate**: Higher = more maneuverable
- **acceleration**: How quickly reaches max speed
- **fuelDuration**: How long powered flight lasts
- **proximityDetonation**: Detection radius for hits

## Troubleshooting

**Missiles don't lock on:**
- Check Target Layer is set correctly
- Ensure targets have colliders
- Verify targets are within lock angle and range
- Check console for debug messages

**Missiles fire but don't track:**
- Verify missile prefab has Missile component
- Check Rigidbody settings (gravity off)
- Ensure Initialize is being called properly

**No audio feedback:**
- Assign AudioClips in MissileSystem
- Check AudioSource is added to aircraft
- Verify audio files are in project

**HUD not showing missile info:**
- Ensure MissileSystem component is on same GameObject as FlightController
- Check HUD TextMeshProUGUI is assigned in FlightController
- Verify missileSystem reference is not null

## Future Enhancements

Consider adding:
- Countermeasures (flares, chaff)
- Radar display showing target positions
- Multiple simultaneous locks
- Different missile variants (short/medium/long range)
- Weapon bay doors animation
- Missile camera view
- Lock warning for targets being locked
- Electronic warfare systems
- Multiple target engagement

## Notes

- The system uses Unity's Physics system, ensure your colliders are properly configured
- Missiles inherit the aircraft's velocity at launch for realistic behavior
- Target prediction calculates intercept points for moving targets
- Fuel management affects missile performance post-burnout
- Splash damage affects all objects within the explosion radius

## Credits

Enhanced missile system for Flight-Simcade F-16 simulator with:
- Proportional navigation guidance
- Progressive locking system  
- Integrated HUD display
- Realistic physics simulation
