using UnityEngine;

[System.Serializable]
public class SpiralFirePattern : IFirePattern
{
    public float circleRadius = 2f;
    public int circleCount = 8;
    public int spiralWaves = 3;

    private int currentIndex = 0;

    public void Fire(Transform origin, ProjectileConfig baseConfig)
    {
        float angleStep = 360f / circleCount;
        float angle = angleStep * currentIndex;

        float waveRadius = circleRadius * (1 + Mathf.Sin(currentIndex * Mathf.PI * 2 / circleCount * spiralWaves) * 0.5f);
        Vector3 offset = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.right * waveRadius;

        var config = baseConfig.Clone();
        config.spawnPosition = origin.position + offset;
        config.direction = origin.forward;

        GlobalProjectileFactory.Instance.GetProjectile(config);

        currentIndex = (currentIndex + 1) % circleCount;
    }

    public void DrawGizmos(Transform origin, Color color)
    {
        Gizmos.color = color;
        float angleStep = 360f / circleCount;

        for (int i = 0; i < circleCount; i++)
        {
            float angle = angleStep * i;
            for (int wave = 0; wave < spiralWaves; wave++)
            {
                float radius = circleRadius * (1 + Mathf.Sin(i * Mathf.PI * 2 / circleCount * (wave + 1)) * 0.5f);
                Vector3 offset = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.right * radius;
                Gizmos.DrawWireSphere(origin.position + offset, 0.05f);
            }
        }
    }
}

