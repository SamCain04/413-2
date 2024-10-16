using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    private Rigidbody rigid;
    private Neighborhood neighborhood;

    public float obstacleAvoidanceDistance = 3f; // Distance to start avoiding obstacles
    public float obstacleBufferDistance = 1f; // Distance to maintain from obstacles
    public float avoidanceWeight = 1f; // Weight for steering away from obstacles
    public LayerMask obstacleLayer; // Layer for obstacles

    void Awake()
    {
        neighborhood = GetComponent<Neighborhood>();
        rigid = GetComponent<Rigidbody>();
        vel = Random.onUnitSphere * Spawner.SETTINGS.velocity;
        LookAhead();
        Colorize();
    }

    void FixedUpdate()
    {
        BoidSettings bSet = Spawner.SETTINGS;
        Vector3 sumVel = Vector3.zero;

        // Attractor behavior
        Vector3 delta = Attractor.POS - pos;
        if (delta.magnitude > bSet.attractPushDist)
        {
            sumVel += delta.normalized * bSet.attractPull;
        }
        else
        {
            sumVel -= delta.normalized * bSet.attractPush;
        }

        // Neighborhood avoidance
        Vector3 velAvoid = Vector3.zero;
        Vector3 tooNearPos = neighborhood.avgNearPos;
        if (tooNearPos != Vector3.zero)
        {
            velAvoid = pos - tooNearPos;
            velAvoid.Normalize();
            sumVel += velAvoid * bSet.nearAvoid;
        }

        // Alignment
        Vector3 velAlign = neighborhood.avgVel;
        if (velAlign != Vector3.zero)
        {
            velAlign.Normalize();
            sumVel += velAlign * bSet.velMatching;
        }

        // Centering
        Vector3 velCenter = neighborhood.avgPos;
        if (velCenter != Vector3.zero)
        {
            velCenter -= transform.position;
            velCenter.Normalize();
            sumVel += velCenter * bSet.flockCentering;
        }

        // Obstacle avoidance
        Vector3 obstacleAvoidanceForce = CalculateObstacleAvoidance();
        sumVel += obstacleAvoidanceForce;

        // Normalize and set the new velocity
        sumVel.Normalize();
        vel = Vector3.Lerp(vel.normalized, sumVel, bSet.velocityEasing);
        vel *= bSet.velocity;

        LookAhead();
    }

    Vector3 CalculateObstacleAvoidance()
    {
        Vector3 force = Vector3.zero;
        Vector3 rayDirection = transform.forward;

        // Cast rays to detect obstacles
        Vector3 leftRay = Quaternion.Euler(0, -10, 0) * rayDirection;  // Slightly left
        Vector3 rightRay = Quaternion.Euler(0, 10, 0) * rayDirection;  // Slightly right

        RaycastHit hit;

        // Check left ray
        if (Physics.Raycast(transform.position, leftRay, out hit, obstacleAvoidanceDistance, obstacleLayer))
        {
            Vector3 steerDirection = (transform.position - hit.point).normalized;
            force += steerDirection * (obstacleAvoidanceDistance - hit.distance + obstacleBufferDistance);
        }

        // Check right ray
        if (Physics.Raycast(transform.position, rightRay, out hit, obstacleAvoidanceDistance, obstacleLayer))
        {
            Vector3 steerDirection = (transform.position - hit.point).normalized;
            force += steerDirection * (obstacleAvoidanceDistance - hit.distance + obstacleBufferDistance);
        }

        return force * avoidanceWeight; // Scale the force by the avoidance weight
    }

    void LookAhead()
    {
        transform.LookAt(pos + rigid.velocity);
    }

    void Colorize()
    {
        Color randColor = Random.ColorHSV(0, 1, 0.5f, 1, 0.5f, 1);
        Renderer[] rends = gameObject.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in rends)
        {
            r.material.color = randColor;
        }

        TrailRenderer trend = GetComponent<TrailRenderer>();
        trend.startColor = randColor;
        randColor.a = 0;
        trend.endColor = randColor;
        trend.endWidth = 0;
    }

    public Vector3 pos
    {
        get { return transform.position; }
        private set { transform.position = value; }
    }

    public Vector3 vel
    {
        get { return rigid.velocity; }
        private set { rigid.velocity = value; }
    }
}
