using System;
using UnityEngine;

namespace Utility
{
    public class UtilsTrigonometry
    {
        public static float HypotenuseLength(float angle, float adjacentCathetusLength)
        {
            return adjacentCathetusLength / Mathf.Cos(Mathf.Deg2Rad * angle);
        }

    }
}