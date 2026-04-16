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
        public float density;
        public float pressure;
    }

    List<Particle> particles = new List<Particle>();
    List<GameObject> particleVisuals = new List<GameObject>();

    [Header("Density")]
    public float smoothingRadius = 0.5f;

    [Header("Pressure")]
    public float restDensity = 1f;
    public float stiffness = 50f;

    [Header("Particle")]
    Transform particleParent;
    public int particleCount = 100;
    public float particleSize = 0.2f;
    public float particleSpacing = 0.25f;
    float particleRadius;

    [Header("Rendering")]
    public GameObject particlePrefab;
    List<SpriteRenderer> renderers = new List<SpriteRenderer>();

    [Header("Physics")]
    public float gravity = -9.8f;
    public float collisionDampening = 0.5f;

    [Header("Bounds")]
    public Vector2 boundsSize = new Vector2(8f, 8f);
    public Vector2 boundsCentre = Vector2.zero;

    [Header("Controllables")]
    public SpawnMode spawnMode;

    ComputeBuffer particleBuffer;
    public ComputeShader computeShader;
    int integrateKernel;


    
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
        boundsRenderer.startWidth = 0.018f;
        boundsRenderer.endWidth = 0.018f;
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

        int stride = sizeof(float) * 6;
        particleBuffer = new ComputeBuffer(particleCount, stride);
        particleBuffer.SetData(particles);

        integrateKernel = computeShader.FindKernel("Integrate");
    }


    void Update()
    {
        float dt = Time.deltaTime;
        particleRadius = particleSize * 0.5f;

        UpdateBoundsVisual();

        int count = Mathf.Min(particles.Count, particleVisuals.Count);

        // ComputeDensities();
        // ComputePressure();
        // ApplyPressureForces(dt);

        float maxDensity = 3.4f;
        computeShader.SetFloat("dt", Time.deltaTime);
        computeShader.SetFloat("gravity", gravity);
        computeShader.SetInt("particleCount", particles.Count);

        computeShader.SetBuffer(integrateKernel, "Particles", particleBuffer);

        int threadGroups = Mathf.CeilToInt(particles.Count / 256f);
        computeShader.Dispatch(integrateKernel, threadGroups, 1, 1);

        particleBuffer.GetData(particles);

        for (int i = 0; i < count; i++)
        {
            Particle p = particles[i];
            Color color = Color.blue;
            renderers[i].color = color;

            // p.position += p.velocity * dt;
            ResolveCollisions(ref p);
            particles[i] = p;
            if (i < particleVisuals.Count) { particleVisuals[i].transform.position = p.position; };
        }

        if (particleCount != previousParticleCount || spawnMode != previousSpawnMode)
        {
            randomPositionsDirty = true;
            RegenerateParticles();

            previousParticleCount = particleCount;
            previousSpawnMode = spawnMode;
        }

        particleBuffer.SetData(particles);
    }

    float SmoothingKernel(float r, float h) // TODO: Poly6 kernel for better performance and more accurate density estimation
    {
        if(r >= h) return 0f;

        float x = 1f - (r / h);
        return x * x;
    }

    void CacheExistingParticles()
    {
        particleVisuals.Clear();
        particles.Clear();
        renderers.Clear();

        if (particleParent == null) return;

        for (int i = 0; i < particleParent.childCount; i++)
        {
            Transform child = particleParent.GetChild(i);

            renderers.Add(child.GetComponent<SpriteRenderer>());
            particleVisuals.Add(child.gameObject);

            particles.Add(new Particle
            {
                position = child.position,
                velocity = Vector2.zero
            });
        }
    }

    ///// Density Calculation:

    //void ComputeDensities() 
    //{
    //    float h2 = smoothingRadius * smoothingRadius;

    //    for (int i = 0; i < particles.Count; i++) 
    //    {
    //        float density = 0f;
    //        Vector2 pos_i = particles[i].position;

    //        for (int j = 0; j < particles.Count; j++) 
    //        {
    //            Vector2 rVec = pos_i - particles[j].position;
    //            float r2 = rVec.sqrMagnitude;

    //            if (r2 < h2) 
    //            {
    //                density += SmoothingKernel(r2, h2);
    //            }
    //        }

    //        Particle p = particles[i];
    //        p.density = density;
    //        particles[i] = p;
    //    }
    //}

    //// Pressure Calculations

    //void ComputePressure() 
    //{
    //    for (int i = 0; i < particles.Count; i++) 
    //    {
    //        Particle p = particles[i];
    //        p.pressure = stiffness * (p.density - restDensity);
    //        particles[i] = p;
    //    }
    //}

    //void ApplyPressureForces(float dt) 
    //{
    //    for (int i = 0; i < particles.Count; i++) 
    //    {
    //        Vector2 force = Vector2.zero;
    //        Vector2 pos_i = particles[i].position;

    //        for (int j = 0; j < particles.Count; j++) 
    //        {
    //            if (i == j) continue;

    //            Vector2 rVec = pos_i - particles[j].position;
    //            float r = rVec.magnitude;

    //            if (r < smoothingRadius && r > 0f) 
    //            {
    //                float grad = (particles[i].pressure + particles[j].pressure) / (2f * particles[j].density);
    //                float q = 1f - (r / smoothingRadius);
    //                float kernalGrad = q; // Spiky kernel gradient (simplified for 2D)
    //                force += rVec.normalized * grad * kernalGrad;
    //            }
    //        }
            
    //        // gravity
    //        force += Vector2.up * gravity;

    //        //velocity
    //        Particle p = particles[i];
    //        p.velocity += force * dt;
    //        particles[i] = p;
    //    }
    //}

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

        renderers.Clear();
        // Ensure enough objects exist
        while (particleVisuals.Count < particleCount)
        {
            GameObject visual = Instantiate(particlePrefab, Vector2.zero, Quaternion.identity, particleParent);
            particleVisuals.Add(visual);
        }

        // Enable only what we need
        for (int i = 0; i < particleVisuals.Count; i++)
        {
            renderers.Add(particleVisuals[i].GetComponent<SpriteRenderer>());
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
