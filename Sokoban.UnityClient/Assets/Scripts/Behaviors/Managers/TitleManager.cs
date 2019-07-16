using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleManager : BaseBehavior
{
    public const string Scene = "TitleScene";

    public GameObject playButton;
    public GameObject createButton;
    public GameObject menuButton;
    public GameObject toggleSoundButton;
    public AlertIAP iapAlert;
    public GameObject versionText;
    public GameObject levelPanel;
    public GameObject mainMenuPanel;
    public Button iapLevelsButton;
    public Button restorePurchaseButton;
    public Alert restorePurchaseAlert;

    private LevelListManager levelManager;
    private MainMenuManager menuManager;

    private IAPManager iapManager => this.iapAlert?.iapManager;
#if UNITY_IOS || UNITY_TVOS
    private bool allowRestoration = true;
#else
    private bool allowRestoration = false;
#endif


    // private Alert alert;

    // Start is called before the first frame update
    protected override void Start()
    {
#if UNITY_TVOS
    UnityEngine.tvOS.Remote.reportAbsoluteDpadValues = true;
    UnityEngine.tvOS.Remote.allowExitToHome  = true;
#endif

        base.Start();
        this.audioSource = this.GetComponent<AudioSource>();
        // this.alert = this.iapAlert.GetComponent<Alert>();
        // alert will hide once initialized
        this.iapAlert.ToggleActive(true, false);
        this.restorePurchaseAlert?.ToggleActive(true, false);

        var sound = this.toggleSoundButton.GetComponent<CustomButton>();
        sound.Toggle(this.Context.data.IsSoundEnabled);
        sound.OnValueChangeEvent.AddListener((caller, value) =>
        {
            if (!GameContext.IsNavigationEnabled)
            {
                this.EventSystem.SetSelectedGameObject(null);
            }
            this.Context.data.IsSoundEnabled = value;
        });

        this.iapManager.WithGrant(IAPManager.IAPRemoveAds, () =>
        {
            this.UpdateMenuButton();
        });

        var version = this.versionText.GetComponent<Text>();
        var versionBuilder = new StringBuilder("");
        // versionBuilder.AppendLine($"© Graphite Software, 2019");
        versionBuilder.Append($"v{Application.version}");
        version.text = versionBuilder.ToString();

        this.levelManager = this.levelPanel.GetComponent<LevelListManager>();
        this.levelManager.OnLevelSelectedEvent.AddListener((data, levelData) =>
        {
            Debug.Log($"Level selection: {levelData.relativePath}");
            this.Context.data.SetActiveData(this.Context, data, levelData.relativePath);
            SceneManager.LoadScene(GameplayManager.Scene);
        });
        this.menuManager = this.mainMenuPanel.GetComponent<MainMenuManager>();
        this.menuManager.OnPageSelectedEvent.AddListener((caller, page) =>
        {
            this.PlaySfx();
            switch (page)
            {
                case MainMenuManager.MenuPageType.Create:
                    SceneManager.LoadScene(EditorManager.Scene);
                    break;
                case MainMenuManager.MenuPageType.LevelList:
                    this.NavigateLevelList();
                    break;
            }
        });

        this.iapLevelsButton.onClick.AddListener(() => this.iapAlert?.PromptIap(IAPLevelPak.LevelPak1));
        this.iapManager.OnGrantEvent.AddListener((grant, fromLocal) =>
        {
            if (!fromLocal && grant.Equals(IAPLevelPak.LevelPak1))
            {
                this.iapLevelsButton.gameObject.SetActive(false);
            }
        });
        this.iapAlert.OnVisibilityEvent.AddListener((isVisible) =>
        {
            if (!isVisible && GameContext.IsNavigationEnabled && this.mainMenuPanel.activeInHierarchy)
            {
                this.SelectDefault();
            }
        });
        this.restorePurchaseAlert.OnVisibilityEvent.AddListener((isVisible) =>
        {
            if (!isVisible && GameContext.IsNavigationEnabled && this.mainMenuPanel.activeInHierarchy)
            {
                this.SelectDefault();
            }
        });

        this.restorePurchaseButton.onClick.AddListener(this.AlertRestore);
        this.iapManager.OnRestoredEvent.AddListener(this.UpdateRestore);

        this.NavigateTitle();
    }

    void Update()
    {
        if (!GameContext.HasRuntimeNavigation && Input.GetButtonDown("Submit"))
        {
            var wasNavigationEnabled = GameContext.IsNavigationEnabled;
            GameContext.HasRuntimeNavigation = true;
            if (!wasNavigationEnabled)
            {
                this.EventSystem.SetSelectedGameObject(this.playButton);
                var play = this.playButton.GetComponent<CustomButton>();
                play.onClick.Invoke();
            }
        }
    }

    private void PromptIapLevels()
    {
        this.iapAlert.Prompt((handle) =>
        {
            if (handle.result == Alert.ResultType.Accepted)
            {
                Debug.Log("Approved IAP");
            }
        });
    }

    private void PromptClearData()
    {
        this.iapAlert.TitleText = "Clear Game Data";
        this.iapAlert.DescriptionText = "Are you sure you want to reset all level progress?";
        this.iapAlert.Prompt((handle) =>
        {
            if (handle.result == Alert.ResultType.Accepted)
            {
                this.Context.ResetData();
            }
        });
    }

    public void SelectDefault()
    {
        this.EventSystem.SetSelectedGameObject(this.playButton);
    }

    public void NavigateTitle()
    {
        AnalyticsManager.Event(() => AnalyticsEvent.ScreenVisit("title"));
        this.levelManager.ToggleActive(false);
        this.mainMenuPanel.SetActive(true);
        this.iapLevelsButton.gameObject.SetActive(false);
        this.restorePurchaseButton?.gameObject?.SetActive(this.allowRestoration);
        this.UpdateMenuButton();

        this.EventSystem.SetSelectedGameObject(null);
        if (GameContext.IsNavigationEnabled)
        {
            this.SelectDefault();
        }

        // this.restorePurchaseButton?.gameObject?.SetActive(this.allowRestoration);
    }

    private void AlertRestore()
    {
        if (this.restorePurchaseAlert == null)
        {
            return;
        }
        this.restorePurchaseAlert.TitleText = "";
        this.restorePurchaseAlert.AcceptText = this.Locale.Get(LocaleTextType.Close, "Close");
        this.restorePurchaseAlert.declineObject?.SetActive(false);
        this.restorePurchaseAlert.DescriptionText = this.Locale.Get(LocaleTextType.Restoring, "Restoring...");
        this.restorePurchaseAlert.Prompt((result) =>
        {

        });
        this.iapManager?.RestoreTransactions();
    }

    private void UpdateRestore(bool isSuccess)
    {
        if (this.restorePurchaseAlert != null && this.allowRestoration && this.restorePurchaseAlert.IsActive)
        {
            var restoreResultText = this.Locale.Get(LocaleTextType.RestoreFinished, "Restore finished");
            var restoreResultStatusText = this.Locale.Get(isSuccess ? LocaleTextType.Success : LocaleTextType.Failure);
            this.restorePurchaseAlert.DescriptionText = $"{restoreResultText}: {restoreResultStatusText}";
        }
    }

    private void NavigateLevelList()
    {
        this.restorePurchaseButton?.gameObject?.SetActive(false);

        var metaData = this.Context.file.Get<WarehouseManager.MetaData>();
        if (!metaData.levelData.HasItems() || !metaData.levelData.Any(x => x.IsCompleted))
        {
            AdManager.SkipCounter = 2;
            // this.Context.data.MoveNext(this.Context, null, this.iapManager);
            // SceneManager.LoadScene(GameplayManager.Scene);
            // return;
        }

        AnalyticsManager.Event(() => AnalyticsEvent.ScreenVisit("levelList"));
        // this.levelPanel.SetActive(true);
        this.levelManager.ToggleActive(true);
        this.mainMenuPanel.SetActive(false);
        this.iapLevelsButton.gameObject.SetActive(!this.iapManager.HasGrant(IAPLevelPak.LevelPak1));
        this.UpdateMenuButton();
    }

    private void UpdateMenuButton()
    {
        var button = this.menuButton.GetComponent<CustomButton>();
        var text = button.GetComponentInChildren<Text>();

        // level page defaults
        var isActive = true;
        text.text = this.Locale.Get(LocaleTextType.Menu, "Main Menu");
        UnityAction onClick = () => this.NavigateTitle();

        // title page
        if (this.mainMenuPanel.activeSelf)
        {
            isActive = !this.iapManager.HasGrant(IAPManager.IAPRemoveAds);
            text.text = this.Locale.Get(LocaleTextType.RemoveAds, "Remove Ads");
            onClick = () =>
            {
                this.PlaySfx();
                iapAlert.PromptIap(IAPManager.IAPRemoveAds);
            };
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);
        this.menuButton.SetActive(isActive);
    }
}
