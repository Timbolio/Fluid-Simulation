using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public enum SpawnMode
{
    Grid,
    Random
};

public class ParticleManager : MonoBehaviour
{
    struct Particle
    {
        public Vector2 position;
        public Vector2 velocity;
    }

    List<Particle> particles = new List<Particle>();
    List<GameObject> particleVisuals = new List<GameObject>();

    [Header("Particle")]
    Transform particleParent;
    public int particleCount = 100;
    public float particleSize = 0.2f;
    public float particleSpacing = 0.25f;
    float particleRadius;

    [Header("Rendering")]
    public GameObject particlePrefab;

    [Header("Physics")]
    public float gravity = -9.8f;
    public float collisionDampening = 0.5f;

    [Header("Bounds")]
    public Vector2 boundsSize = new Vector2(8f, 8f);
    public Vector2 boundsCentre = Vector2.zero;

    [Header("Controllables")]
    public SpawnMode spawnMode;

    
    LineRenderer boundsRenderer;
    List<Vector2> randomPositions = new List<Vector2>(); // Object pool for random spawn positions to avoid teleporting when changing particle count in random mode
    bool randomPositionsDirty = true;
    
    int previousParticleCount; // For editor change detection
    SpawnMode previousSpawnMode; 


    private void OnValidate()
    {
        if (!Application.isPlaying) 
        {
            randomPositionsDirty = true;
            RegenerateParticles();
        }

    }

    void Start()
    {
        boundsRenderer = GetComponent<LineRenderer>();
        boundsRenderer.positionCount = 5;
        boundsRenderer.startWidth = 0.008f;
        boundsRenderer.endWidth = 0.008f;
        boundsRenderer.useWorldSpace = true;
        boundsRenderer.material.color = Color.green;

        previousParticleCount = particleCount;
        previousSpawnMode = spawnMode;

        Transform existing = transform.Find("Particles");

        if (existing != null)
        {
            particleParent = existing;
            CacheExistingParticles();
        }
        else
        {
            RegenerateParticles(); // fallback if nothing exists
        }
    }


    void Update()
    {
        float dt = Time.deltaTime;
        particleRadius = particleSize * 0.5f;

        UpdateBoundsVisual();

        int count = Mathf.Min(particles.Count, particleVisuals.Count);

        for (int i = 0; i < count; i++)
        {
            Particle p = particles[i];
            // Apply gravity
            p.velocity.y += gravity * dt;
            // Update position
            p.position += p.velocity * dt;
            // Collision with bounds
            ResolveCollisions(ref p);
            particles[i] = p; // update the struct in the list
            
            // Update visual position
            particleVisuals[i].transform.position = p.position;
            particleVisuals[i].transform.localScale = Vector3.one * particleSize;
        }

        if (particleCount != previousParticleCount || spawnMode != previousSpawnMode)
        {
            randomPositionsDirty = true;
            RegenerateParticles();

            previousParticleCount = particleCount;
            previousSpawnMode = spawnMode;
        }
    }

    void CacheExistingParticles()
    {
        particleVisuals.Clear();
        particles.Clear();

        if (particleParent == null) return;

        for (int i = 0; i < particleParent.childCount; i++)
        {
            Transform child = particleParent.GetChild(i);

            particleVisuals.Add(child.gameObject);

            particles.Add(new Particle
            {
                position = child.position,
                velocity = Vector2.zero
            });
        }
    }


    void PrecomputeRandomPositions()
    {
        randomPositions.Clear();

        Vector2 half = boundsSize * 0.5f;

        float left = boundsCentre.x - half.x;
        float right = boundsCentre.x + half.x;
        float bottom = boundsCentre.y - half.y;
        float top = boundsCentre.y + half.y;

        for (int i = 0; i < particleCount; i++)
        {
            randomPositions.Add(new Vector2(
                Random.Range(left, right),
                Random.Range(bottom, top)
            ));
        }

        randomPositionsDirty = false;
    }

