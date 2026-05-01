using System;
using UnityEngine;

[Serializable]
public class Tank
{
    [SerializeField] private float aceleration = 30.0f;
    [SerializeField] private Vector2 position;
    public Vector2 Position => position;
    public Vector2 Size => size;
    public float fallSpeed;

    [SerializeField] private Vector2 velocity;
    [SerializeField] private Vector2 size;
    [SerializeField] private float restitution = 0.8f;
    [SerializeField] private float mass = 1f;
    [SerializeField] private float friction = 0.3f;

    private float canionRotation = 0.0f;

    public void CheckInput(float deltaTime)
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            velocity += Vector2.left * deltaTime;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            velocity += Vector2.right * deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            canionRotation -= aceleration * deltaTime;
        }
        else if (Input.GetKeyDown(KeyCode.O))
        {
            canionRotation += aceleration * deltaTime;
        }
    }

    public void Integrate(float deltaTime, float gravity)
    {
        fallSpeed += gravity * Time.deltaTime;

        Vector2 verticalVelocity = Vector2.down * fallSpeed;

        position += (velocity + verticalVelocity) * Time.deltaTime;
    }

    public void CheckWallCollision(Wall wall)
    {
        Vector2 wallVector = wall.pointB - wall.pointA;
        float wallVectorSqrMag = Vector2.SqrMagnitude(wallVector);

        if (wallVectorSqrMag <= Mathf.Epsilon)
            return;

        Vector2 ballPointAVector = position - wall.pointA;

        float ballWallInterpolation = Vector2.Dot(ballPointAVector, wallVector) / wallVectorSqrMag;
        ballWallInterpolation = Mathf.Clamp01(ballWallInterpolation);

        Vector2 closestPointToWall = wall.pointA + wallVector * ballWallInterpolation;

        Vector2 delta = position - closestPointToWall;
        float dist = delta.magnitude;

        float minDist = wall.thickness + size.y;

        if (dist > minDist || dist < Mathf.Epsilon)
            return;

        Vector2 normal = delta / dist;

        position = closestPointToWall + normal * minDist;

        Vector2 vNormal = Vector2.Dot(velocity, normal) * normal;
        Vector2 vTangent = velocity - vNormal;

        vNormal = -vNormal * restitution;
        vTangent *= (1.0f - friction);

        velocity = vNormal + vTangent;
    }
}
