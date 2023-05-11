using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MathUtils
{
    // Calculate target distance based on current distance
    private static float GetSmoothTransformTargetDist(
        float current,
        float minFollow, float maxFollow, 
        ref bool recentering,
        ref float followTime, float epsilon, ref float deltaTime)
    {
        if (deltaTime < 0) deltaTime = Time.deltaTime;

        float sign = Mathf.Sign(current);
        float abs = Mathf.Abs(current);

        if (abs > maxFollow) recentering = true;
        if (abs < epsilon) recentering = false;

        if (abs > maxFollow)
        {
            followTime = 0;
            return (maxFollow - epsilon) * sign;
        }
        if (recentering) return 0;
        if (abs > minFollow) return (minFollow - epsilon) * sign;
        return current;
    }


    public static float SmoothTransform(
        this float current, float target, ref float velocity,
        float minFollow, float maxFollow, ref bool recentering, 
        float epsilon = 0.01f, float followTime = 0.1f, float deltaTime = -1)
    {
        target += GetSmoothTransformTargetDist(target - current, 
            minFollow, maxFollow, ref recentering, 
            ref followTime, epsilon, ref deltaTime);
        return Mathf.SmoothDamp(current, target, ref velocity, followTime, Mathf.Infinity, deltaTime);
    }
    public static float SmoothTransform(
        this float current, float target, ref float velocity,
        float minFollow, float maxFollow,
        float followTime = 0.1f, float epsilon = 0.01f, float deltaTime = -1)
    {
        bool recentering = false;
        return current.SmoothTransform(target, ref velocity, 
            minFollow, maxFollow, ref recentering, followTime, epsilon, deltaTime);
    }

    public static float SmoothTransformAngle(
        this float current, float target, ref float velocity,
        float minFollow, float maxFollow, ref bool recentering,
        float followTime = 0.1f, float epsilon = 0.5f, float deltaTime = -1)
    {
        float dist = Mathf.DeltaAngle(target, current);
        target += GetSmoothTransformTargetDist(dist, 
            minFollow, maxFollow, ref recentering,
            ref followTime, epsilon, ref deltaTime);
        return Mathf.SmoothDampAngle(current, target, ref velocity, followTime, Mathf.Infinity, deltaTime);
    }


    /// <summary>
    /// minFollow:
    /// Distance from target in which current begins moving
    /// 
    /// maxFollow:
    /// Maximum distance from target. If maxSpeed > 0, 
    /// then this is the distance in which maxSpeed is applied
    /// 
    /// followTime:
    /// Time it takes to get from maxFollow to minFollow
    /// 
    /// deltaTime:
    /// Time since last call. Defaults to Time.deltaTime
    /// 
    /// recentering:
    /// Whether transitioning to exactly current, ignoring minFollow.
    /// Set to true if maxFollow is reached and false when recentered.
    /// 
    /// epsilon:
    /// Error offset for distances. If too small, it might be stuck in one state
    /// </summary>
    /// 
    /// <returns>The new position</returns>
    public static Vector3 SmoothTransform(
        this Vector3 current, Vector3 target, ref Vector3 velocity,
        float minFollow, float maxFollow, ref bool recentering,
        float followTime = 0.1f, float epsilon = 0.01f, float deltaTime = -1)
    {
        float dist = (target - current).magnitude;
        dist = GetSmoothTransformTargetDist(dist, 
            minFollow, maxFollow, ref recentering, 
            ref followTime, epsilon, ref deltaTime);
        target += (current-target).normalized * dist;
        return Vector3.SmoothDamp(current, target, ref velocity, followTime, Mathf.Infinity, deltaTime);
    }
    public static Vector3 SmoothTransform(
        this Vector3 current, Vector3 target, ref Vector3 velocity,
        float minFollow, float maxFollow,
        float followTime = 0.1f, float epsilon = 0.01f, float deltaTime = -1)
    {
        bool recentering = false;
        return current.SmoothTransform(target, ref velocity, 
            minFollow, maxFollow, ref recentering, followTime, epsilon, deltaTime);
    }
    
}
