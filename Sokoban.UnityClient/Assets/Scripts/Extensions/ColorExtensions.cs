using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ColorExtensions
{
    public static Color WithR(this Color color, float r)
    {
        return new Color(r, color.g, color.b, color.a);
    }

    public static Color WithG(this Color color, float g)
    {
        return new Color(color.r, g, color.b, color.a);
    }

    public static Color WithB(this Color color, float b)
    {
        return new Color(color.r, color.g, b, color.a);
    }

    public static Color WithA(this Color color, float a)
    {
        return new Color(color.r, color.g, color.b, a);
    }
}
