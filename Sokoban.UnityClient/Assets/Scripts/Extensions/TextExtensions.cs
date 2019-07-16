using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class TextExtensions
{
    public static Vector2 GetPreferredSize(this Text text, int widthBuffer = 5, int heightBuffer = 5)
    {
        var currentWidth = text.GetComponent<RectTransform>().rect.size.x;
        var settings = text.GetGenerationSettings(text.GetComponent<RectTransform>().rect.size);
        var preferredWidth = text.cachedTextGeneratorForLayout.GetPreferredWidth(text.text, settings);
        var preferredHeight = text.cachedTextGeneratorForLayout.GetPreferredHeight(text.text, settings);
        return new Vector2(preferredWidth + widthBuffer * 2, preferredHeight + heightBuffer * 2);
    }
}
