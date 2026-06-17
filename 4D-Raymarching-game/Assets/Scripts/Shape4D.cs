using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shape4D : MonoBehaviour
{
    public enum ShapeType { HyperSphere, HyperCube, DuoCylinder, plane, Cone, FiveCell, SixteenCell };
    public enum Operation { Union, Blend, Substract, Intersect };
    
    [Header("Shape Settings")]
    public ShapeType shapeType;
    public Operation operation;

    [Header("4D Transform Settings")]
    public float positionW;
    [Tooltip ("The rotation around the xw, yw and zw planes respectively")]
    public Vector3 rotationW;
    public float scaleW = 1f;

    [Header("Render Settings")]
    public Color colour;
    [Range (0,1)]
    public float smoothRadius;

    [HideInInspector]
    public int numChildren;
    Vector4 parentScale = Vector4.one;

    public Vector4 Position()
    {
        Vector3 position3D = transform.position;
        return new Vector4(position3D.x, position3D.y, position3D.z, positionW);
    }

    public Vector3 Rotation()
    {
        return transform.eulerAngles * Mathf.Deg2Rad;
    }

    public Vector3 RotationW()
    {
        return rotationW * Mathf.Deg2Rad;
    }
    
    public Vector4 Scale()
    {
        if (transform.parent != null && transform.parent.TryGetComponent(out Shape4D shape))
        {
            parentScale = shape.Scale();
        }
        else parentScale = Vector4.one;

        Vector3 localScale3D = transform.localScale;
        return Vector4.Scale(new Vector4(localScale3D.x, localScale3D.y, localScale3D.z, scaleW), parentScale);

    }

}

