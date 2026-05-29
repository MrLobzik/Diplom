using UnityEngine;

public interface IFirePattern
{
    void Fire(Transform origin, ProjectileConfig baseConfig);
    void DrawGizmos(Transform origin, Color color);
}

