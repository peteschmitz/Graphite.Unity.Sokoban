using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if !UNITY_TVOS
using UnityEngine.Advertisements;
#endif
using UnityEngine.Analytics;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AdManager : BaseBehavior//, IUnityAdsListener
{
    private static readonly TimeSpan AdIntervalSpan = TimeSpan.FromMinutes(4);

    public const string Scene = "AdScene";
    public static int SkipCounter = 0;

    public bool showAtStart;
    public bool loadSceneAfterAd;
    public string targetScene = GameplayManager.Scene;
    public Button continueButton;
    public TimeSpan continuationSpan = TimeSpan.FromSeconds(3.0f);

#if !UNITY_TVOS
    public class AdEvent : UnityEvent<string> { }
    public class AdResultEvent : UnityEvent<string, ShowResult> { }

    // public AdEvent OnUnityAdsReadyEvent = new AdEvent();
    public AdEvent OnUnityAdsDidErrorEvent = new AdEvent();
    // public AdEvent OnUnityAdsDidStartEvent = new AdEvent();
    public AdResultEvent OnUnityAdsDidFinishEvent = new AdResultEvent();

    private static readonly string PlacementVideo = "video";
    private static DateTime LastAdDate = default(DateTime);

    string gameId = "cafcd252-253f-4e64-8139-7bd1c19b4df7";

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        // this.OnUnityAdsReadyEvent.AddListener((placementId) => Debug.Log($"AdManager.AdsReadyEvent-> placement: {placementId}"));
        this.OnUnityAdsDidErrorEvent.AddListener((message) => Debug.Log($"AdManager.AdsDidError-> message: {message}"));
        // this.OnUnityAdsDidStartEvent.AddListener((placementId) => Debug.Log($"AdManager.AdsDidStart-> placement: {placementId}"));
        this.OnUnityAdsDidFinishEvent.AddListener((placementId, result) =>
        {
            Debug.Log($"AdManager.AdsDidFinish-> placement: {placementId}, result: {result}");
            if (this.loadSceneAfterAd)
            {
                this.LoadScene(this.targetScene);
            }
        });

        if (!Advertisement.isInitialized)
        {
            Advertisement.Initialize(gameId, GameContext.IsDebugMode);
            if (GameContext.IsDebugMode)
            {
                Advertisement.debugMode = true;
            }
        }

        if (!Advertisement.isSupported || !Advertisement.IsReady())
        {
            if (this.loadSceneAfterAd)
            {
                this.LoadScene(this.targetScene);
            }
            return;
        }

        if (this.showAtStart)
        {
            this.continueButton?.gameObject?.SetActive(false);
            this.RunAd();
        }
    }

    private void OnShowResult(string placementId, ShowResult result)
    {
        AnalyticsManager.Event(() =>
        {
            switch (result)
            {
                case ShowResult.Failed:
                    AnalyticsEvent.AdSkip(false, eventData: new Dictionary<string, object> { { "skipReason", "showFailed" } });
                    break;
                case ShowResult.Skipped:
                    AnalyticsEvent.AdSkip(false, eventData: new Dictionary<string, object> { { "skipReason", "showSkipped" } });
                    break;
                case ShowResult.Finished:
                    AnalyticsEvent.AdComplete(false);
                    break;
            }
        });

        if (result == ShowResult.Failed)
        {
            this.OnUnityAdsDidError($"failed to show advertisement for '{placementId}'");
        }
        this.OnUnityAdsDidFinish(placementId, result);
    }

    public void OnUnityAdsDidFinish(string placementId, ShowResult showResult)
    {
        this.OnUnityAdsDidFinishEvent.Invoke(placementId, showResult);
    }

    public void OnUnityAdsDidError(string message)
    {
        this.OnUnityAdsDidErrorEvent.Invoke(message);
    }
#endif

    void Update()
    {
        if (this.continueButton?.gameObject != null && !this.continueButton.gameObject.activeSelf)
        {
            this.continuationSpan -= TimeSpan.FromSeconds(Time.deltaTime);
            if (this.continuationSpan.TotalSeconds < 0)
            {
                this.continueButton.gameObject.SetActive(true);
            }
        }
    }

    public void ForceNext()
    {
        this.LoadScene(this.targetScene);
    }

    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public static bool ShouldShow(bool justCompletedLevel = false)
    {
#if UNITY_TVOS
        return false;
#else
        if (SkipCounter > 0)
        {
            --SkipCounter;
            return false;
        }
        return DateTime.Now - LastAdDate > AdIntervalSpan;
#endif
    }

    public void RunAd()
    {
#if UNITY_TVOS
        return;
#else
        AnalyticsManager.Event(() => AnalyticsEvent.AdStart(false));

        LastAdDate = DateTime.Now;
        Advertisement.Show(PlacementVideo, new ShowOptions
        {
            resultCallback = (action) => this.OnShowResult(PlacementVideo, action)
        });
#endif
    }
}
