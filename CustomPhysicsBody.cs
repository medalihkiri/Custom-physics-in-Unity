using UnityEngine;
using System.Collections.Generic;

public class CustomPhysicsBody : MonoBehaviour
{
    public Vector3 velocity;
    public Vector3 angularVelocity;
    public float mass = 1.0f;
    public float restitution = 0.8f;
    public float staticFriction = 0.5f;
    public float dynamicFriction = 0.3f;
    public float fractureThreshold = 100f;
    public bool fractured = false;

    private Vector3 previousPosition;
    private Matrix4x4 inertiaTensorInverse;
    private Vector3[] shapeVertices; // World-space vertices of the mesh

    public float groundHeight = 0f;
    public LayerMask groundLayer;

    private MeshFilter meshFilter;

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("MeshFilter component is missing. CustomPhysicsBody requires a MeshFilter to define shape.");
            return;
        }

        previousPosition = transform.position;
        inertiaTensorInverse = CalculateInertiaTensorInverse();

        // Cache the local-space vertices from the mesh
        CacheShapeVertices();
    }

    private void CacheShapeVertices()
    {
        // Retrieve vertices from the MeshFilter's mesh
        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            Debug.LogError("MeshFilter has no mesh assigned.");
            return;
        }

        shapeVertices = mesh.vertices; // Local-space vertices
    }

    private Matrix4x4 CalculateInertiaTensorInverse()
    {
        Vector3 size = meshFilter.sharedMesh.bounds.size;
        float width = size.x, height = size.y, depth = size.z;

        // Approximate inertia tensor for a box
        float ix = (mass / 12f) * (height * height + depth * depth);
        float iy = (mass / 12f) * (width * width + depth * depth);
        float iz = (mass / 12f) * (width * width + height * height);

        Matrix4x4 inertiaTensor = Matrix4x4.Scale(new Vector3(ix, iy, iz));
        return inertiaTensor.inverse;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        Integrate(deltaTime);
    }

    public void Integrate(float deltaTime)
    {
        // Update velocity with gravity
        Vector3 acceleration = Physics.gravity;
        velocity += acceleration * deltaTime;

        // Update angular velocity
        angularVelocity += inertiaTensorInverse.MultiplyVector(Vector3.zero) * deltaTime;

        HandleGroundCollision();

        // Update position and orientation
        transform.position += velocity * deltaTime;
        transform.rotation *= Quaternion.Euler(angularVelocity * Mathf.Rad2Deg * deltaTime);
    }

    private void HandleGroundCollision()
    {
        if (shapeVertices == null) return;

        foreach (var localVertex in shapeVertices)
        {
            Vector3 worldVertex = transform.TransformPoint(localVertex); // Transform to world space
            if (worldVertex.y < groundHeight)
            {
                Vector3 contactNormal = Vector3.up;
                Vector3 contactPoint = new Vector3(worldVertex.x, groundHeight, worldVertex.z);

                CorrectPosition(contactNormal, groundHeight - worldVertex.y);
                ApplyCollisionImpulse(contactPoint, contactNormal);

                if (velocity.magnitude > fractureThreshold)
                {
                    CheckForFracture();
                }
            }
        }
    }

    public void CorrectPosition(Vector3 contactNormal, float penetrationDepth)
    {
        // Adjust position to resolve penetration
        transform.position += contactNormal * penetrationDepth;

        // Reflect linear and angular velocity
        velocity = Vector3.Reflect(velocity, contactNormal) * restitution;
        angularVelocity = Vector3.Reflect(angularVelocity, contactNormal) * restitution;
    }

    private void ApplyCollisionImpulse(Vector3 contactPoint, Vector3 contactNormal)
    {
        Vector3 relativeVelocity = velocity + Vector3.Cross(angularVelocity, contactPoint - transform.position);
        float normalVelocity = Vector3.Dot(relativeVelocity, contactNormal);

        if (normalVelocity < 0)
        {
            // Calculate normal impulse
            float impulseMagnitude = -(1 + restitution) * normalVelocity;
            impulseMagnitude /= (1 / mass) + Vector3.Dot(contactNormal,
                                Vector3.Cross(inertiaTensorInverse.MultiplyVector(Vector3.Cross(contactPoint - transform.position, contactNormal)),
                                contactPoint - transform.position));

            Vector3 impulse = impulseMagnitude * contactNormal;

            // Apply impulse to linear and angular velocities
            velocity += impulse / mass;
            angularVelocity += inertiaTensorInverse.MultiplyVector(Vector3.Cross(contactPoint - transform.position, impulse));

            // Friction impulse (tangential)
            Vector3 tangentVelocity = relativeVelocity - normalVelocity * contactNormal;
            if (tangentVelocity.magnitude > 0.001f)
            {
                Vector3 frictionDirection = -tangentVelocity.normalized;
                float frictionMagnitude = Mathf.Min(tangentVelocity.magnitude, staticFriction * impulseMagnitude);
                Vector3 frictionImpulse = frictionDirection * frictionMagnitude;

                velocity += frictionImpulse / mass;
                angularVelocity += inertiaTensorInverse.MultiplyVector(Vector3.Cross(contactPoint - transform.position, frictionImpulse));
            }
        }
    }

    public void CheckForFracture()
    {
        if (!fractured && transform.childCount > 0) // Only fracture if there are child objects
        {
            fractured = true;
            Shatter();
        }
    }

    private void Shatter()
    {
        // Only fracture if there are child objects
        if (transform.childCount == 0)
        {
            Debug.Log("No child objects to fracture.");
            return;
        }

        // Iterate through children and apply physics to them.
        List<Transform> children = new List<Transform>();
        foreach (Transform child in transform)
        {
            children.Add(child); // Store children in a list
        }

        // Process each child
        foreach (Transform child in children)
        {
            if (child != null)
            {
                // Detach the child from the parent
                child.SetParent(null);
                child.gameObject.SetActive(true);

                // Calculate and apply energy to the fragment
                float totalEnergy = 0.5f * mass * velocity.sqrMagnitude;
                float energyPerFragment = totalEnergy / children.Count;

                CustomPhysicsBody fragmentBody = child.gameObject.AddComponent<CustomPhysicsBody>();
                fragmentBody.velocity = velocity / children.Count + Random.insideUnitSphere * Mathf.Sqrt(2 * energyPerFragment / fragmentBody.mass);

                // Optionally, you can add custom behavior specific to fragments here
            }
        }

        // Disable the parent object after fracturing
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}