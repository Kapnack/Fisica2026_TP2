using UnityEngine;

namespace Assets.Code
{
    public class TankA : Tank
    {
        public override void CheckInput(float deltaTime)
        {
            if (Input.GetKey(KeyCode.UpArrow))
                canionForce += deltaTime;
            else if (Input.GetKey(KeyCode.DownArrow))
                canionForce -= deltaTime;

            canionForce = Mathf.Max(canionForce, 0.0f);

            if (Input.GetKey(KeyCode.LeftArrow))
                velocity += Vector2.left * (acceleration * deltaTime);
            else if (Input.GetKey(KeyCode.RightArrow))
                velocity += Vector2.right * (acceleration * deltaTime);

            if (Input.GetKey(KeyCode.I))
                cannonRotation += acceleration * 2f * deltaTime;
            else if (Input.GetKey(KeyCode.O))
                cannonRotation -= acceleration * 2f * deltaTime;

            if (Input.GetKeyDown(KeyCode.Space))
                shoot = true;

            cannonRotation = Mathf.Clamp(cannonRotation, 0f, 180f);
        }
    }

    public class TankB : Tank
    {
        public override void CheckInput(float deltaTime)
        {
            if (Input.GetKey(KeyCode.W))
                canionForce += deltaTime;
            else if (Input.GetKey(KeyCode.S))
                canionForce -= deltaTime;

            canionForce = Mathf.Max(canionForce, 0.0f);

            if (Input.GetKey(KeyCode.A))
                velocity += Vector2.left * (acceleration * deltaTime);
            else if (Input.GetKey(KeyCode.D))
                velocity += Vector2.right * (acceleration * deltaTime);

            if (Input.GetKey(KeyCode.Q))
                cannonRotation += acceleration * 2f * deltaTime;
            else if (Input.GetKey(KeyCode.E))
                cannonRotation -= acceleration * 2f * deltaTime;

            if (Input.GetKeyDown(KeyCode.F))
                shoot = true;

            cannonRotation = Mathf.Clamp(cannonRotation, 0f, 180f);
        }
    }
}
