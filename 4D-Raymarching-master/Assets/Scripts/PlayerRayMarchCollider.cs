using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using static Unity.Mathematics.math;

namespace Unity.Mathematics
{
    public class PlayerRayMarchCollider : MonoBehaviour
    {
        [Header("Collision Settings")]
        public float colliderOffset = 1.2f;
        public float maxDownMovement = 1f;

        [Header("Gravity Settings")]
        [Tooltip("Скорость притяжения к земле (чем больше, тем быстрее игрок прилипает к поверхности)")]
        public float groundSnapSpeed = 10f;

        [Tooltip("Максимальная скорость падения")]
        public float maxFallSpeed = 20f;

        [Tooltip("Расстояние, на котором начинается притяжение к земле")]
        public float groundSnapDistance = 3f;

        [Tooltip("Если true, использует плавное движение; если false - моментальное")]
        public bool useSmoothGroundSnap = true;

        [Tooltip("The transforms from which the raymarcher will test the distances and apply the collision")]
        public Transform[] rayMarchTransforms;

        private DistanceFunctions Df;
        private RaymarchCam camScript;

        // Для плавного движения
        private float currentFallVelocity;
        private bool isGrounded;

        // Словарь для отслеживания состояния триггеров
        private Dictionary<Shape4D, bool> triggerStates = new Dictionary<Shape4D, bool>();

        void Start()
        {
            camScript = Camera.main.GetComponent<RaymarchCam>();
            Df = GetComponent<DistanceFunctions>();
            currentFallVelocity = 0f;
        }

        void Update()
        {
            MoveToGround();
            RayMarch(rayMarchTransforms);
            CheckTriggers();
        }

        // Проверка триггеров
        void CheckTriggers()
        {
            if (camScript == null || camScript.orderedShapes == null) return;

            Vector3 playerPos = transform.position;
            float4 p4D = float4(playerPos, camScript._wPosition);
            Vector3 wRot = camScript._wRotation * Mathf.Deg2Rad;

            if (wRot.magnitude != 0)
            {
                p4D.xw = mul(p4D.xw, float2x2(cos(wRot.x), -sin(wRot.x), sin(wRot.x), cos(wRot.x)));
                p4D.yw = mul(p4D.yw, float2x2(cos(wRot.y), -sin(wRot.y), sin(wRot.y), cos(wRot.y)));
                p4D.zw = mul(p4D.zw, float2x2(cos(wRot.z), -sin(wRot.z), sin(wRot.z), cos(wRot.z)));
            }

            foreach (var shape in camScript.orderedShapes)
            {
                // Проверяем только триггерные объекты
                if (shape.TryGetComponent<Shape4DTrigger>(out var trigger))
                {
                    float distance = GetShapeDistance(shape, p4D);
                    bool isInside = distance < trigger.triggerRadius;

                    // Инициализируем состояние если нужно
                    if (!triggerStates.ContainsKey(shape))
                    {
                        triggerStates[shape] = false;
                    }

                    bool wasInside = triggerStates[shape];

                    // Вход в триггер
                    if (isInside && !wasInside)
                    {
                        trigger.OnTriggerEnter(transform);
                    }
                    // Выход из триггера
                    else if (!isInside && wasInside)
                    {
                        trigger.OnTriggerExit(transform);
                    }
                    // Нахождение в триггере
                    else if (isInside && wasInside)
                    {
                        trigger.OnTriggerStay(transform);
                    }

                    triggerStates[shape] = isInside;
                }
            }
        }

        public float GetShapeDistance(Shape4D shape, float4 p4D)
        {
            p4D -= (float4)shape.Position();

            Vector3 shapeRotation = shape.Rotation();
            p4D.xz = mul(p4D.xz, math.float2x2(cos(shapeRotation.y), sin(shapeRotation.y), -sin(shapeRotation.y), cos(shapeRotation.y)));
            p4D.yz = mul(p4D.yz, math.float2x2(cos(shapeRotation.x), -sin(shapeRotation.x), sin(shapeRotation.x), cos(shapeRotation.x)));
            p4D.xy = mul(p4D.xy, math.float2x2(cos(shapeRotation.z), -sin(shapeRotation.z), sin(shapeRotation.z), cos(shapeRotation.z)));

            Vector3 shapeRotationW = shape.RotationW();
            p4D.xw = mul(p4D.xw, math.float2x2(cos(shapeRotationW.x), sin(shapeRotationW.x), -sin(shapeRotationW.x), cos(shapeRotationW.x)));
            p4D.zw = mul(p4D.zw, math.float2x2(cos(shapeRotationW.z), -sin(shapeRotationW.z), sin(shapeRotationW.z), cos(shapeRotationW.z)));
            p4D.yw = mul(p4D.yw, math.float2x2(cos(shapeRotationW.y), -sin(shapeRotationW.y), sin(shapeRotationW.y), cos(shapeRotationW.y)));

            switch (shape.shapeType)
            {
                case Shape4D.ShapeType.HyperCube:
                    return Df.sdHypercube(p4D, shape.Scale());
                case Shape4D.ShapeType.HyperSphere:
                    return Df.sdHypersphere(p4D, shape.Scale().x);
                case Shape4D.ShapeType.DuoCylinder:
                    return Df.sdDuoCylinder(p4D, ((float4)shape.Scale()).xy);
                case Shape4D.ShapeType.plane:
                    return Df.sdPlane(p4D, shape.Scale());
                case Shape4D.ShapeType.Cone:
                    return Df.sdCone(p4D, shape.Scale());
                case Shape4D.ShapeType.FiveCell:
                    return Df.sd5Cell(p4D, shape.Scale());
                case Shape4D.ShapeType.SixteenCell:
                    return Df.sd16Cell(p4D, shape.Scale().x);
            }

            return Camera.main.farClipPlane;
        }

