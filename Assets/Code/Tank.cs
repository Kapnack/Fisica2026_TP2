using System;
using UnityEngine;

[Serializable]
public class Tank
{
    [Header("Movement")]
    [SerializeField] private float acceleration = 30.0f;
    [SerializeField] private Vector2 position;
    [SerializeField] private Vector2 velocity;

    [Header("Dimensions")]
    [SerializeField] private Vector2 size;
    [SerializeField] private Vector2 cannonSize = new Vector2(2f, 0.5f);

    [Header("Canion Data")]
    [SerializeField] public float cannonRotation = 0.0f;
    [SerializeField] public float canionForce = 0.0f;

    public bool shoot = false;

    public Vector2 CanionDir
    {
        get
        {
            float radians = cannonRotation * Mathf.Deg2Rad;

            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }
    }

    public Vector2 CannonTipPosition
    {
        get
        {
            float rad = cannonRotation * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            return position + direction * cannonSize.x;
        }
    }


    [Header("Physics")]
    [SerializeField] private float restitution = 0.8f;
    [SerializeField] private float mass = 1f;
    [SerializeField] private float friction = 0.3f;

    private bool isGrounded = false;

    public Vector2 Position => position;
    public Vector2 CannonSize => cannonSize;
    public float CannonRotation => cannonRotation;
    public Vector2 TankSize => size;

    public void CheckInput(float deltaTime)
    {
        if (Input.GetKey(KeyCode.LeftArrow))
            velocity += Vector2.left * (acceleration * deltaTime);
        else if (Input.GetKey(KeyCode.RightArrow))
            velocity += Vector2.right * (acceleration * deltaTime);

        if (Input.GetKey(KeyCode.I))
            cannonRotation += acceleration * 2f * deltaTime;
        else if (Input.GetKey(KeyCode.O))
            cannonRotation -= acceleration * 2f * deltaTime;

        cannonRotation = Mathf.Clamp(cannonRotation, 0f, 180f);
    }

    public void Integrate(float deltaTime, float gravity)
    {
        if (!isGrounded)
            velocity += Vector2.down * (gravity * deltaTime);
        else if (velocity.y < 0)
            velocity.y = 0;

        position += velocity * deltaTime;

        isGrounded = false;
    }

    public void CheckWallCollision(Wall wall)
    {
        Vector2 wallVec = wall.pointB - wall.pointA;
        float wallLen = wallVec.magnitude;

        if (wallLen < Mathf.Epsilon)
            return;

        Vector2 wallDir = wallVec / wallLen;

        float t = Vector2.Dot(position - wall.pointA, wallDir);
        if (t < -size.x * 0.5f || t > wallLen + size.x * 0.5f)
            return;

        Vector2 normal = new Vector2(-wallDir.y, wallDir.x);
        Vector2 halfSize = size * 0.5f;
        float distToWall = Vector2.Dot(position - wall.pointA, normal);

        float projectedRadius = Mathf.Abs(halfSize.x * normal.x) + Mathf.Abs(halfSize.y * normal.y);
        float penetration = (projectedRadius + wall.thickness) - Mathf.Abs(distToWall);

        if (penetration < 0 || Mathf.Approximately(penetration, 0))
            return;

        float sign = Mathf.Sign(distToWall);
        Vector2 n = normal * sign;
        position += n * penetration;

        float vDotN = Vector2.Dot(velocity, n);
        if (vDotN < 0)
        {
            Vector2 vNormal = vDotN * n;
            Vector2 vTangent = velocity - vNormal;

            float combinedFriction = Mathf.Clamp01((friction + wall.friction) * 0.5f);

            velocity = (vTangent * (1.0f - combinedFriction)) - (vNormal * restitution);

            if (n.y > 0.7f)
            {
                isGrounded = true;

                if (velocity.sqrMagnitude < Mathf.Epsilon * Mathf.Epsilon)
                    velocity = Vector2.zero;
            }
        }
    }

    public void CheckTankCollision(Tank other)
    {
        if (this == other) 
            return;

        Vector2 delta = other.position - this.position;
        Vector2 combinedHalfSize = (this.size + other.size) * 0.5f;

        float overlapX = combinedHalfSize.x - Mathf.Abs(delta.x);
        float overlapY = combinedHalfSize.y - Mathf.Abs(delta.y);

        if (overlapX > 0 && overlapY > 0)
        {
            Vector2 normal;
            float penetration;

            if (overlapX < overlapY)
            {
                penetration = overlapX;
                normal = new Vector2(Mathf.Sign(delta.x), 0);
            }
            else
            {
                penetration = overlapY;
                normal = new Vector2(0, Mathf.Sign(delta.y));
            }

            Vector2 correction = normal * (penetration * 0.5f);
            position -= correction;
            other.position += correction;

            Vector2 relativeVelocity = other.velocity - this.velocity;
            float velAlongNormal = Vector2.Dot(relativeVelocity, normal);

            if (velAlongNormal > 0) 
                return;

            float e = Mathf.Min(restitution, other.restitution);
            float j = -(1 + e) * velAlongNormal;
            j /= (1 / this.mass) + (1 / other.mass);

            Vector2 impulse = j * normal;
            this.velocity -= impulse * (1 / this.mass);
            other.velocity += impulse * (1 / other.mass);

            if (normal.y > 0.5f)
                other.isGrounded = true;

            if (normal.y < -0.5f)
                this.isGrounded = true;
        }
    }

    public void OnDrawGizmos()
    {
        float rad = cannonRotation * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        Vector2 cannonCenter = position + direction * (cannonSize.x * 0.5f);

        Gizmos.color = Color.green;

        Matrix4x4 oldMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(
            new Vector3(cannonCenter.x, cannonCenter.y, 0),
            Quaternion.Euler(0, 0, cannonRotation),
            Vector3.one
        );

        Gizmos.DrawWireCube(Vector3.zero, new Vector3(cannonSize.x, cannonSize.y, 0.1f));

        Gizmos.matrix = oldMatrix;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(position, position + direction * cannonSize.x);
    }
}