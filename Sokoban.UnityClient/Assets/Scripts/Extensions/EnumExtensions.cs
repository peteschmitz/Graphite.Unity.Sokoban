using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EnumExtensions
{
    public static T GetNext<T>(this T value) where T : Enum
    {
        var values = Enum.GetValues(typeof(T));
        var index = Array.IndexOf(values, value);
        return index >= 0 && index + 1 < values.Length ? (T)values.GetValue(index + 1) : value;
    }

    public static T GetPrevious<T>(this T value) where T : Enum
    {
        var values = Enum.GetValues(typeof(T));
        var index = Array.IndexOf(values, value);
        return index > 0 ? (T)values.GetValue(index - 1) : value;
    }
}
