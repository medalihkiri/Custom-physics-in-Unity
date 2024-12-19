# **Introduction**

This paper presents a **custom rigid body fracture system** implemented in Unity, designed to simulate realistic fractures in objects based on their kinetic energy and physical interactions. The system is powered by a physics engine built entirely using **C# code**, without relying on Unity's built-in physics components for fracture. The simulation incorporates several core physics principles, including **impulse-based collision response**, **restitution**, **energy redistribution** during fracture, and **rotational dynamics**.

The goal of this work is to provide a flexible, realistic, and customizable fracture system that can be used for various simulations in Unity, including object destruction, dynamic debris, and realistic object behavior during collisions.

---

## **System Overview**

The core of the system revolves around the **`CustomPhysicsBody`** class, which simulates the physics of an object. This class handles an object's **velocity**, **angular velocity**, **collision detection**, **collision response**, and **fracture logic**. When the object reaches a specific threshold of velocity, it fractures into child objects (fragments), and the energy of the object is redistributed to these fragments. Each fragment behaves independently, inheriting velocity and angular velocity from the parent object.

---

## **How the System Works**

### **Step 1: Initialization**

When a **`CustomPhysicsBody`** object is initialized, the system calculates the **inertia tensor**, which describes how the mass is distributed relative to the object's center of mass. The inertia tensor is essential for calculating the object's response to applied torques (rotational forces). Additionally, the object's **mesh vertices** are cached to detect collisions with the ground or other objects.

### **Step 2: Physics Update (Integration)**

Each frame, the physics system updates the object's **velocity** and **angular velocity**:

- **Velocity** is updated using gravity and collision forces.
- **Angular velocity** is updated using rotational forces (torques) applied from collisions.

### **Step 3: Collision Handling**

The object's position is checked for collisions. When a collision occurs, the system calculates the **impulse** applied to the object. This impulse is based on the relative velocity between the object and the ground (or another object) and includes:

- **Normal Impulse**: This is the impulse applied along the contact normal (the direction perpendicular to the surface of contact).
- **Friction Impulse**: This is the impulse applied along the tangential direction to simulate friction between the object and the surface it collides with.

### **Step 4: Fracturing**

If the object's velocity exceeds a predefined fracture threshold, it fractures. The object's total energy is calculated by considering both its **linear kinetic energy** (due to velocity) and **rotational kinetic energy** (due to angular velocity). This energy is then divided equally among the fragments. Each fragment inherits a portion of the original velocity and angular velocity, and a randomized impulse is applied to each fragment to make the fracture appear more natural.

---

## **Physics Concepts and Mathematical Formulas**

This system is based on fundamental **classical mechanics**, and the physics principles are implemented using mathematical formulas. The following sections describe the core physics concepts used in the system, along with the corresponding **C# code** that implements them.

---

### **1. Restitution (Elasticity/Bounciness)**

**Concept**:

Restitution defines how an object behaves after a collision. A **restitution coefficient** of 1 indicates a perfectly elastic collision (no energy loss), while a coefficient of 0 indicates a perfectly inelastic collision (maximum energy loss).

**Formula**:

The velocity after collision is scaled by the restitution coefficient e:

$$
v_{after} = e \cdot v_{before}
$$

Where:

- e is the coefficient of restitution.
- v_{after} is the velocity after the collision.
- v_{before} is the velocity before the collision.

**C# Code Implementation**:
In Unity, the reflection of velocity and angular velocity after a collision is handled using the **`Vector3.Reflect`** function:

```csharp
velocity = Vector3.Reflect(velocity, contactNormal) * restitution;
angularVelocity = Vector3.Reflect(angularVelocity, contactNormal) * restitution;
```

This **reflects** the velocity and angular velocity, reversing them along the contact normal (the direction perpendicular to the surface the object is colliding with) and applying the restitution coefficient to control the "bounce."

---

### **2. Fracture (Energy Redistribution)**

**Concept**:

When an object fractures, the total energy (both **linear kinetic energy** and **rotational kinetic energy**) is redistributed to the fragments. The energy is equally divided among all fragments, ensuring that each fragment behaves independently but conserves the original energy.

**Formula**:

The total energy of the object is computed as the sum of its **linear kinetic energy** and **rotational kinetic energy**:

$$
E_{total} = \frac{1}{2} m v^2 + \frac{1}{2} \omega^T I \omega
$$

Where:

- E_{total} is the total energy.
- m is the mass of the object.
- v is the velocity vector.
- ω is the angular velocity vector.
- I is the inertia tensor.

The energy for each fragment is:

$$
E_{fragment} = \frac{E_{total}}{N}
$$

Where N is the number of fragments.

**C# Code Implementation**:
The total energy of the object is calculated from both its linear and angular velocities:

