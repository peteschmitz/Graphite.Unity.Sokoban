using System.Collections.Generic;
using UnityEngine;

public static class TransformExtensions
{
    #region Transform
    public static Transform WithPositionX(this Transform transform, float newX)
    {
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        return transform;
    }

    public static Transform WithPositionY(this Transform transform, float newY)
    {
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        return transform;
    }

    public static Transform WithPositionZ(this Transform transform, float newZ)
    {
        transform.position = new Vector3(transform.position.x, transform.position.y, newZ);
        return transform;
    }

    public static IEnumerable<Transform> WithChildren(this Transform transform)
    {
        var childTransforms = new List<Transform>();
        for (var i = 0; i < transform.childCount; ++i)
        {
            childTransforms.Add(transform.GetChild(i));
        }
        return childTransforms;
    }
    #endregion

    #region Vector3
    public static Vector3 WithX(this Vector3 vector, float newX)
    {
        return new Vector3(newX, vector.y, vector.z);
    }

    public static Vector3 WithY(this Vector3 vector, float newY)
    {
        return new Vector3(vector.x, newY, vector.z);
    }

    public static Vector3 WithZ(this Vector3 vector, float newZ)
    {
        return new Vector3(vector.x, vector.y, newZ);
    }

    public static Vector3 AddX(this Vector3 vector, float newX)
    {
        return new Vector3(vector.x + newX, vector.y, vector.z);
    }

    public static Vector3 AddY(this Vector3 vector, float newY)
    {
        return new Vector3(vector.x, vector.y + newY, vector.z);
    }

    public static Vector3 AddZ(this Vector3 vector, float newZ)
    {
        return new Vector3(vector.x, vector.y, vector.z + newZ);
    }
    #endregion

    #region Vector2
    public static Vector2 WorldToGUIPoint(Vector3 world)
    {
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(world);
        screenPoint.y = Screen.height - screenPoint.y;
        return screenPoint;
    }

    public static Vector2 WithX(this Vector2 vector, float newX)
    {
        return new Vector2(newX, vector.y);
    }

    public static Vector2 WithY(this Vector2 vector, float newY)
    {
        return new Vector2(vector.x, newY);
    }

    public static Vector2 AddX(this Vector2 vector, float newX)
    {
        return new Vector2(vector.x + newX, vector.y);
    }

    public static Vector2 AddY(this Vector2 vector, float newY)
    {
        return new Vector2(vector.x, vector.y + newY);
    }

    public static Vector2Int AsVector2Int(this Vector2 vector)
    {
        return new Vector2Int((int)vector.x, (int)vector.y);
    }
    #endregion

    #region Vector2Int
    public static Vector2Int WithX(this Vector2Int vector, int newX)
    {
        return new Vector2Int(newX, vector.y);
    }

    public static Vector2Int WithY(this Vector2Int vector, int newY)
    {
        return new Vector2Int(vector.x, newY);
    }

    public static Vector2Int AddX(this Vector2Int vector, int newX)
    {
        return new Vector2Int(vector.x + newX, vector.y);
    }

    public static Vector2Int AddY(this Vector2Int vector, int newY)
    {
        return new Vector2Int(vector.x, vector.y + newY);
    }
    #endregion

    #region Rect
    // http://answers.unity.com/answers/1191276/view.html
    public static Rect AsGuiRect(this Bounds bounds)
    {
        Vector3 cen = bounds.center;
        Vector3 ext = bounds.extents;
        Vector2[] extentPoints = new Vector2[8]
         {
               WorldToGUIPoint(new Vector3(cen.x-ext.x, cen.y-ext.y, cen.z-ext.z)),
               WorldToGUIPoint(new Vector3(cen.x+ext.x, cen.y-ext.y, cen.z-ext.z)),
               WorldToGUIPoint(new Vector3(cen.x-ext.x, cen.y-ext.y, cen.z+ext.z)),
               WorldToGUIPoint(new Vector3(cen.x+ext.x, cen.y-ext.y, cen.z+ext.z)),
               WorldToGUIPoint(new Vector3(cen.x-ext.x, cen.y+ext.y, cen.z-ext.z)),
               WorldToGUIPoint(new Vector3(cen.x+ext.x, cen.y+ext.y, cen.z-ext.z)),
               WorldToGUIPoint(new Vector3(cen.x-ext.x, cen.y+ext.y, cen.z+ext.z)),
               WorldToGUIPoint(new Vector3(cen.x+ext.x, cen.y+ext.y, cen.z+ext.z))
         };
        Vector2 min = extentPoints[0];
        Vector2 max = extentPoints[0];
        foreach (Vector2 v in extentPoints)
        {
            min = Vector2.Min(min, v);
            max = Vector2.Max(max, v);
        }
        return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
    }

    public static Rect AsNormalizedUv(this Rect rect, int width, int height)
    {
        var left = rect.xMin / width;
        var top = rect.yMin / height;
        var right = rect.xMax / width;
        var bottom = rect.yMax / height;
        return new Rect(left, top, right - left, bottom - top);
    }
    #endregion
}
