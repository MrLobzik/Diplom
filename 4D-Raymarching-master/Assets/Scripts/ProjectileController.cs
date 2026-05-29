using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class ProjectileController : MonoBehaviour
{
    [Header("Lifecycle Settings")]
    public float lifetime = 10f;
    public float maxDistance = 100f;
    public bool returnOnPlayerHit = true;
    public float returnDelay = 0.5f;

    [Header("Collision Settings")]
    [Tooltip("Должен ли снаряд уничтожаться при столкновении с Shape4D")]
    public bool destroyOnShapeCollision = true;

    [Tooltip("Слои, с которыми снаряд сталкивается (если не указано - все Shape4D)")]
    public LayerMask collisionLayers = -1;

    [Tooltip("Радиус для проверки столкновений")]
    public float collisionRadius = 0.5f;

    [Header("Player Hit Settings")]
    [Tooltip("Отправлять игрока на чекпоинт при попадании")]
    public bool respawnOnPlayerHit = true;

    [Header("Effects")]
    [Tooltip("Эффект при столкновении")]
    public GameObject hitEffect;

    [Tooltip("Звук при столкновении")]
    public AudioClip hitSound;

    private float _spawnTime;
    private Vector3 _spawnPosition;
    private bool _isReturned;
    private Shape4DTrigger _trigger;
    private Shape4D _ownShape4D;
    private DistanceFunctions _distanceFunctions;

    private void OnEnable()
    {
        _spawnTime = Time.time;
        _spawnPosition = transform.position;
        _isReturned = false;
        _trigger = GetComponent<Shape4DTrigger>();
        _ownShape4D = GetComponent<Shape4D>();
        _distanceFunctions = GetComponent<DistanceFunctions>();

        if (_distanceFunctions == null)
        {
            _distanceFunctions = gameObject.AddComponent<DistanceFunctions>();
        }

        if (_trigger != null && returnOnPlayerHit)
        {
            _trigger.onTriggerEnter.AddListener(OnPlayerHit);
        }
    }

    private void Update()
    {
        if (_isReturned) return;

        // Проверка времени жизни и дистанции
        if (Time.time - _spawnTime > lifetime ||
            Vector3.Distance(transform.position, _spawnPosition) > maxDistance)
        {
            ReturnToPool();
            return;
        }

        // Проверка столкновений с Shape4D
        if (destroyOnShapeCollision)
        {
            CheckShapeCollision();
        }

        // Проверка столкновения с игроком через дистанцию
        CheckPlayerCollision();
    }

    private void CheckPlayerCollision()
    {
        // Находим игрока
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // Проверяем дистанцию до игрока
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        // Если игрок близко - считаем что попали
        if (distanceToPlayer < collisionRadius * 2f) // Увеличиваем радиус для надежности
        {
            // Проверяем также 4D расстояние если есть компоненты
            bool hitIn4D = true;

            if (_ownShape4D != null)
            {
                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc != null)
                {
                    // Получаем W позицию игрока
                    var wPositionField = typeof(PlayerController).GetField("wPosition",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (wPositionField != null)
                    {
                        float playerW = (float)wPositionField.GetValue(pc);
                        float projectileW = _ownShape4D.positionW;

                        // Проверяем что снаряд и игрок на одной W-координате
                        if (Mathf.Abs(playerW - projectileW) > 1f)
                        {
                            hitIn4D = false;
                        }
                    }
                }
            }

            if (hitIn4D)
            {
                OnPlayerHit(player.transform);
            }
        }
    }

    private void CheckShapeCollision()
    {
        var camScript = Camera.main?.GetComponent<RaymarchCam>();
        if (camScript == null || camScript.orderedShapes == null || _ownShape4D == null) return;

        float4 p4D = float4(
            transform.position.x,
            transform.position.y,
            transform.position.z,
            _ownShape4D.positionW
        );

        Vector3 wRot = camScript._wRotation * Mathf.Deg2Rad;
        if (wRot.magnitude != 0)
        {
            p4D.xw = math.mul(p4D.xw, float2x2(cos(wRot.x), -sin(wRot.x), sin(wRot.x), cos(wRot.x)));
            p4D.yw = math.mul(p4D.yw, float2x2(cos(wRot.y), -sin(wRot.y), sin(wRot.y), cos(wRot.y)));
            p4D.zw = math.mul(p4D.zw, float2x2(cos(wRot.z), -sin(wRot.z), sin(wRot.z), cos(wRot.z)));
        }

        for (int i = 0; i < camScript.orderedShapes.Count; i++)
        {
            Shape4D shape = camScript.orderedShapes[i];

            if (shape == _ownShape4D)
            {
                i += shape.numChildren;
                continue;
            }

            // Пропускаем Shape4D игрока
            if (shape.gameObject.CompareTag("Player"))
            {
                i += shape.numChildren;
                continue;
            }

            if (collisionLayers != -1 && ((1 << shape.gameObject.layer) & collisionLayers) == 0)
            {
                i += shape.numChildren;
                continue;
            }

            float distance = GetShapeDistance(shape, p4D);

            if (distance < collisionRadius)
            {
                OnShapeCollision(shape);
                return;
            }

            i += shape.numChildren;
        }
    }

    private float GetShapeDistance(Shape4D shape, float4 p4D)
    {
        float4 localP = p4D - (float4)shape.Position();

        Vector3 shapeRotation = shape.Rotation();
        localP.xz = math.mul(localP.xz, float2x2(cos(shapeRotation.y), sin(shapeRotation.y), -sin(shapeRotation.y), cos(shapeRotation.y)));
        localP.yz = math.mul(localP.yz, float2x2(cos(shapeRotation.x), -sin(shapeRotation.x), sin(shapeRotation.x), cos(shapeRotation.x)));
        localP.xy = math.mul(localP.xy, float2x2(cos(shapeRotation.z), -sin(shapeRotation.z), sin(shapeRotation.z), cos(shapeRotation.z)));

        Vector3 shapeRotationW = shape.RotationW();
        localP.xw = math.mul(localP.xw, float2x2(cos(shapeRotationW.x), sin(shapeRotationW.x), -sin(shapeRotationW.x), cos(shapeRotationW.x)));
        localP.zw = math.mul(localP.zw, float2x2(cos(shapeRotationW.z), -sin(shapeRotationW.z), sin(shapeRotationW.z), cos(shapeRotationW.z)));
        localP.yw = math.mul(localP.yw, float2x2(cos(shapeRotationW.y), -sin(shapeRotationW.y), sin(shapeRotationW.y), cos(shapeRotationW.y)));

        switch (shape.shapeType)
        {
            case Shape4D.ShapeType.HyperCube:
                return _distanceFunctions.sdHypercube(localP, shape.Scale());
            case Shape4D.ShapeType.HyperSphere:
                return _distanceFunctions.sdHypersphere(localP, shape.Scale().x);
            case Shape4D.ShapeType.DuoCylinder:
                return _distanceFunctions.sdDuoCylinder(localP, ((float4)shape.Scale()).xy);
            case Shape4D.ShapeType.plane:
                return _distanceFunctions.sdPlane(localP, shape.Scale());
            case Shape4D.ShapeType.Cone:
                return _distanceFunctions.sdCone(localP, shape.Scale());
            case Shape4D.ShapeType.FiveCell:
                return _distanceFunctions.sd5Cell(localP, shape.Scale());
            case Shape4D.ShapeType.SixteenCell:
                return _distanceFunctions.sd16Cell(localP, shape.Scale().x);
        }

        return Camera.main.farClipPlane;
    }

    private void OnShapeCollision(Shape4D shape)
    {
        Debug.Log($"Projectile hit Shape4D: {shape.gameObject.name}");

        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }

        ReturnToPool();
    }

    private void OnPlayerHit(Transform player)
    {
        if (_isReturned) return;

        Debug.Log($"Projectile hit player: {player.name}");

        // Эффекты попадания
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }


        // Отправляем игрока на чекпоинт
        if (respawnOnPlayerHit)
        {
            // Используем чекпоинт вместо ResetPlayer()
            Checkpoint4D.RespawnAtLastCheckpoint(player);
        }
        else
        {
            // Старое поведение - сброс на стартовую позицию
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc == null) pc = player.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                pc.ResetPlayer();
            }
        }

        // Возвращаем снаряд в пул с задержкой
        if (returnOnPlayerHit)
        {
            if (returnDelay > 0)
                Invoke(nameof(ReturnToPool), returnDelay);
            else
                ReturnToPool();
        }
    }

    public void ReturnToPool()
    {
        if (_isReturned) return;
        _isReturned = true;

        GlobalProjectileFactory.Instance?.ReturnProjectile(gameObject);
    }

    private void OnDisable()
    {
        if (_trigger != null)
            _trigger.onTriggerEnter.RemoveListener(OnPlayerHit);

        CancelInvoke();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);

        // Зона поражения игрока
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, collisionRadius * 2f);
    }
}

