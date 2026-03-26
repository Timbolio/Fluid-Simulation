using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

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
    public Vector2 boundsSize = new Vector2(10f, 10f);
    public Vector2 boundsCentre = Vector2.zero;

    LineRenderer boundsRenderer;

    void Start()
    {
        
        // Bounds visualization
        boundsRenderer = GetComponent<LineRenderer>();
        boundsRenderer.positionCount = 5;
        boundsRenderer.startWidth = 0.008f;
        boundsRenderer.endWidth = 0.008f;
        boundsRenderer.useWorldSpace = true;
        boundsRenderer.material.color = Color.green;

        SpawnParticles();

    }


    void Update()
    {
        float dt = Time.deltaTime;
        particleRadius = particleSize * 0.5f;

        UpdateBoundsVisual();

        for (int i = 0; i < particles.Count; i++)
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


    }

    void SpawnParticles()
    {
        particles.Clear();
        particleVisuals.Clear();
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(particleCount));

        for (int i = 0; i < particleCount; i++)
        {
            int x = i % gridSize;
            int y = i / gridSize;

            Vector2 pos = boundsCentre + new Vector2(
                (x - gridSize * 0.5f) * particleSpacing,
                (y - gridSize * 0.5f) * particleSpacing
            );

            Particle p = new Particle { position = pos, velocity = Vector2.zero };

            particles.Add(p);

            GameObject visual = Instantiate(particlePrefab, pos, Quaternion.identity);
            visual.transform.localScale = Vector3.one * particleSize;
            particleVisuals.Add(visual);
        }
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