        public float DistanceField(float3 p)
        {
            float4 p4D = float4(p, camScript._wPosition);
            Vector3 wRot = camScript._wRotation * Mathf.Deg2Rad;

            if ((wRot).magnitude != 0)
            {
                p4D.xw = mul(p4D.xw, float2x2(cos(wRot.x), -sin(wRot.x), sin(wRot.x), cos(wRot.x)));
                p4D.yw = mul(p4D.yw, float2x2(cos(wRot.y), -sin(wRot.y), sin(wRot.y), cos(wRot.y)));
                p4D.zw = mul(p4D.zw, float2x2(cos(wRot.z), -sin(wRot.z), sin(wRot.z), cos(wRot.z)));
            }

            float globalDst = Camera.main.farClipPlane;

            for (int i = 0; i < camScript.orderedShapes.Count; i++)
            {
                Shape4D shape = camScript.orderedShapes[i];
                int numChildren = shape.numChildren;

                float localDst = GetShapeDistance(shape, p4D);

                for (int j = 0; j < numChildren; j++)
                {
                    Shape4D childShape = camScript.orderedShapes[i + j + 1];
                    float childDst = GetShapeDistance(childShape, p4D);
                    localDst = Df.Combine(localDst, childDst, childShape.operation, childShape.smoothRadius);
                }
                i += numChildren;

                globalDst = Df.Combine(globalDst, localDst, shape.operation, shape.smoothRadius);
            }

            return globalDst;
        }

        void RayMarch(Transform[] ro)
        {
            int nrHits = 0;

            for (int i = 0; i < ro.Length; i++)
            {
                Vector3 p = ro[i].position;
                float d = DistanceField(p);

                if (d < 0)
                {
                    //Debug.Log("hit" + i);
                    nrHits++;
                    transform.Translate(ro[i].forward * d * 1.5f, Space.World);
                }
            }
        }

        void MoveToGround()
        {
            Vector3 p = transform.position;
            float distanceToGround = DistanceField(p);

            if (useSmoothGroundSnap)
            {
                SmoothGroundSnap(distanceToGround);
            }
            else
            {
                InstantGroundSnap(distanceToGround);
            }
        }

        // Мгновенное перемещение к земле (старый метод)
        void InstantGroundSnap(float distanceToGround)
        {
            float d = Mathf.Min(distanceToGround, maxDownMovement);
            transform.Translate(Vector3.down * d, Space.World);
        }

        // Плавное перемещение к земле
        void SmoothGroundSnap(float distanceToGround)
        {
            // Если расстояние до земли очень маленькое - мы на земле
            if (distanceToGround < 0.01f)
            {
                isGrounded = true;
                currentFallVelocity = 0f;
                return;
            }

            // Если мы над поверхностью
            if (distanceToGround > 0)
            {
                isGrounded = false;

                // Если расстояние больше дистанции притяжения - используем свободное падение
                if (distanceToGround > groundSnapDistance)
                {
                    // Ускоряемся вниз (гравитация)
                    currentFallVelocity = Mathf.MoveTowards(
                        currentFallVelocity,
                        maxFallSpeed,
                        groundSnapSpeed * Time.deltaTime
                    );
                }
                else
                {
                    // В зоне притяжения - плавно двигаемся к земле
                    float targetVelocity = Mathf.Lerp(0, maxFallSpeed, distanceToGround / groundSnapDistance);

                    // Плавно изменяем скорость
                    currentFallVelocity = Mathf.MoveTowards(
                        currentFallVelocity,
                        targetVelocity,
                        groundSnapSpeed * 2f * Time.deltaTime
                    );
                }

                // Ограничиваем скорость падения расстоянием до земли
                float moveAmount = Mathf.Min(currentFallVelocity * Time.deltaTime, distanceToGround);

                // Не превышаем максимальное движение за кадр
                moveAmount = Mathf.Min(moveAmount, maxDownMovement);

                // Двигаем вниз
                transform.Translate(Vector3.down * moveAmount, Space.World);

                // Обновляем скорость падения для следующего кадра
                if (moveAmount >= distanceToGround)
                {
                    currentFallVelocity = 0f;
                    isGrounded = true;
                }
            }
            else
            {
                // Мы внутри объекта - выталкиваем наружу
                float pushOut = Mathf.Abs(distanceToGround);
                pushOut = Mathf.Min(pushOut, maxDownMovement);
                transform.Translate(Vector3.up * pushOut, Space.World);
                currentFallVelocity = 0f;
                isGrounded = true;
            }
        }

        // Публичный метод для проверки, на земле ли игрок
        public bool IsGrounded()
        {
            return isGrounded;
        }

        // Метод для сброса скорости падения (например, при прыжке)
        public void ResetFallVelocity()
        {
            currentFallVelocity = 0f;
        }
    }
}

