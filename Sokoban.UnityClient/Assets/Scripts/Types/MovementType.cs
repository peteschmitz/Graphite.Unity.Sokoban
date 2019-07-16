using System;
using UnityEngine;

public enum MovementType
{
    [Vector2(X = 0.0f, Y = -1.0f)]
    Down,

    [Vector2(X = 1.0f, Y = 0.0f)]
    Right,

    [Vector2(X = 0.0f, Y = 1.0f)]
    Up,

    [Vector2(X = -1.0f, Y = 0.0f)]
    Left
}

public static class MovementTypesHelper
{
    public static MovementType? AsMovementType(this Vector2Int angle) => AsMovementType((Vector2)angle);
    public static MovementType? AsMovementType(this Vector2 angle)
    {
        foreach (var movementType in Enum.GetValues(typeof(MovementType)))
        {
            var movementVector = ((MovementType)movementType).GetCustomTypeAttribute<Vector2Attribute>();
            if (movementVector != null && angle.x == movementVector.X && angle.y == movementVector.Y)
            {
                return (MovementType)movementType;
            }
        }
        return null;
        //throw new ArgumentException($"Velocity ({velocity.x},{velocity.y}) couldn't resolve to {nameof(MovementType)}");
    }

    public static Vector2 GetAngle(this MovementType? type) => type.HasValue ? GetAngle(type.Value) : new Vector2(0.0f, 0.0f);
    public static Vector2 GetAngle(this MovementType type)
    {
        return type.GetCustomTypeAttribute<Vector2Attribute>().AsVector2;
    }

    public static MovementType GetOpposite(this MovementType original)
    {
        switch (original)
        {
            case MovementType.Down:
                return MovementType.Up;
            case MovementType.Up:
                return MovementType.Down;
            case MovementType.Left:
                return MovementType.Right;
            case MovementType.Right:
                return MovementType.Left;
            default:
                throw new ArgumentException($"Cant find opposite for {original.ToString()}");
        }
    }
}
