using UnityEngine;

[System.Serializable]
public class CircleFirePattern : IFirePattern
{
    public float circleRadius = 2f;
    public int circleCount = 8;

    public void Fire(Transform origin, ProjectileConfig baseConfig)
    {
        float angleStep = 360f / circleCount;

        for (int i = 0; i < circleCount; i++)
        {
            float angle = angleStep * i;
            Vector3 offset = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.right * circleRadius;

            var config = baseConfig.Clone();
            config.spawnPosition = origin.position + offset;
            config.direction = origin.forward;

            GlobalProjectileFactory.Instance.GetProjectile(config);
        }
    }

    public void DrawGizmos(Transform origin, Color color)
    {
        Gizmos.color = color;
        float angleStep = 360f / circleCount;

        for (int i = 0; i < circleCount; i++)
        {
            float angle = angleStep * i;
            Vector3 offset = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.right * circleRadius;
            Vector3 spawnPos = origin.position + offset;

            Gizmos.DrawSphere(spawnPos, 0.1f);
            Gizmos.DrawRay(spawnPos, origin.forward * 1f);
        }
    }
}

