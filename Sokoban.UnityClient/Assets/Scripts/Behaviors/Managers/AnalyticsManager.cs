using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

public static class AnalyticsManager
{
    private static readonly bool AnalyticsEnabled = true;

    public static void Event(Action analyticsEvent)
    {
        AnalyticsEvent.debugMode = GameContext.IsDebugMode;

        if (!AnalyticsEnabled)
        {
            return;
        }

        try
        {
            analyticsEvent.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"Analytics Error: {e.GetType().Name} ({e.Message})");
        }
    }
}
