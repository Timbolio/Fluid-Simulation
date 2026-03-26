using System.Collections.Generic;
using UnityEngine;

public class Particle : MonoBehaviour
{
    // This script will contain the behaviour of each particle of fluid (droplet), and will run calculations for density, pressure, and viscosity forces. It will also handle the movement of the particle based on these forces.

    public List<Vector2> particles = new List<Vector2>();

    void Start()
    {
        
    }
    void OnDrawGizmos() // Starting with gizmos to visualize the particles in the editor, we will later replace this with actual rendering of the particles probably using sprites or meshes.
    {
        if (particles == null || particles.Count == 0)
        {
            particles = new List<Vector2>();

            for (int i = 0; i < 200; i++)
            {
                particles.Add(Random.insideUnitCircle * 3f);
            }
        }

        Gizmos.color = Color.cyan;

        foreach (var p in particles)
        {
            Gizmos.DrawSphere(p, 0.05f);
        }
    }
}
