using UnityEngine;

[System.Serializable]
public class Random360FirePattern : IFirePattern
{
    public int projectileCount = 8;

    public void Fire(Transform origin, ProjectileConfig baseConfig)
    {
        for (int i = 0; i < projectileCount; i++)
        {
            Vector3 randomDirection = Random.onUnitSphere;
            randomDirection.y = Mathf.Abs(randomDirection.y); // Не стреляем вниз

            var config = baseConfig.Clone();
            config.spawnPosition = origin.position;
            config.direction = randomDirection;

            GlobalProjectileFactory.Instance.GetProjectile(config);
        }
    }

    public void DrawGizmos(Transform origin, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(origin.position, 1f);
    }
}

