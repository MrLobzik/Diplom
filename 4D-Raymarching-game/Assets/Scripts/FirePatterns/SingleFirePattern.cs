using UnityEngine;

[System.Serializable]
public class SingleFirePattern : IFirePattern
{
    public void Fire(Transform origin, ProjectileConfig baseConfig)
    {
        var config = baseConfig.Clone();
        config.spawnPosition = origin.position;
        config.direction = GetDirectionWithSpread(origin.forward);

        GlobalProjectileFactory.Instance.GetProjectile(config);
    }

    private Vector3 GetDirectionWithSpread(Vector3 baseDirection)
    {
        return baseDirection;
    }

    public void DrawGizmos(Transform origin, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawRay(origin.position, origin.forward * 3f);
    }
}

