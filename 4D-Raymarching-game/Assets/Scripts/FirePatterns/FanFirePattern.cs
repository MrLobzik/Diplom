using UnityEngine;

[System.Serializable]
public class FanFirePattern : IFirePattern
{
    [Range(0f, 360f)]
    public float fanAngle = 30f;
    public int fanCount = 5;
    [Range(0f, 180f)]
    public float spreadAngle = 0f;

    public void Fire(Transform origin, ProjectileConfig baseConfig)
    {
        float startAngle = -fanAngle / 2f;
        float angleStep = fanCount > 1 ? fanAngle / (fanCount - 1) : 0;

        for (int i = 0; i < fanCount; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * origin.forward;
            direction = ApplySpread(direction);

            var config = baseConfig.Clone();
            config.spawnPosition = origin.position;
            config.direction = direction;

            GlobalProjectileFactory.Instance.GetProjectile(config);
        }
    }

    private Vector3 ApplySpread(Vector3 direction)
    {
        if (spreadAngle <= 0f) return direction;

        float randomAngle = Random.Range(-spreadAngle / 2f, spreadAngle / 2f);
        Vector3 randomAxis = Random.onUnitSphere;
        return Quaternion.AngleAxis(randomAngle, randomAxis) * direction;
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