```csharp
float totalEnergy = 0.5f * mass * velocity.sqrMagnitude + 0.5f * angularVelocity.sqrMagnitude * mass;
```

The energy per fragment is calculated by dividing the total energy by the number of fragments:

```csharp
float energyPerFragment = totalEnergy / transform.childCount;
```

Each fragment inherits a portion of the original velocity and angular velocity:

```csharp
fragmentBody.velocity = velocity / transform.childCount + Random.insideUnitSphere * Mathf.Sqrt(2 * energyPerFragment / fragmentBody.mass);
fragmentBody.angularVelocity = angularVelocity + Random.insideUnitSphere * energyTransferCoefficient;
```

This code ensures that fragments inherit energy from the original object and are given some random variation to simulate realistic motion after fracture.

---

### **3. Impulse-Based Collision Response**

**Concept**:

When two objects collide, an **impulse** is applied to each object. This impulse changes the velocity of the object based on the collision normal and the relative velocity between the objects.

**Formula**:

$$
J = - (1 + e) \frac{\vec{v} \cdot \vec{n}}{\frac{1}{m} + \vec{n} \cdot (I^{-1} (\vec{r} \times \vec{n}) \times \vec{r})}
$$

Where:

- J is the impulse magnitude.
- e is the restitution coefficient.
- \vec{v} is the relative velocity.
- \vec{n} is the contact normal.
- I is the inertia tensor.
- m is the mass of the object.
- \vec{r} is the vector from the center of mass to the collision point.

**C# Code Implementation**:
The impulse magnitude is calculated based on the relative velocity and the inertia tensor:

```csharp
float impulseMagnitude = -(1 + restitution) * normalVelocity;
impulseMagnitude /= (1 / mass) + Vector3.Dot(contactNormal,
                            Vector3.Cross(inertiaTensorInverse.MultiplyVector(Vector3.Cross(contactPoint - transform.position, contactNormal)),
                            contactPoint - transform.position));
```

This impulse is then applied to both the **linear velocity** and **angular velocity**:

```csharp
Vector3 impulse = impulseMagnitude * contactNormal;
velocity += impulse / mass;
angularVelocity += inertiaTensorInverse.MultiplyVector(Vector3.Cross(contactPoint - transform.position, impulse));
```

---

### **4. Gravity**

**Concept**:

Gravity is a force that pulls objects downward, which affects their velocity over time. This is modeled as a constant acceleration due to gravity.

**Formula**:

$$
\vec{a} = \vec{g}
$$

Where:

- \vec{g} is the gravitational acceleration.

**C# Code Implementation**:
Gravity is applied each frame to modify the object's velocity:

```csharp
Vector3 acceleration = Physics.gravity;
velocity += acceleration * deltaTime;
```

---

### **5. Friction (Static and Dynamic)**

**Concept**:

Friction opposes the motion between two objects. **Static friction** prevents initial motion, while **dynamic friction** resists ongoing motion. The frictional force is proportional to the normal force.

**Formula**:

$$
F_{friction} = \mu F_{normal}
$$

Where:

- \mu is the coefficient of friction.
- F_{normal} is the normal force.

**C# Code Implementation**:
The friction force is computed based on the tangential velocity (the velocity parallel to the contact surface) and applied to reduce the object's speed:

```csharp
Vector3 frictionDirection = -tangentVelocity.normalized;
float frictionMagnitude = Mathf.Min(tangentVelocity.magnitude, staticFriction * impulseMagnitude);
Vector3 frictionImpulse = frictionDirection * frictionMagnitude;

velocity += frictionImpulse / mass;
angularVelocity += inertiaTensorInverse.MultiplyVector(Vector3.Cross(contactPoint - transform.position, frictionImpulse));
```

---

### **6. Impulse Direction in Fracture**

**Concept**:
During fracture, each fragment receives an impulse in a random direction. This helps simulate the natural spread of debris when an object breaks apart.

**Formula**:
The impulse direction for each fragment is randomized using the collision normal:

$$
\vec{v}_{fragment} = \vec{v}_{original} + \text{random impulse direction}
$$

**C# Code Implementation**:
The impulse direction is calculated using the cross product of the contact normal and a random vector:

```csharp
Vector3 impulseDirection = Vector3.Cross(contactNormal, Random.onUnitSphere).normalized;
fragmentBody.velocity += impulseDirection * Mathf.Sqrt(energyPerFragment / fragmentBody.mass);
```

---
**Reverence**:
-[rigidBodyFracture_I3D2018_preprint.pdf](https://github.com/user-attachments/files/18191568/rigidBodyFracture_I3D2018_preprint.pdf)
-[Projet1_article.pdf](https://github.com/user-attachments/files/18191590/Projet1_article.pdf)

##



