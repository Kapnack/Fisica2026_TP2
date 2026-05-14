using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Physics
{
    public class Math
    {
        static public float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;

        static public float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        static public Vector2 Project(Vector2 vector, Vector2 ontoNormal)
        {
            float dot = Dot(vector, ontoNormal);
            return ontoNormal * dot;
        }

        static public Vector2 Reflect(Vector2 vector, Vector2 normal, float bounce)
        {
            Vector2 vNormal = Project(vector, normal);
            return (-vNormal * bounce);
        }
    }
}
