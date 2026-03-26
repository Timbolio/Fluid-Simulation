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

    Particle particle;

    [Header("Rendering")]
    public GameObject particlePrefab;
    GameObject particleVisual;

    [Header("Physics")]
    public float gravity = -9.8f;

    void Start()
    {
        particle = new Particle
        {
            position = Vector2.zero,
            velocity = Vector2.zero
        };

        particleVisual = Instantiate(particlePrefab, particle.position, Quaternion.identity);
    }

    
    void Update()
    {
        float dt = Time.deltaTime;
        
        // Apply gravity
        particle.velocity += new Vector2(0, gravity) * dt;
        // Update position
        particle.position += particle.velocity * dt;
        // Sync
        particleVisual.transform.position = particle.position;


    }

}
