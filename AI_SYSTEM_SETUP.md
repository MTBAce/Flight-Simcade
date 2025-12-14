# AI Enemy System Setup Guide

This guide will help you set up the AI enemy system with random flight movement and wave-based spawning in your Flight Simcade project.

## Overview

The system consists of two main components:
1. **AI_Movement.cs** - Handles individual enemy aircraft behavior (random flight, waypoint navigation, optional combat mode)
2. **AI_Spawner.cs** - Manages wave-based spawning of enemies with progressive difficulty

## Features

### AI_Movement Features
- ✈️ Random waypoint-based flight patterns
- 🎯 Optional combat mode to track and intercept the player
- 🔄 Automatic boundary enforcement to keep AI within a defined area
- ⚙️ Configurable speed, turn rate, and altitude ranges
- 🎨 Visual debugging with Gizmos in the editor

### AI_Spawner Features
- 🌊 Wave-based enemy spawning system
- 📈 Progressive difficulty (more enemies per wave)
- ⏱️ Configurable delays between waves
- 🎯 Circular spawn area with customizable radius
- 📊 Wave tracking and enemy count monitoring
- 🎛️ Manual control methods for testing

## Setup Instructions

### Step 1: Create an Enemy Prefab

1. **Create a new GameObject** in your scene for the enemy aircraft:
   - Right-click in Hierarchy → 3D Object → Create Empty
   - Name it "EnemyAircraft"

2. **Add your aircraft model**:
   - Drag your F-16 model (or any aircraft model) as a child of EnemyAircraft
   - Scale and position as needed

3. **Add required components**:
   - Select the EnemyAircraft GameObject
   - Add Component → Physics → Rigidbody
     - Set Mass: 10
     - Set Drag: 0.5
     - Set Angular Drag: 2
     - Enable Use Gravity
   
4. **Add the AI_Movement script**:
   - Add Component → Scripts → AI_Movement
   - Configure the settings (see Configuration section below)

5. **Optional - Add a collider**:
   - Add Component → Physics → Box Collider (or Mesh Collider)
   - This allows the enemy to be destroyed by missiles

