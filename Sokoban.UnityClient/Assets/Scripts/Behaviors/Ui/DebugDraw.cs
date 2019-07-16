using System;
using UnityEngine;

public static class DebugDraw
{
    public static GUIStyle DebugTextWhite { get; private set; }
    public static GUIStyle DebugBoxWhite { get; private set; }
    public static GUIStyle DebugBoxRed { get; private set; }
    public static GUIStyle DebugBoxGreen { get; private set; }
    public static GUIStyle DebugBoxBlue { get; private set; }

    public static void DrawWorldText(float worldX, float worldY, string text)
    {
        PrepareDebugStyle();
        var position = Camera.main.WorldToScreenPoint(new Vector3(worldX, worldY));
        DebugTextWhite.fontSize = Convert.ToInt32(70 / Camera.main.orthographicSize);
        var textSize = DebugTextWhite.CalcSize(new GUIContent(text));
        GUI.Label(new Rect(position.x - textSize.x / 2, Screen.height - position.y, textSize.x, textSize.y), text, DebugTextWhite);
    }

    public static void DrawWorldBox(Bounds bounds, GUIStyle boxStyle = null, string text = "")
    {
        PrepareDebugStyle();
        boxStyle = boxStyle ?? DebugBoxWhite;
        var rect = bounds.AsGuiRect();
        GUI.Box(rect, text, DebugBoxWhite);
    }

    private static void PrepareDebugStyle()
    {
        if (DebugTextWhite == null)
        {
            DebugTextWhite = new GUIStyle(GUI.skin.label);
            DebugTextWhite.fontStyle = FontStyle.Bold;
            DebugTextWhite.normal.textColor = new Color(1.0f, 1.0f, 1.0f, 0.3f);
        }
        if (DebugBoxWhite == null)
        {
            new (string prop, Color color)[]
            {
                (prop: nameof(DebugBoxWhite), color: new Color(1.0f, 1.0f, 1.0f, 0.2f)),
                (prop: nameof(DebugBoxRed), color: new Color(1.0f, 0.0f, 0.0f, 0.2f)),
                (prop: nameof(DebugBoxGreen), color: new Color(0.0f, 1.0f, 0.0f, 0.2f)),
                (prop: nameof(DebugBoxBlue), color: new Color(0.0f, 0.0f, 1.0f, 0.2f))
            }
            .ForEach(x =>
            {
                var guiStyle = new GUIStyle(GUI.skin.box);
                guiStyle.normal.background = TextureExtensions.New(x.color);
                typeof(DebugDraw).GetProperty(x.prop).SetValue(typeof(GUIStyle), guiStyle);
            });
        }
    }
}
