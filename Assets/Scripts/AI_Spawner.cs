using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI_Spawner : MonoBehaviour
{
    [Header("Enemy Prefab")]
    [Tooltip("The enemy aircraft prefab to spawn (must have AI_Movement component)")]
    public GameObject enemyPrefab;
    
    [Header("Wave Configuration")]
    [Tooltip("Number of enemies in the first wave")]
    public int initialEnemiesPerWave = 3;
    
    [Tooltip("How many additional enemies per wave (progressive difficulty)")]
    public int enemyIncreasePerWave = 2;
    
    [Tooltip("Maximum enemies that can spawn in a single wave")]
    public int maxEnemiesPerWave = 15;
    
    [Tooltip("Delay in seconds between waves")]
    public float delayBetweenWaves = 20f;
    
    [Tooltip("Start spawning waves automatically on game start")]
    public bool autoStart = true;
    
    [Header("Spawn Area")]
    [Tooltip("Center point for spawning (uses this GameObject's position if not set)")]
    public Transform spawnCenter;
    
    [Tooltip("Minimum distance from center to spawn enemies")]
    public float minSpawnDistance = 500f;
    
    [Tooltip("Maximum distance from center to spawn enemies")]
    public float maxSpawnDistance = 1000f;
    
    [Tooltip("Minimum spawn altitude")]
    public float minSpawnAltitude = 200f;
    
    [Tooltip("Maximum spawn altitude")]
    public float maxSpawnAltitude = 400f;
    
    [Header("Combat Settings")]
    [Tooltip("Should spawned enemies have combat mode enabled?")]
    public bool enemiesInCombatMode = true;
    
    [Header("Wave Info Display")]
    [Tooltip("Display wave information in console")]
    public bool showWaveInfo = true;
    
    // Private variables
    private int currentWave = 0;
    private int enemiesAlive = 0;
    private bool waveInProgress = false;
    private List<GameObject> activeEnemies = new List<GameObject>();
    
    void Start()
    {
        // Use this GameObject's position as spawn center if not set
        if (spawnCenter == null)
        {
            spawnCenter = transform;
        }
        
        // Validate enemy prefab
        if (enemyPrefab == null)
        {
            Debug.LogError("[AI_Spawner] Enemy prefab is not assigned! Please assign an enemy prefab in the inspector.");
            return;
        }
        
        if (enemyPrefab.GetComponent<AI_Movement>() == null)
        {
            Debug.LogWarning("[AI_Spawner] Enemy prefab doesn't have AI_Movement component. AI behavior may not work correctly.");
        }
        
        if (autoStart)
        {
            StartNextWave();
        }
    }
    
    void Update()
    {
        // Clean up destroyed enemies from the list
        activeEnemies.RemoveAll(enemy => enemy == null);
        enemiesAlive = activeEnemies.Count;
        
        // Check if wave is complete and start next wave
        if (waveInProgress && enemiesAlive == 0)
        {
            waveInProgress = false;
            StartCoroutine(WaveCompleteDelay());
        }
    }
    
    IEnumerator WaveCompleteDelay()
    {
        if (showWaveInfo)
        {
            Debug.Log($"[AI_Spawner] Wave {currentWave} complete! Next wave in {delayBetweenWaves} seconds...");
        }
        
        yield return new WaitForSeconds(delayBetweenWaves);
        StartNextWave();
    }
    
    public void StartNextWave()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("[AI_Spawner] Cannot start wave: Enemy prefab is not assigned!");
            return;
        }
        
        currentWave++;
        waveInProgress = true;
        
        // Calculate enemies for this wave
        int enemiesToSpawn = Mathf.Min(
            initialEnemiesPerWave + (enemyIncreasePerWave * (currentWave - 1)),
            maxEnemiesPerWave
        );
        
        if (showWaveInfo)
        {
            Debug.Log($"[AI_Spawner] Starting Wave {currentWave} - Spawning {enemiesToSpawn} enemies");
        }
        
        // Spawn enemies
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();
        }
    }
    
    void SpawnEnemy()
    {
        // Generate random spawn position
        Vector3 spawnPosition = GenerateSpawnPosition();
        
        // Generate random rotation (facing somewhat towards center)
        Vector3 directionToCenter = (spawnCenter.position - spawnPosition).normalized;
        Quaternion spawnRotation = Quaternion.LookRotation(directionToCenter) * Quaternion.Euler(0, Random.Range(-45f, 45f), 0);
        
        // Instantiate enemy
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, spawnRotation);
        
        // Configure AI_Movement component if it exists
        AI_Movement aiMovement = enemy.GetComponent<AI_Movement>();
        if (aiMovement != null)
        {
            aiMovement.enableCombatMode = enemiesInCombatMode;
            
            // Optional: Vary AI parameters slightly for each enemy
            aiMovement.baseSpeed += Random.Range(-10f, 10f);
            aiMovement.turnSpeed += Random.Range(-0.3f, 0.3f);
        }
        
        // Add to active enemies list
        activeEnemies.Add(enemy);
        enemiesAlive++;
    }
    
    Vector3 GenerateSpawnPosition()
    {
        // Generate random direction
        Vector2 randomCircle = Random.insideUnitCircle.normalized;
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        
        // Calculate position
        Vector3 spawnPos = spawnCenter.position;
        spawnPos.x += randomCircle.x * distance;
        spawnPos.z += randomCircle.y * distance;
        spawnPos.y = Random.Range(minSpawnAltitude, maxSpawnAltitude);
        
        return spawnPos;
    }
    
    // Public methods for external control
    
    public void ForceNextWave()
    {
        if (!waveInProgress)
        {
            StartNextWave();
        }
    }
    
    public void StopWaves()
    {
        StopAllCoroutines();
        waveInProgress = false;
    }
    
    public void ClearAllEnemies()
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        activeEnemies.Clear();
        enemiesAlive = 0;
    }
    
    public void ResetSpawner()
    {
        StopWaves();
        ClearAllEnemies();
        currentWave = 0;
        waveInProgress = false;
    }
    
    // Getters for wave information
    public int GetCurrentWave() { return currentWave; }
    public int GetEnemiesAlive() { return enemiesAlive; }
    public bool IsWaveInProgress() { return waveInProgress; }
    
    // Visualize spawn area in editor
    void OnDrawGizmos()
    {
        Vector3 center = spawnCenter != null ? spawnCenter.position : transform.position;
        
        // Draw spawn area boundaries
        Gizmos.color = Color.green;
        DrawCircle(center, minSpawnDistance, 32);
        
        Gizmos.color = Color.yellow;
        DrawCircle(center, maxSpawnDistance, 32);
        
        // Draw altitude range
        Gizmos.color = Color.cyan;
        Vector3 minAltPos = center;
        minAltPos.y = minSpawnAltitude;
        Vector3 maxAltPos = center;
        maxAltPos.y = maxSpawnAltitude;
        
        Gizmos.DrawWireSphere(minAltPos, 50f);
        Gizmos.DrawWireSphere(maxAltPos, 50f);
    }
    
    void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}
