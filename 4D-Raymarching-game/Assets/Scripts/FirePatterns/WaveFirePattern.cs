using UnityEngine;

[System.Serializable]
public class WaveFirePattern : IFirePattern
{
    [Range(0f, 360f)]
    public float fanAngle = 30f;
    public int fanCount = 5;
    public float waveFrequency = 2f;
    public float waveAmplitude = 0.3f;

    public void Fire(Transform origin, ProjectileConfig baseConfig)
    {
        float startAngle = -fanAngle / 2f;
        float angleStep = fanCount > 1 ? fanAngle / (fanCount - 1) : 0;

        for (int i = 0; i < fanCount; i++)
        {
            float angle = startAngle + angleStep * i;
            float speedMultiplier = 1 + Mathf.Sin(Time.time * waveFrequency + i * 0.5f) * waveAmplitude;

            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * origin.forward;

            var config = baseConfig.Clone();
            config.spawnPosition = origin.position;
            config.direction = direction;
            config.speed *= speedMultiplier;

            GlobalProjectileFactory.Instance.GetProjectile(config);
        }
    }

    public void DrawGizmos(Transform origin, Color color)
    {
        Gizmos.color = color;
        float startAngle = -fanAngle / 2f;
        float angleStep = fanCount > 1 ? fanAngle / (fanCount - 1) : 0;

        for (int i = 0; i < fanCount; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * origin.forward;
            Gizmos.DrawRay(origin.position, direction * 2f);
        }
    }
}

