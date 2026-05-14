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
        if (Physics.Math.DoSegmentCollide(previousPosition, Position, wall.pointA, wall.pointB, out Vector2 intersectPoint))
        {
            Vector2 wallDir = (wall.pointB - wall.pointA).normalized;
            Vector2 normal = new Vector2(-wallDir.y, wallDir.x);

            if (Vector2.Dot(normal, previousPosition - intersectPoint) < 0)
                normal = -normal;

            float combinedRadius = wall.thickness + radius;

            Position = intersectPoint + (normal * combinedRadius);

            velocity = Reflect(velocity, normal, restitution);

            return;
        }

        Vector2 closestPointToWall = GetClosestPointOnWall(wall);
        Vector2 delta = Position - closestPointToWall;
        float dist = delta.magnitude;
        float minDist = wall.thickness + radius;

        if (dist < minDist && dist > Mathf.Epsilon)
        {
            Vector2 normal = delta / dist;
            ResolveWallOverlap(closestPointToWall, normal, minDist);
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