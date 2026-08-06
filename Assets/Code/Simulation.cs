using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Code
{
    public class Simulation : MonoBehaviour
    {
        [SerializeField] private float predictionMaxTime = 2.0f;

        [Header("Bullets Properties")]
        [SerializeField] private float radius;

        [Header("Scene Objects")]
        public Tank[] tanks;
        [SerializeField] private Wall[] walls;
        [SerializeField] private List<Bullet> bullets;

        private List<Bullet> bulletsToRemove;

        private List<Vector2> predictedPoints;

        private const float Gravity = 9.81f;

        private void Awake()
        {
            Application.runInBackground = true;
            predictedPoints = new List<Vector2>();
            bulletsToRemove = new List<Bullet>();

            tanks = new Tank[2];

            tanks[0] = new TankA();
            tanks[1] = new TankB();
        }

        private void Update()
        {
            predictedPoints.Clear();

            float deltaTime = Time.deltaTime;

            foreach (Tank tank in tanks)
            {
                tank.CheckInput(deltaTime);

                tank.Integrate(deltaTime, Gravity);

                foreach (Wall wall in walls)
                {
                    tank.CheckWallCollision(wall, Gravity);
                }

                foreach (Tank other in tanks)
                {
                    if (!tank.Equals(other))
                        tank.CheckTankCollision(other);
                }


                predictedPoints.Add(PredictHitPoint(tank.CannonTipPosition + tank.CanionDir * radius, tank.canionForce, tank.CanionDir, Gravity, predictionMaxTime, deltaTime));

                if (tank.shoot)
                {
                    Bullet bullet = new Bullet();

                    bullet.SetRadius(radius);
                    bullet.SetStartingPos(tank.CannonTipPosition + tank.CanionDir.normalized * radius);
                    bullet.Impulse(tank.canionForce, tank.CanionDir);

                    bullets.Add(bullet);

                    tank.shoot = false;
                }
            }

            for (int i = 0; i < bullets.Count; ++i)
            {
                Bullet bullet = bullets[i];

                bullet.Integrate(deltaTime, Gravity);

                foreach (Tank tank in tanks)
                {
                    if (bullet.CheckTankCollision(tank, out Vector2 _))
                        bulletsToRemove.Add(bullet);

                    if (bullet.CheckCannonCollision(tank, out Vector2 _))
                        bulletsToRemove.Add(bullet);
                }

                foreach (Wall wall in walls)
                    bullet.CheckWallCollision(wall);

                foreach (Bullet other in bullets)
                {
                    if (bullet.Equals(other))
                        continue;

                    bullet.CheckBallCollision(other);
                }
            }

            bullets.RemoveAll(b => bulletsToRemove.Contains(b));
            bulletsToRemove.Clear();
        }

        public Vector2 PredictHitPoint(Vector2 sartingPos, float aceleration, Vector2 dir, float gravity, float predictionMaxTime, float deltaTime)
        {
            Bullet bulletSim = new Bullet();
            float time = 0f;

            bulletSim.SetStartingPos(sartingPos);
            bulletSim.Impulse(aceleration, dir);

            while (time < predictionMaxTime)
            {
                bulletSim.Integrate(deltaTime, gravity);

                foreach (Tank tank in tanks)
                {
                    if (bulletSim.TestTankCollision(tank, out Vector2 hitPoint))
                        return hitPoint;

                    if (bulletSim.TestCannonCollision(tank, out Vector2 cannonHitPoint))
                        return cannonHitPoint;
                }

                foreach (Wall wall in walls)
                {
                    Vector2 closestPoint = bulletSim.GetClosestPointOnWall(wall);

                    Vector2 midPoint = (bulletSim.PreviousPosition + bulletSim.Position) * 0.5f;

                    Vector2 delta = midPoint - closestPoint;
                    float dist = delta.magnitude;
                    float minDist = wall.thickness + bulletSim.Radius;

                    if (dist <= minDist && dist > Mathf.Epsilon)
                    {
                        Vector2 normal = delta / dist;

                        return closestPoint + normal * minDist;
                    }
                }

                foreach (Bullet other in bullets)
                {
                    Vector2 midPoint = (bulletSim.PreviousPosition + bulletSim.Position) * 0.5f;

                    float dist = Vector2.Distance(midPoint, other.Position);
                    float minDist = bulletSim.Radius + other.Radius;

                    if (dist <= minDist)
                    {
                        Vector2 normal = (midPoint - other.Position).normalized;
                        return other.Position + normal * other.Radius;
                    }
                }

                time += deltaTime;
            }

            return bulletSim.Position;
        }

        private void OnDrawGizmos()
        {
            if (tanks != null)
            {
                Gizmos.color = Color.white;
                foreach (Tank tank in tanks)
                {
                    Gizmos.DrawCube(tank.Position, tank.TankSize);
                    tank.OnDrawGizmos();
                    Gizmos.DrawSphere(tank.CannonTipPosition, 0.3f);
                }
            }

            Gizmos.color = Color.yellow;
            foreach (Wall wall in walls)
                Gizmos.DrawLine(wall.pointA, wall.pointB);

            if (predictedPoints != null)
            {
                Gizmos.color = Color.purple;
                foreach (Vector2 vector2 in predictedPoints)
                    Gizmos.DrawSphere(vector2, 0.2f);
            }

            Gizmos.color = Color.red;
            foreach (Bullet bullet in bullets)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(bullet.Position, bullet.Radius);
            }
        }
    }
}