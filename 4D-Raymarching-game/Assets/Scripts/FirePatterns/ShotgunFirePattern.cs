using UnityEngine;

[System.Serializable]
public class ShotgunFirePattern : IFirePattern
{
    public int pelletCount = 8;
    [Range(0f, 180f)]
    public float spreadAngle = 30f;

    public void Fire(Transform origin, ProjectileConfig baseConfig)
    {
        for (int i = 0; i < pelletCount; i++)
        {
            var config = baseConfig.Clone();
            config.spawnPosition = origin.position;
            config.direction = GetDirectionWithSpread(origin.forward);

            GlobalProjectileFactory.Instance.GetProjectile(config);
        }
    }

    private Vector3 GetDirectionWithSpread(Vector3 baseDirection)
    {
        float randomAngle = Random.Range(0f, spreadAngle);
        Vector3 randomAxis = Random.onUnitSphere;
        return Quaternion.AngleAxis(randomAngle, randomAxis) * baseDirection;
    }

    public void DrawGizmos(Transform origin, Color color)
    {
        Gizmos.color = color;
        Vector3 direction = origin.forward;

        // Рисуем конус разброса
        int segments = 20;
        float radius = 3f * Mathf.Tan(spreadAngle * Mathf.Deg2Rad / 2f);
        Vector3 endPoint = origin.position + direction * 3f;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
        if (perpendicular.magnitude < 0.1f)
            perpendicular = Vector3.Cross(direction, Vector3.right).normalized;
        Vector3 up = Vector3.Cross(direction, perpendicular).normalized;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)i / segments * Mathf.PI * 2f;
            float angle2 = (float)(i + 1) / segments * Mathf.PI * 2f;
            Vector3 point1 = endPoint + (perpendicular * Mathf.Cos(angle1) + up * Mathf.Sin(angle1)) * radius;
            Vector3 point2 = endPoint + (perpendicular * Mathf.Cos(angle2) + up * Mathf.Sin(angle2)) * radius;
            Gizmos.DrawLine(point1, point2);
        }
    }
}

