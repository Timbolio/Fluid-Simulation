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

    Particle particle;

    [Header("Particle")]
    public float particleRadius = 0.1f;

    [Header("Rendering")]
    public GameObject particlePrefab;
    GameObject particleVisual;

    [Header("Physics")]
    public float gravity = -9.8f;
    public float collisionDampening = 0.5f;

    [Header("Bounds")]
    public Vector2 boundsSize = new Vector2(10f, 10f);
    public Vector2 boundsCentre = Vector2.zero;

    LineRenderer boundsRenderer;

    void Start()
    {
        particle = new Particle
        {
            position = Vector2.zero,
            velocity = Vector2.zero
        };

        particleVisual = Instantiate(particlePrefab, particle.position, Quaternion.identity);

        // Bounds visualization
        boundsRenderer = GetComponent<LineRenderer>();
        boundsRenderer.positionCount = 5;
        boundsRenderer.startWidth = 0.008f;
        boundsRenderer.endWidth = 0.008f;
        boundsRenderer.useWorldSpace = true;
        boundsRenderer.material.color = Color.green;

    }


    void Update()
    {
        float dt = Time.deltaTime;
        // Apply gravity
        particle.velocity += new Vector2(0, gravity) * dt;
        // Update position
        particle.position += particle.velocity * dt;
        UpdateandResolve();
        // Sync
        particleVisual.transform.position = particle.position;

    }

    void UpdateandResolve() 
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

        // X collisions
        if (particle.position.x - particleRadius < left)
        {
            particle.position.x = left + particleRadius;
            particle.velocity.x *= -collisionDampening; // simple bounce with damping
        }
        else if (particle.position.x + particleRadius > right)
        {
            particle.position.x = right - particleRadius;
            particle.velocity.x *= -collisionDampening;
        }

        // y collisions
        if (particle.position.y - particleRadius < bottom)
        {
            particle.position.y = bottom + particleRadius;

            if (Mathf.Abs(particle.velocity.y) < 0.1f)
            {
                particle.velocity.y = 0; // stop small bounces
            }
            else 
            {
                particle.velocity.y *= -collisionDampening;
            }
        }
        else if (particle.position.y + particleRadius > top)
        {
            particle.position.y = top - particleRadius;
            particle.velocity.y *= -collisionDampening;
        }

    }


}
