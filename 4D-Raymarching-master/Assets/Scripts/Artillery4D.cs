// Artillery4D.cs
using UnityEngine;
using System;

public class Artillery4D : MonoBehaviour
{
    [Header("Base Projectile Settings")]
    public ProjectileConfig baseProjectileConfig = new ProjectileConfig();

    [Header("Fire Control")]
    public float fireInterval = 2f;
    public bool autoStart = true;
    public float startDelay = 0f;

    [Header("Activation")]
    public Vector2 activeInterval = Vector2.zero; // x - active, y - inactive

    [Header("Pattern Selection")]
    [SerializeReference]
    [SerializeField] private IFirePattern firePattern;

    // Для сериализации в инспекторе
    [SerializeField] private PatternType patternType = PatternType.Single;
    [SerializeField] private SingleFirePattern singlePattern = new SingleFirePattern();
    [SerializeField] private FanFirePattern fanPattern = new FanFirePattern();
    [SerializeField] private CircleFirePattern circlePattern = new CircleFirePattern();
    [SerializeField] private SpiralFirePattern spiralPattern = new SpiralFirePattern();
    [SerializeField] private WaveFirePattern wavePattern = new WaveFirePattern();
    [SerializeField] private Random360FirePattern random360Pattern = new Random360FirePattern();
    [SerializeField] private ShotgunFirePattern shotgunPattern = new ShotgunFirePattern();

    // Burst требует MonoBehaviour для корутин
    private BurstFirePattern burstPattern;

    private bool isActive;
    private float fireTimer;
    private float activationTimer;
    private bool activationState = true;

    public enum PatternType
    {
        Single,
        Fan,
        Circle,
        Spiral,
        Wave,
        Burst,
        Random360,
        Shotgun
    }

    void Awake()
    {
        burstPattern = new BurstFirePattern(this);
        UpdatePattern();
    }

    void Start()
    {
        fireTimer = fireInterval + startDelay;
        isActive = autoStart;
    }

    void Update()
    {
        if (!isActive) return;

        // Управление активацией
        if (activeInterval != Vector2.zero)
        {
            HandleActivation();
        }

        if (!activationState) return;

        // Таймер стрельбы
        fireTimer += Time.deltaTime;

        if (fireTimer >= fireInterval)
        {
            fireTimer = 0f;
            Fire();
        }
    }

    void HandleActivation()
    {
        activationTimer += Time.deltaTime;

        if (activationState && activationTimer >= activeInterval.x)
        {
            activationState = false;
            activationTimer = 0f;
        }
        else if (!activationState && activationTimer >= activeInterval.y)
        {
            activationState = true;
            activationTimer = 0f;
        }
    }

    public void Fire()
    {
        firePattern?.Fire(transform, baseProjectileConfig);
    }

    void UpdatePattern()
    {
        switch (patternType)
        {
            case PatternType.Single:
                firePattern = singlePattern;
                break;
            case PatternType.Fan:
                firePattern = fanPattern;
                break;
            case PatternType.Circle:
                firePattern = circlePattern;
                break;
            case PatternType.Spiral:
                firePattern = spiralPattern;
                break;
            case PatternType.Wave:
                firePattern = wavePattern;
                break;
            case PatternType.Burst:
                firePattern = burstPattern;
                break;
            case PatternType.Random360:
                firePattern = random360Pattern;
                break;
            case PatternType.Shotgun:
                firePattern = shotgunPattern;
                break;
            default:
                firePattern = singlePattern;
                break;
        }
    }

    public void SetPattern(PatternType type)
    {
        patternType = type;
        UpdatePattern();
    }

    public void SetPattern(IFirePattern pattern)
    {
        firePattern = pattern;
    }

    public void Activate()
    {
        isActive = true;
        activationState = true;
        activationTimer = 0f;
        fireTimer = fireInterval;
    }

    public void Deactivate()
    {
        isActive = false;
        activationState = false;
    }

    void OnValidate()
    {
        UpdatePattern();
    }

    void OnDrawGizmos()
    {
        firePattern?.DrawGizmos(transform, Color.red);
    }
}

