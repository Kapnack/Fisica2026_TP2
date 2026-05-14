using System.Collections.Generic;
using UnityEngine;

namespace Assets.Code
{
    public class Simulation : MonoBehaviour
    {
        [SerializeField] private float predictionMaxTime = 2.0f;

        [Header("Bullets Properties")]
        [SerializeField] private float radius;

        [Header("Scene Objects")]
        [SerializeField] private Tank[] tanks;
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
                    tank.CheckWallCollision(wall);
                }

                predictedPoints.Add(PredictHitPoint(tank.CannonTipPosition + tank.CanionDir * radius, tank.canionForce, tank.CanionDir, walls, bullets, tanks, Gravity, predictionMaxTime, deltaTime));

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
                    if (bullet.CheckTankCollision(tank, out Vector2 _))
                        bulletsToRemove.Add(bullet);

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

        public Vector2 PredictHitPoint(Vector2 sartingPos, float aceleration, Vector2 dir, Wall[] walls, List<Bullet> bullets, Tank[] tanks, float gravity, float predictionMaxTime, float deltaTime)
        {
            Bullet sim = new Bullet();
            float time = 0f;

            sim.SetStartingPos(sartingPos);
            sim.Impulse(aceleration, dir);

            while (time < predictionMaxTime)
            {
                sim.Integrate(deltaTime, gravity);

                foreach (Tank tank in tanks)
                {
                    if (sim.CheckTankCollision(tank, out Vector2 hitPoint))
                        return hitPoint;
                }

                foreach (Wall wall in walls)
                {
                    Vector2 closestPoint = sim.GetClosestPointOnWall(wall);

                    Vector2 midPoint = (sim.PreviousPosition + sim.Position) * 0.5f;

                    Vector2 delta = midPoint - closestPoint;
                    float dist = delta.magnitude;
                    float minDist = wall.thickness + sim.Radius;

                    if (dist <= minDist && dist > Mathf.Epsilon)
                    {
                        Vector2 normal = delta / dist;

                        return closestPoint + normal * minDist;
                    }
                }

                foreach (Bullet other in bullets)
                {
                    Vector2 midPoint = (sim.PreviousPosition + sim.Position) * 0.5f;

                    float dist = Vector2.Distance(midPoint, other.Position);
                    float minDist = sim.Radius + other.Radius;

                    if (dist <= minDist)
                    {
                        Vector2 normal = (midPoint - other.Position).normalized;
                        return other.Position + normal * other.Radius;
                    }
                }

                time += deltaTime;
            }

            return sim.Position;
        }

        private void OnDrawGizmos()
        {
            foreach (Tank tank in tanks)
            {
                Gizmos.DrawCube(tank.Position, tank.TankSize);
                tank.OnDrawGizmos();
                Gizmos.DrawSphere(tank.CannonTipPosition, 0.3f);
            }

            foreach (Wall wall in walls)
                Gizmos.DrawLine(wall.pointA, wall.pointB);

            if (predictedPoints != null)
            {
                Gizmos.color = Color.purple;
                foreach (Vector2 vector2 in predictedPoints)
                    Gizmos.DrawSphere(vector2, 0.2f);
            }

            foreach (Bullet bullet in bullets)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(bullet.Position, bullet.Radius);
            }
        }
    }
}