    Vector2 GetSpawnPosition(int index)
    {
        Vector2 half = boundsSize * 0.5f;

        float left = boundsCentre.x - half.x;
        float right = boundsCentre.x + half.x;
        float bottom = boundsCentre.y - half.y;
        float top = boundsCentre.y + half.y;

        switch (spawnMode)
        {
            case SpawnMode.Grid:
                {
                    int gridSize = Mathf.CeilToInt(Mathf.Sqrt(particleCount));

                    int x = index % gridSize;
                    int y = index / gridSize;

                    return boundsCentre + new Vector2(
                        (x - gridSize * 0.5f) * particleSpacing,
                        (y - gridSize * 0.5f) * particleSpacing
                    );
                }

            case SpawnMode.Random:
                {
                    
                    if (randomPositionsDirty || randomPositions.Count != particleCount)
                    {
                        PrecomputeRandomPositions();
                    }

                    return randomPositions[index];
                }
        }

        return boundsCentre;
    }

    /// <summary>
    /// 
    /// REGENERATION LOGIC:
    /// 
    /// </summary>
    void RegenerateParticles()
    {
        if (particleParent == null)
        {
            Transform existing = transform.Find("Particles");

            if (existing != null)
                particleParent = existing;
            else
            {
                particleParent = new GameObject("Particles").transform;
                particleParent.parent = transform;
            }
        }
        if (particleParent.childCount > 0) // for resync with editor between playtests
        {
            CacheExistingParticles();
        }
        // Ensure enough objects exist
        while (particleVisuals.Count < particleCount)
        {
            GameObject visual = Instantiate(particlePrefab, Vector2.zero, Quaternion.identity, particleParent);
            particleVisuals.Add(visual);
        }

        // Enable only what we need
        for (int i = 0; i < particleVisuals.Count; i++)
        {
            bool active = i < particleCount;
            particleVisuals[i].SetActive(active);

            if (active)
            {
                Vector2 pos = GetSpawnPosition(i);
                particleVisuals[i].transform.position = pos;

                if (i >= particles.Count)
                {
                    particles.Add(new Particle { position = pos, velocity = Vector2.zero });
                }
                else
                {
                    Particle p = particles[i];
                    p.position = pos;
                    p.velocity = Vector2.zero; // reset velocity when regenerating
                    particles[i] = p;
                }
            }
        }

        // Trim particle data list if needed
        if (particles.Count > particleCount) { particles.RemoveRange(particleCount, particles.Count - particleCount); };

    }


    void UpdateBoundsVisual()
    {
        Vector2 half = boundsSize * 0.5f;

        float left = boundsCentre.x - half.x;
        float right = boundsCentre.x + half.x;
        float bottom = boundsCentre.y - half.y;
        float top = boundsCentre.y + half.y;

        Vector3 bottomLeft = new Vector3(left, bottom, 0);
        Vector3 bottomRight = new Vector3(right, bottom, 0);
        Vector3 topRight = new Vector3(right, top, 0);
        Vector3 topLeft = new Vector3(left, top, 0);

        boundsRenderer.SetPosition(0, bottomLeft);
        boundsRenderer.SetPosition(1, bottomRight);
        boundsRenderer.SetPosition(2, topRight);
        boundsRenderer.SetPosition(3, topLeft);
        boundsRenderer.SetPosition(4, bottomLeft); // close loop
    }

    void ResolveCollisions(ref Particle p)
    {
        Vector2 half = boundsSize * 0.5f;

        float left = boundsCentre.x - half.x;
        float right = boundsCentre.x + half.x;
        float bottom = boundsCentre.y - half.y;
        float top = boundsCentre.y + half.y;

        // X
        if (p.position.x - particleRadius < left)
        {
            p.position.x = left + particleRadius;
            p.velocity.x *= -collisionDampening;
        }
        else if (p.position.x + particleRadius > right)
        {
            p.position.x = right - particleRadius;
            p.velocity.x *= -collisionDampening;
        }

        // Y
        if (p.position.y - particleRadius < bottom)
        {
            p.position.y = bottom + particleRadius;

            if (Mathf.Abs(p.velocity.y) < 0.1f)
                p.velocity.y = 0;
            else
                p.velocity.y *= -collisionDampening;

            p.velocity.x *= 0.9f; // friction
        }
        else if (p.position.y + particleRadius > top)
        {
            p.position.y = top - particleRadius;
            p.velocity.y *= -collisionDampening;
        }

    }

}
