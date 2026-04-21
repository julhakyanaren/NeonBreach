using System.Collections.Generic;
using UnityEngine;

public class SpawnPositionGenerator : MonoBehaviour
{
    [Header("Spawn Area")]
    [Tooltip("Center of the allowed spawn area.")]
    [SerializeField] private Transform spawnAreaCenter;

    [Tooltip("Size of the allowed spawn area. X = width, Z = depth.")]
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(20f, 1f, 20f);

    [Tooltip("Fixed Y position used for all generated spawn points.")]
    [SerializeField] private float spawnHeight = 1.1f;

    [Header("Generation Settings")]
    [Tooltip("Maximum number of attempts for generating all requested spawn positions.")]
    [SerializeField] private int maxGenerationAttempts = 200;

    [Tooltip("Minimum allowed distance from player.")]
    [SerializeField] private float minDistanceFromPlayer = 5f;

    [Tooltip("Minimum allowed distance between generated spawn positions.")]
    [SerializeField] private float minDistanceBetweenSpawnPoints = 2f;

    [Header("Overlap Check")]
    [Tooltip("Half extents of the overlap box used to validate spawn position.")]
    [SerializeField] private Vector3 checkBoxHalfExtents = new Vector3(0.6f, 0.8f, 0.6f);

    [Tooltip("Layers that make spawn position invalid.")]
    [SerializeField] private LayerMask blockedMask;

    [Header("References")]
    [Tooltip("Target player transform used for minimum distance validation.")]
    [SerializeField] private Transform playerTransform;

    [Header("Debug")]
    [Tooltip("Draw spawn area gizmo.")]
    [SerializeField] private bool drawSpawnArea = true;

    [Tooltip("Draw generated points gizmo.")]
    [SerializeField] private bool drawGeneratedPoints = true;

    [SerializeField] private List<Vector3> debugGeneratedPoints = new List<Vector3>();

    public List<Vector3> GenerateSpawnPositions(int count)
    {
        List<Vector3> generatedPoints = new List<Vector3>();

        if (count <= 0)
        {
            debugGeneratedPoints = generatedPoints;
            return generatedPoints;
        }

        if (spawnAreaCenter == null)
        {
            Debug.LogWarning($"{name}: SpawnPositionGenerator has no spawnAreaCenter assigned.");
            debugGeneratedPoints = generatedPoints;
            return generatedPoints;
        }

        int attempts = 0;

        while (generatedPoints.Count < count && attempts < maxGenerationAttempts)
        {
            attempts++;

            Vector3 candidatePoint = GetRandomPointInsideArea();

            if (!IsFarEnoughFromPlayer(candidatePoint))
            {
                continue;
            }

            if (!IsFarEnoughFromOtherPoints(candidatePoint, generatedPoints))
            {
                continue;
            }

            if (IsBlocked(candidatePoint))
            {
                continue;
            }

            generatedPoints.Add(candidatePoint);
        }

        debugGeneratedPoints = generatedPoints;

        if (generatedPoints.Count < count)
        {
            Debug.LogWarning($"{name}: Only generated {generatedPoints.Count} of {count} requested spawn positions.");
        }

        return generatedPoints;
    }

    private Vector3 GetRandomPointInsideArea()
    {
        Vector3 center = spawnAreaCenter.position;

        float halfX = spawnAreaSize.x * 0.5f;
        float halfZ = spawnAreaSize.z * 0.5f;

        float randomX = Random.Range(center.x - halfX, center.x + halfX);
        float randomZ = Random.Range(center.z - halfZ, center.z + halfZ);

        Vector3 point = new Vector3(randomX, spawnHeight, randomZ);
        return point;
    }

    private bool IsFarEnoughFromPlayer(Vector3 point)
    {
        if (playerTransform == null)
        {
            return true;
        }

        Vector3 flatPoint = point;
        flatPoint.y = 0f;

        Vector3 flatPlayerPosition = playerTransform.position;
        flatPlayerPosition.y = 0f;

        float distance = Vector3.Distance(flatPoint, flatPlayerPosition);

        if (distance < minDistanceFromPlayer)
        {
            return false;
        }

        return true;
    }

    private bool IsFarEnoughFromOtherPoints(Vector3 point, List<Vector3> existingPoints)
    {
        for (int i = 0; i < existingPoints.Count; i++)
        {
            Vector3 existingPoint = existingPoints[i];

            Vector3 flatPoint = point;
            flatPoint.y = 0f;

            Vector3 flatExistingPoint = existingPoint;
            flatExistingPoint.y = 0f;

            float distance = Vector3.Distance(flatPoint, flatExistingPoint);

            if (distance < minDistanceBetweenSpawnPoints)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsBlocked(Vector3 point)
    {
        bool hasBlockingCollider = Physics.OverlapBox(
            point,
            checkBoxHalfExtents,
            Quaternion.identity,
            blockedMask,
            QueryTriggerInteraction.Ignore
        ).Length > 0;

        if (hasBlockingCollider)
        {
            return true;
        }

        return false;
    }

    private void OnValidate()
    {
        spawnHeight = Mathf.Max(0f, spawnHeight);
        maxGenerationAttempts = Mathf.Max(1, maxGenerationAttempts);
        minDistanceFromPlayer = Mathf.Max(0f, minDistanceFromPlayer);
        minDistanceBetweenSpawnPoints = Mathf.Max(0f, minDistanceBetweenSpawnPoints);

        checkBoxHalfExtents.x = Mathf.Max(0.05f, checkBoxHalfExtents.x);
        checkBoxHalfExtents.y = Mathf.Max(0.05f, checkBoxHalfExtents.y);
        checkBoxHalfExtents.z = Mathf.Max(0.05f, checkBoxHalfExtents.z);
    }

    private void OnDrawGizmosSelected()
    {
        if (drawSpawnArea && spawnAreaCenter != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(
                new Vector3(spawnAreaCenter.position.x, spawnHeight, spawnAreaCenter.position.z),
                new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.z)
            );
        }

        if (drawGeneratedPoints)
        {
            Gizmos.color = Color.green;

            for (int i = 0; i < debugGeneratedPoints.Count; i++)
            {
                Vector3 point = debugGeneratedPoints[i];
                Gizmos.DrawWireSphere(point, 0.25f);
                Gizmos.DrawWireCube(point, checkBoxHalfExtents * 2f);
            }
        }
    }

    [ContextMenu("Generate Test Points")]
    private void GenerateTestPoints()
    {
        GenerateSpawnPositions(10);
    }
}