6. **Optional - Add a tag**:
   - Set Tag to "Enemy" for easy identification
   - (Create the "Enemy" tag in Tag Manager if it doesn't exist)

7. **Create the prefab**:
   - Drag the EnemyAircraft GameObject from the Hierarchy into your Prefabs folder
   - Delete the GameObject from the scene (it will be spawned by the spawner)

### Step 2: Set Up the Spawner

1. **Create a spawner GameObject**:
   - Right-click in Hierarchy → Create Empty
   - Name it "AI_Spawner"
   - Position it where you want enemies to spawn around (e.g., at the center of your map)

2. **Add the AI_Spawner script**:
   - Select the AI_Spawner GameObject
   - Add Component → Scripts → AI_Spawner

3. **Configure the spawner**:
   - **Enemy Prefab**: Drag your EnemyAircraft prefab into this slot
   - Configure other settings as needed (see Configuration section)

### Step 3: Tag Your Player

For combat mode to work, your player must be tagged correctly:
1. Select your player GameObject
2. Set Tag to "Player" (create if needed)

## Configuration

### AI_Movement Settings

#### Flight Settings
- **Base Speed** (80): The base flight speed of the AI
- **Turn Speed** (2): How quickly the AI can turn
- **Speed Variation** (20): Random speed variation range

#### Waypoint Settings
- **Min Waypoint Distance** (200): Minimum distance for waypoints
- **Max Waypoint Distance** (500): Maximum distance for waypoints
- **Waypoint Reach Threshold** (50): Distance to consider waypoint reached
- **Min Altitude** (100): Minimum flight altitude
- **Max Altitude** (500): Maximum flight altitude

#### Boundary Settings
- **Boundary Radius** (2000): Keep AI within this radius (0 = no limit)

#### Combat Settings
- **Enable Combat Mode** (false): Track and attack the player
- **Detection Range** (1000): Range to detect and pursue player
- **Player Target**: Auto-detected if not set and combat mode is enabled

### AI_Spawner Settings

#### Wave Configuration
- **Initial Enemies Per Wave** (3): Number of enemies in wave 1
- **Enemy Increase Per Wave** (2): Additional enemies added each wave
- **Max Enemies Per Wave** (15): Maximum enemies in any wave
- **Delay Between Waves** (20): Seconds between waves
- **Auto Start** (true): Start spawning automatically

#### Spawn Area
- **Spawn Center**: Center point for spawning (uses spawner position if not set)
- **Min Spawn Distance** (500): Minimum distance from center
- **Max Spawn Distance** (1000): Maximum distance from center
- **Min Spawn Altitude** (200): Minimum spawn height
- **Max Spawn Altitude** (400): Maximum spawn height

#### Combat Settings
- **Enemies In Combat Mode** (true): Enable combat mode on spawned enemies

## Usage Examples

### Example 1: Passive Enemies
For enemies that just fly around randomly:
- Set `enableCombatMode` to **false** in AI_Movement
- Set `enemiesInCombatMode` to **false** in AI_Spawner

### Example 2: Aggressive Enemies
For enemies that hunt the player:
- Set `enableCombatMode` to **true** in AI_Movement
- Set `detectionRange` to a value like **1000**
- Set `enemiesInCombatMode` to **true** in AI_Spawner
- Ensure player is tagged as "Player"

### Example 3: Mixed Behavior
Create two different enemy prefabs:
- One with combat mode disabled (patrol aircraft)
- One with combat mode enabled (fighter aircraft)
- Use multiple spawners for different wave types

## Testing

### Visual Debugging in Editor
When you select an AI GameObject in the editor, you'll see:
- **Yellow sphere**: Current waypoint location
- **Yellow line**: Path to waypoint
- **Red sphere**: Boundary radius
- **Cyan sphere**: Detection range (if combat mode enabled)

For the spawner:
- **Green circle**: Minimum spawn distance
- **Yellow circle**: Maximum spawn distance
- **Cyan spheres**: Altitude range indicators

### Runtime Testing
You can call these methods from other scripts or via the console:

```csharp
// Get reference to spawner
AI_Spawner spawner = FindObjectOfType<AI_Spawner>();

// Force next wave immediately
spawner.ForceNextWave();

// Stop all waves
spawner.StopWaves();

// Clear all enemies
spawner.ClearAllEnemies();

// Reset to wave 1
spawner.ResetSpawner();

// Get wave information
int currentWave = spawner.GetCurrentWave();
int enemiesAlive = spawner.GetEnemiesAlive();
```

## Integration with Missile System

The AI enemies can be destroyed by your existing missile system. Ensure:
1. Enemy prefabs have colliders
2. Enemies are tagged appropriately
3. Your Missile.cs script handles enemy destruction on collision

Example collision handling (add to your Missile.cs if not present):

```csharp
private void OnCollisionEnter(Collision collision)
{
    if (collision.gameObject.CompareTag("Enemy"))
    {
        Destroy(collision.gameObject); // Destroy enemy
        Destroy(gameObject); // Destroy missile
    }
}
```

## Tips and Best Practices

1. **Performance**: 
   - Keep max enemies per wave reasonable (10-15)
   - Use LOD (Level of Detail) on aircraft models
   - Consider object pooling for many enemies

2. **Balancing**:
   - Start with 3-5 enemies per wave
   - Test different spawn distances for difficulty
   - Adjust AI speed based on player aircraft speed

3. **Visual Variety**:
   - Create multiple enemy prefabs with different models
   - Randomize spawn rotations and initial velocities
   - Add variation to AI parameters

4. **Combat Balance**:
   - Detection range should be slightly less than missile range
   - Adjust turn speed to make enemies dodgeable but challenging
   - Consider adding evasive maneuvers for advanced AI

## Troubleshooting

**Enemies not spawning:**
- Check that Enemy Prefab is assigned in spawner
- Verify spawner has Auto Start enabled
- Check console for error messages

**Enemies flying erratically:**
- Adjust turn speed (lower = smoother)
- Increase waypoint reach threshold
- Check Rigidbody drag settings

**Enemies leaving the area:**
- Set appropriate Boundary Radius
- Check spawn distances don't exceed boundary
- Verify spawn center is set correctly

**Combat mode not working:**
- Ensure player is tagged as "Player"
- Check detection range is large enough
- Verify Enable Combat Mode is checked

## Next Steps

Consider extending the system with:
- Health system for enemies
- AI weapon firing
- Formation flying
- Different AI difficulty levels
- Boss waves with special enemies
- Score/reward system for destroying enemies

---

**Need Help?** Check the script comments for detailed parameter descriptions, or review the code for customization options.
