using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Bullet
{
    [SerializeField] private Vector2 previousPosition;
    public Vector2 PreviousPosition => previousPosition;
    [SerializeField] private Vector2 position;
    [SerializeField] private float radius;
    [SerializeField] private Vector2 velocity;
    private const float restitution = 1.0f;
    [SerializeField] private float mass = 1f;
    [SerializeField] private float friction = 0.3f;

    public Vector2 Position
    {
        get => position;
        private set
        {
            previousPosition = Position;
            position = value;
        }
    }

    public float Radius => radius;
    private float InvMass => (mass <= 0f) ? 0f : 1f / mass;

    private Vector2 Project(Vector2 vector, Vector2 ontoNormal)
    {
        float dot = Physics.Math.Dot(vector, ontoNormal);
        return ontoNormal * dot;
    }

    private Vector2 Reflect(Vector2 vector, Vector2 normal, float bounce)
    {
        Vector2 vNormal = Project(vector, normal);
        Vector2 vTangent = vector - vNormal;

        return (-vNormal * bounce) + (vTangent * (1.0f - friction));
    }

    public void Impulse(float aceleration, Vector2 dir)
    {
        velocity += aceleration * dir.normalized;
    }

    public void Integrate(float deltaTime, float gravity)
    {
        Position += velocity * deltaTime;

        velocity += Vector2.down * (gravity * deltaTime);
    }

    public void SetStartingPos(Vector2 startingPos)
    {
        position = startingPos;
        previousPosition = position;
    }

    public void SetRadius(float radius)
    {
        this.radius = radius;
    }

    public void CheckWallCollision(Wall wall)
    {
        Vector2 wallVec = wall.pointB - wall.pointA;
        float wallLen = wallVec.magnitude;
        if (wallLen < Mathf.Epsilon) return;

        Vector2 wallDir = wallVec / wallLen;
        Vector2 wallNormal = new Vector2(-wallDir.y, wallDir.x);
        float minDist = wall.thickness + radius;

        float distPrev = Vector2.Dot(previousPosition - wall.pointA, wallNormal);
        float distCurr = Vector2.Dot(position - wall.pointA, wallNormal);

        if (Mathf.Sign(distPrev) != Mathf.Sign(distCurr) || Mathf.Abs(distCurr) < minDist)
        {
            float collisionTime = 0;
            if (Mathf.Abs(distPrev - distCurr) > Mathf.Epsilon)
            {
                collisionTime = (distPrev - (Mathf.Sign(distPrev) * minDist)) / (distPrev - distCurr);
            }

            collisionTime = Mathf.Clamp01(collisionTime);
            Vector2 intersectPoint = Vector2.Lerp(previousPosition, position, collisionTime);

            float projection = Vector2.Dot(intersectPoint - wall.pointA, wallDir);

            if (projection >= 0 && projection <= wallLen)
            {
                Vector2 normal = wallNormal * Mathf.Sign(distPrev);
                position = intersectPoint;
                velocity = Reflect(velocity, normal, restitution);
                return;
            }
        }

        Vector2 closestPointToWall = GetClosestPointOnWall(wall);
        Vector2 delta = position - closestPointToWall;
        float distSqr = delta.sqrMagnitude;

        if (distSqr < minDist * minDist && distSqr > Mathf.Epsilon)
        {
            float dist = Mathf.Sqrt(distSqr);
            Vector2 normal = delta / dist;
            position = closestPointToWall + normal * minDist;
            velocity = Reflect(velocity, normal, restitution);
        }
    }

    public Vector2 GetClosestPointOnWall(Wall wall)
    {
        Vector2 wallVector = wall.pointB - wall.pointA;
        float wallVectorSqrMag = wallVector.sqrMagnitude;

        if (wallVectorSqrMag < Mathf.Epsilon)
            return wall.pointA;

        float ballWallInterpolation = Physics.Math.Dot(Position - wall.pointA, wallVector) / wallVectorSqrMag;
        ballWallInterpolation = Mathf.Clamp01(ballWallInterpolation);

        return wall.pointA + wallVector * ballWallInterpolation;
    }

    private void ResolveWallOverlap(Vector2 closestPointToWall, Vector2 normal, float minDist)
    {
        Position = closestPointToWall + normal * minDist;
    }

    public void CheckBallCollision(Bullet other)
    {
        Vector2 otherToThisVector = Position - other.Position;
        float ballsDistance = otherToThisVector.magnitude;
        float minDist = radius + other.radius;

        if (ballsDistance <= Mathf.Epsilon || ballsDistance > minDist)
            return;

        Vector2 normal = otherToThisVector / ballsDistance;

        ResolveBallOverlap(other, normal, minDist - ballsDistance);
        ApplyBallPhysicsResponse(other, normal);
    }

    public bool CheckTankCollision(Tank tank, out Vector2 hitPoint)
    {
        hitPoint = Vector2.zero;

        Vector2 halfSize = tank.TankSize * 0.5f;
        Vector2 min = tank.Position - halfSize;
        Vector2 max = tank.Position + halfSize;

        Vector2 closest = new Vector2(
            Mathf.Clamp(Position.x, min.x, max.x),
            Mathf.Clamp(Position.y, min.y, max.y)
        );

        Vector2 delta = Position - closest;
        float dist = delta.magnitude;

        if (dist > radius || dist < Mathf.Epsilon)
            return false;

        Vector2 normal = delta / dist;

        Position = closest + normal * radius;

        velocity = Reflect(velocity, normal, restitution);

        hitPoint = closest;

        return true;
    }

    public bool CheckCannonCollision(Tank tank, out Vector2 hitPoint)
    {
        hitPoint = Vector2.zero;

        Vector2 cannonCenter = tank.CannonCenter;
        float angle = -tank.CannonRotation * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        Vector2 localPos = Position - cannonCenter;
        Vector2 rotatedPos = new Vector2(
            localPos.x * cos - localPos.y * sin,
            localPos.x * sin + localPos.y * cos
        );

        Vector2 halfSize = tank.CannonSize * 0.5f;
        Vector2 closestLocal = new Vector2(
            Mathf.Clamp(rotatedPos.x, -halfSize.x, halfSize.x),
            Mathf.Clamp(rotatedPos.y, -halfSize.y, halfSize.y)
        );

        Vector2 deltaLocal = rotatedPos - closestLocal;
        float distSqr = deltaLocal.sqrMagnitude;

        if (distSqr > radius * radius)
            return false;

        // Volvemos a rotar el punto más cercano al espacio del mundo para poder
        // sacar la normal real y reposicionar/reflejar la bala.
        float worldAngle = tank.CannonRotation * Mathf.Deg2Rad;
        float cosW = Mathf.Cos(worldAngle);
        float sinW = Mathf.Sin(worldAngle);

        Vector2 closestWorld = cannonCenter + new Vector2(
            closestLocal.x * cosW - closestLocal.y * sinW,
            closestLocal.x * sinW + closestLocal.y * cosW
        );

        Vector2 delta = Position - closestWorld;
        float dist = delta.magnitude;

        if (dist < Mathf.Epsilon)
            return false;

        Vector2 normal = delta / dist;

        Position = closestWorld + normal * radius;
        velocity = Reflect(velocity, normal, restitution);

        hitPoint = closestWorld;

        return true;
    }

    public bool TestCannonCollision(Tank tank, out Vector2 hitPoint)
    {
        hitPoint = Vector2.zero;

        Vector2 cannonCenter = tank.CannonCenter;
        float angle = -tank.CannonRotation * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        Vector2 localPos = position - cannonCenter;
        Vector2 rotatedPos = new Vector2(
            localPos.x * cos - localPos.y * sin,
            localPos.x * sin + localPos.y * cos
        );

        Vector2 halfSize = tank.CannonSize * 0.5f;
        Vector2 closestLocal = new Vector2(
            Mathf.Clamp(rotatedPos.x, -halfSize.x, halfSize.x),
            Mathf.Clamp(rotatedPos.y, -halfSize.y, halfSize.y)
        );

        Vector2 deltaLocal = rotatedPos - closestLocal;
        float distSqr = deltaLocal.sqrMagnitude;

        if (distSqr > radius * radius)
            return false;

        float worldAngle = tank.CannonRotation * Mathf.Deg2Rad;
        float cosW = Mathf.Cos(worldAngle);
        float sinW = Mathf.Sin(worldAngle);

        hitPoint = cannonCenter + new Vector2(
            closestLocal.x * cosW - closestLocal.y * sinW,
            closestLocal.x * sinW + closestLocal.y * cosW
        );

        return true;
    }

    private void ResolveBallOverlap(Bullet other, Vector2 normal, float penetration)
    {
        Vector2 correction = normal * (penetration * 0.5f);
        Position += correction;
        other.Position -= correction;
    }

    private void ApplyBallPhysicsResponse(Bullet other, Vector2 normal)
    {
        Vector2 relativeVelocity = velocity - other.velocity;
        float velAlongNormal = Physics.Math.Dot(relativeVelocity, normal);

        if (velAlongNormal > 0)
            return;

        float invMassA = InvMass;
        float invMassB = other.InvMass;
        float denom = invMassA + invMassB;

        if (denom <= 0 || Mathf.Approximately(denom, 0f))
            return;

        float impulseCorrecction = (-(1 + restitution) * velAlongNormal) / denom;

        Vector2 impulse = impulseCorrecction * normal;
        velocity += impulse * invMassA;
        other.velocity -= impulse * invMassB;

        ApplyBallFriction(other, relativeVelocity, normal, impulseCorrecction, denom);
    }
    public bool TestTankCollision(Tank tank, out Vector2 hitPoint)
    {
        hitPoint = Vector2.zero;
        Vector2 halfSize = tank.TankSize * 0.5f;
        Vector2 min = tank.Position - halfSize;
        Vector2 max = tank.Position + halfSize;

        Vector2 closest = new Vector2(
            Mathf.Clamp(position.x, min.x, max.x),
            Mathf.Clamp(position.y, min.y, max.y)
        );

        Vector2 delta = position - closest;
        float distSqr = delta.sqrMagnitude;

        if (distSqr <= radius * radius)
        {
            hitPoint = closest;
            return true;
        }
        return false;
    }

    private void ApplyBallFriction(Bullet other, Vector2 relativeVelocity, Vector2 normal, float impulseCorrecction, float denom)
    {
        Vector2 tangent = relativeVelocity - Project(relativeVelocity, normal);

        if (tangent.sqrMagnitude > Mathf.Epsilon)
            tangent.Normalize();

        float relativeVelTangent = Physics.Math.Dot(relativeVelocity, tangent);
        float tangencialImpulse = -relativeVelTangent / denom;

        float coeficientFriction = (friction + other.friction) * 0.5f;
        tangencialImpulse = Mathf.Clamp(tangencialImpulse, -impulseCorrecction * coeficientFriction, impulseCorrecction * coeficientFriction);

        Vector2 frictionImpulse = tangencialImpulse * tangent;
        velocity += frictionImpulse * InvMass;
        other.velocity -= frictionImpulse * other.InvMass;
    }
}