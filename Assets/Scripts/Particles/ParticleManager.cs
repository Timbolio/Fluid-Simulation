using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    
    struct Particle
    {
        public Vector2 position;
        public Vector2 velocity;
    }

    List<Particle> particles = new List<Particle>();

    // Settings
    public int particleCount = 200;
    public float spawnRadius = 3f;
    public float gravity = -9.8f;
    public float particleSize = 0.2f;

    void Start()
    {
        particles = new List<Particle>();
        for (int i = 0; i < particleCount; i++)
        {
            Particle p = new Particle
            {
                position = Random.insideUnitCircle * spawnRadius,
                velocity = Vector2.zero
            };
            particles.Add(p);
        }
    }

    
    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < particles.Count; i++)
        {
            Particle p = particles[i];

            // Apply gravity
            p.velocity += new Vector2(0, gravity) * dt;
            // Update position
            p.position += p.velocity * dt;
            // Save Back
            particles[i] = p;
        }
    }

    void OnDrawGizmos()
    {
        if (particles == null) return;

        Gizmos.color = Color.cyan;

        foreach (var p in particles)
        {
            Gizmos.DrawSphere(p.position, particleSize);
        }
    }
}
