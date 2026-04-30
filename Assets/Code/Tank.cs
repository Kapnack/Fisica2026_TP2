using System;
using UnityEngine;

[Serializable]
public class Tank
{
    [SerializeField] private float aceleration = 30.0f;
    [SerializeField] private Vector2 position;
    public Vector2 Position => position;
    public Vector2 Size => size;

    [SerializeField] private Vector2 velocity;
    [SerializeField] private Vector2 size;

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
}
