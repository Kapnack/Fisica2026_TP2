using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code
{
    public class Simulation : MonoBehaviour
    {
        [SerializeField] private List<Bullet> bullets;
        [SerializeField] private Tank[] tanks;
        [SerializeField] private Wall[] walls;

        private const float Gravity = 9.81f;

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            foreach (Tank tank in tanks)
            {
                tank.CheckInput(deltaTime);

                tank.Integrate(deltaTime, Gravity);
            }


            foreach (Bullet bullet in bullets)
            {
                bullet.Integrate(deltaTime, Gravity);

                foreach (Wall wall in walls)
                    bullet.CheckWallCollision(wall);

                foreach (Bullet other in bullets)
                {
                    if (bullet.Equals(other))
                        continue;

                    bullet.CheckBulletCollision(other);
                }
            }
        }

        private void OnDrawGizmos()
        {
            foreach (Tank tank in tanks)
                Gizmos.DrawCube(tank.Position, tank.Size);

            foreach (Wall wall in walls)
                Gizmos.DrawLine(wall.pointA, wall.pointB);

            foreach (Bullet bullet in bullets)
                Gizmos.DrawSphere(bullet.Position, bullet.Radius);
        }
    }
}
