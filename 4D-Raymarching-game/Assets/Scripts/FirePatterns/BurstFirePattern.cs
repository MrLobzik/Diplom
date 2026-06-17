using UnityEngine;
using System.Collections;

[System.Serializable]
public class BurstFirePattern : IFirePattern
{
    public int projectilesPerBurst = 5;
    public float burstDelay = 0.1f;
    [Range(0f, 180f)]
    public float spreadAngle = 10f;

    private MonoBehaviour coroutineRunner;

    public BurstFirePattern(MonoBehaviour runner)
    {
        coroutineRunner = runner;
    }

    public void Fire(Transform origin, ProjectileConfig baseConfig)
    {
        coroutineRunner.StartCoroutine(FireBurstRoutine(origin, baseConfig));
    }

    private IEnumerator FireBurstRoutine(Transform origin, ProjectileConfig baseConfig)
    {
        for (int i = 0; i < projectilesPerBurst; i++)
        {
            var config = baseConfig.Clone();
            config.spawnPosition = origin.position;
            config.direction = GetDirectionWithSpread(origin.forward);

            GlobalProjectileFactory.Instance.GetProjectile(config);

            yield return new WaitForSeconds(burstDelay);
        }
    }

    private Vector3 GetDirectionWithSpread(Vector3 baseDirection)
    {
        if (spreadAngle <= 0f) return baseDirection;

        float randomAngle = Random.Range(-spreadAngle / 2f, spreadAngle / 2f);
        Vector3 randomAxis = Random.onUnitSphere;
        return Quaternion.AngleAxis(randomAngle, randomAxis) * baseDirection;
    }

    public void DrawGizmos(Transform origin, Color color)
    {
        Gizmos.color = color;
        Vector3 direction = origin.forward;
        Gizmos.DrawRay(origin.position, direction * 3f);

        // Показываем область разброса
        if (spreadAngle > 0)
        {
            DrawSpreadCone(origin, direction, spreadAngle);
        }
    }

    private void DrawSpreadCone(Transform origin, Vector3 direction, float angle)
    {
        int segments = 20;
        float radius = 3f * Mathf.Tan(angle * Mathf.Deg2Rad / 2f);
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
            Gizmos.DrawLine(origin.position, point1);
        }
    }
}

