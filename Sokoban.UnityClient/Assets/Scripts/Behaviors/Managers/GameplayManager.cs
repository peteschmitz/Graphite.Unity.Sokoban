using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Notices;

public class GameplayManager : BaseBehavior, BaseItem.ITimeProvider
{
    public class Event : UnityEvent<GameplayManager> { }

    public const string Scene = "GameplayScene";

    public Event OnWinEvent = new Event();

    public float cameraPadding = 0.3f;
    public AlertGameplay alert;
    public Notice notice;
    public CustomButton restartButton;
    public CustomButton toggleSoundButton;
    public CustomButton menuButton;
    public CustomButton editorButton;
    public Text boxCounterText;
    public Text levelNameText;
    public IAPManager iapManager;
    // public Button iapAdsButton;
    // public Button iapLevelsButton;
    public AlertIAP iapAlert;

    private WarehouseManager warehouseManager { get; set; }
    private Player player { get; set; }
    private Ground playerGround { get; set; }
    private Vector2Int lastCameraAdjustment { get; set; }
    private Camera gameplayCamera { get; set; }
    private uint timeMs { get; set; }

    #region properties
    public uint TimeMs => this.timeMs;
    #endregion

    // Start is called before the first frame update
    protected override void Start()
    {
#if UNITY_TVOS
    UnityEngine.tvOS.Remote.allowExitToHome  = false;
#endif
        base.Start();

        // alert will hide once initialized
        this.alert.ToggleActive(true, false);
        this.iapAlert.ToggleActive(true, false);

        // notice will hide once initialized
        this.notice.ToggleActive(true);

        this.UpdateUi();

        #region iaps
        // "remove ads"
        this.iapManager?.OnGrantEvent.AddListener((grant, fromLocal) =>
        {
            if (grant.Equals(IAPManager.IAPRemoveAds))
            {
                this.alert.ToggleExtras(ads: false);
            }
        });
        if (this.alert.iapRemoveAdsButton != null)
        {
            this.alert.iapRemoveAdsButton.onClick.AddListener(() =>
            {
                this.iapAlert.PromptIap(IAPManager.IAPRemoveAds);
                this.restartButton?.PlaySfx();
            });
        }

        // "more levels"
        this.iapManager?.OnGrantEvent.AddListener((grant, fromLocal) =>
        {
            if (grant.Equals(IAPLevelPak.LevelPak1))
            {
                this.alert.iapMoreLevelsButton?.gameObject.SetActive(false);
            }
        });
        if (this.alert.iapMoreLevelsButton != null)
        {
            this.alert.iapMoreLevelsButton.onClick.AddListener(() =>
            {
                this.iapAlert.PromptIap(IAPLevelPak.LevelPak1);
                this.restartButton?.PlaySfx();
            });
        }

        this.iapAlert.OnVisibilityEvent.AddListener((isVisible) =>
        {
            if (!isVisible && GameContext.IsNavigationEnabled && this.alert.gameObject.activeInHierarchy)
            {
                this.alert.SelectDefault();
            }
        });
        #endregion

        this.timeMs = 0;
        this.gameplayCamera = Camera.main;
        this.warehouseManager = this.GetComponent<WarehouseManager>();
        this.warehouseManager.timeProvider = this;
        this.warehouseManager.OnGroundSelectedEvent.AddListener(this.OnGroundSelected);
        this.warehouseManager.OnOccupantSelectedEvent.AddListener(this.OnItemSelected);
        this.warehouseManager.OnItemAddedEvent.AddListener(this.OnItemAdded);
        this.warehouseManager.OnInitializedEvent.AddListener((warehouse) =>
        {
            this.playerGround = this.warehouseManager.GetGroundByOccupantTypes<Player>().First();
            this.player = (Player)this.playerGround.occupant;
            this.UpdateBoxCounter();
            if (this.levelNameText != null)
            {
                this.levelNameText.text = this.Context.data.activeWarehouseData.DisplayName;
            }

            AnalyticsManager.Event(() =>
            {
                AnalyticsEvent.LevelStart(
                    this.Context.data.activeWarehouseData.NormalizedName,
                    new Dictionary<string, object>
                    {
                        {"id", this.Context.data.activeWarehouseData.localIdentifier}
                    }
                );
            });
            // if (this.gameplayEditorPanel != null)
            // {
            //     this.gameplayEditorPanel.OnNameSelectedEvent.AddListener(this.OnEditorSelection);
            //     this.gameplayEditorPanel.SetVisibility(this.Context.data.enableGameplayEditor);
            // }
        });
        this.warehouseManager.OnAnimatedInEvent.AddListener((warehouse) =>
        {
            this.Context.SetRunning(true);

            var noticeItem = this.GetNoticeItem();
            if (noticeItem.HasValue)
            {
                notice.Show(noticeItem.Value);
            }
            // notice.Show("asdf", TimeSpan.FromSeconds(3));
        });

        #region bottom navigation input
        this.toggleSoundButton.Toggle(this.Context.data.IsSoundEnabled);
        this.toggleSoundButton?.OnValueChangeEvent.AddListener((caller, value) =>
        {
            this.Context.data.IsSoundEnabled = value;
        });
        this.alert?.soundToggleButton?.Toggle(this.Context.data.IsSoundEnabled);
        this.alert?.soundToggleButton?.OnValueChangeEvent.AddListener((caller, value) =>
        {
            this.Context.data.IsSoundEnabled = value;
        });

        UnityAction restartAction = () =>
        {
            if (!this.Context.isRunning && this.restartButton?.gameObject?.activeInHierarchy == true)
            {
                return;
            }

            AnalyticsManager.Event(() =>
            {
                AnalyticsEvent.LevelFail(
                    this.Context.data.activeWarehouseData.NormalizedName,
                    new Dictionary<string, object>
                    {
                        {"id", this.Context.data.activeWarehouseData.localIdentifier},
                        {"timeMs", this.timeMs},
                        {"moveCount", this.Context.data.RecentMovementHistory.Count}
                    }
                );
            });

            this.menuButton?.PlaySfx();
            SceneManager.LoadScene(Scene);
        };
        this.restartButton?.onClick.AddListener(restartAction);
        this.alert?.restartButton?.onClick.AddListener(restartAction);

        if (this.editorButton != null)
        {
            this.editorButton.onClick.AddListener(() =>
            {

                if (!this.Context.isRunning)
                {
                    return;
                }
                SceneManager.LoadScene(EditorManager.Scene);
            });
            this.editorButton.gameObject.SetActive(this.Context.data.enableGameplayEditor);
        }
        this.menuButton?.onClick.AddListener(this.OnMenuClick);
        #endregion

        this.Context.data.RecentMovementHistory = new List<BaseItem.MovementHistory>();
        this.Context.SetRunning(false);
    }

    void Update()
    {
        if (this.alert?.IsActive == false)
        {
            if (Input.GetKeyUp(KeyCode.Escape) || GameContext.IsNavigationEnabled && Input.GetButtonDown("Submit"))
            {
                this.menuButton?.PlaySfx();
                this.OnMenuClick();
                return;
            }
        }

        this.AdjustStaticCamera();

        if (!this.Context.isRunning)
        {
            return;
        }
        this.timeMs += (uint)(Time.deltaTime * 1000);
    }

    private void OnMenuClick()
    {
        if (!this.Context.isRunning)
        {
            return;
        }
        this.Context.SetRunning(false);
        this.PromptMenu();
    }

    private NoticeItem? GetNoticeItem()
    {
        var meta = this.File.Get<WarehouseManager.MetaData>();
        var levelIdentifier = this.Context.data.activeWarehouseData?.localIdentifier;
        if (!levelIdentifier.IsValid() || meta == null)
        {
            return null;
        }
        var activeItem = Notices.LevelNotices.FirstOrDefault(x => x.localIdentifier.Equals(levelIdentifier));
        if (!activeItem.noticeKey.IsValid() ||
            activeItem.showOnce && meta.userSettings.shownNotices.AsNotNull().Contains(activeItem.noticeKey))
        {
            return null;
        }
        meta.userSettings.shownNotices = meta.userSettings.shownNotices ?? new List<string>();
        if (!meta.userSettings.shownNotices.Contains(activeItem.noticeKey))
        {
            meta.userSettings.shownNotices.Add(activeItem.noticeKey);
        }
        return activeItem;
    }
    private void OnGroundSelected(BaseItem item) => this.OnItemSelected(item as Ground);
    private void OnItemSelected(Ground item, BaseItem occupant = null)
    {
        if (item == null)
        {
            return;
        }

        BaseItem playerDestination = null;
        var isQueue = false;

        if (player.isMoving/* || warehouseManager.HasMovingItems*/)
        {
            isQueue = true;
            playerDestination = player.warehouseDestination ?? player.parent;
        }
        else
        {
            playerDestination = player;
        }

        var distanceFromDestination = item.Distance(playerDestination);
        var intendedMovement = distanceFromDestination.AsMovementType();
        if (intendedMovement == null ||
            item?.WarehouseIndex == null)
        {
            Debug.Log($"Player movement-queue failed sanity check ({distanceFromDestination.ToString()})");
            return;
        }

        var destination = item.WarehouseIndex.Value;
        var inboundOccupant = this.warehouseManager.GetInboundOccupant(destination);
        if (inboundOccupant != null ||
            !this.warehouseManager.IsTraversable(destination, intendedMovement.Value, player.PushStrength))
        {
            Debug.Log($"Player movement-queue failed traversable check ({inboundOccupant?.gameObject?.name ?? destination.ToString()})");
            return;
        }

        Debug.Log($"Movement from player {intendedMovement.ToString()} ({(isQueue ? "Queue: " : "")}{distanceFromDestination.ToString()})");

        if (isQueue)
        {
            player.warehouseDestinationQueue = item;
        }
        else
        {
            player.warehouseDestination = item;
        }
    }

    private void OnItemAdded(BaseItem item)
    {
        item.OnArriveDestinationEvent.AddListener(this.OnItemArriveDestination);
        item.OnStartDestinationEvent.AddListener(this.OnStartDestination);
    }

    private async void OnItemArriveDestination(Ground destination, BaseItem item)
    {
        destination.SetOccupant(item, false);

        if (item.GetType().IsAssignableFrom(typeof(Box)))
        {
            var boxMatch = false;
            var isContinuation = false;
            var box = (Box)item;
            var lastMovement = item.movementDirection.Value;
            // Blue box doesn't stop
            if (box.boxType == BoxType.Blue)
            {
                var destinationIndex = item.WarehouseIndex.Value + lastMovement.GetAngle().AsVector2Int();
                if (this.warehouseManager.IsTraversable(destinationIndex, lastMovement, box.PushStrength))
                {
                    box.warehouseDestinationQueue = this.warehouseManager.GetGround(destinationIndex);
                    isContinuation = true;
                }
            }

            if (!isContinuation)
            {
                if (destination?.OccupantAsMarker != null &&
                    destination.occupant.GetType().IsAssignableFrom(typeof(Box)))
                {
                    var destinationMarker = destination.OccupantAsMarker;
                    var destinationBox = (Box)destination.occupant;
                    if (destinationMarker.markerType == destinationBox.boxType)
                    {
                        await this.OnMatchingBoxMarker(destinationMarker, destinationBox);
                        boxMatch = true;
                    }
                }
            }

            if (!boxMatch)
            {
                this.OnNonMatchingBoxMovement(box);
            }
        }
    }

    private async Task OnMatchingBoxMarker(Marker marker, Box box)
    {
        this.UpdateBoxCounter();
        box.StartMarkerAnimation();
        Debug.Log($"Box hit goal at {marker.WarehouseIndex.ToString()}");
        if (this.warehouseManager.HasMarkerWinCondition)
        {
            Debug.Log($"All goals achieved");
            await this.OnWin();
        }
    }

    private void OnNonMatchingBoxMovement(Box box)
    {
        box.StopMarkerAnimation();
        this.UpdateBoxCounter();
    }

    private void UpdateBoxCounter()
    {
        if (this.boxCounterText != null)
        {
            var boxLocale = this.Locale.Get(LocaleTextType.Boxes, "Boxes");
            this.boxCounterText.text = $"{boxLocale} {this.warehouseManager.MatchedMarkerCount}/{this.warehouseManager.MarkerCount}";
        }
    }

    private async Task OnWin()
    {
        AnalyticsManager.Event(() =>
        {
            AnalyticsEvent.LevelComplete(
                this.Context.data.activeWarehouseData.NormalizedName,
                new Dictionary<string, object>
                {
                    {"id", this.Context.data.activeWarehouseData.localIdentifier},
                    {"timeMs", this.timeMs},
                    {"moveCount", this.Context.data.RecentMovementHistory.Count}
                }
            );

            // very first completion
            if (!this.Context.data.activeWarehouseLevelMeta.IsCompleted &&
                this.Context.data.activeWarehouseData.localIdentifier.Equals(Notices.LevelNotices.First().localIdentifier))
            {
                AnalyticsEvent.FirstInteraction("completedLevel");
            }
        });

        this.Context.SetRunning(false);
        // var activeMeta = this.Context.data.activeWarehouseLevelMeta;
        // activeMeta.Update(this.Context.data, this);
        this.Context.data.UpdateLevelMetaData(this.Context.data, this);
        var activeMeta = this.Context.data.activeWarehouseLevelMeta;
        if (!this.Context.data.enableGameplayEditor)
        {
            var meta = this.File.Get<WarehouseManager.MetaData>();
            meta.levelData.RemoveAll(x => x.Identifier == activeMeta.Identifier);
            meta.levelData.Add(activeMeta);
            this.File.Save(meta);
        }
        await this.warehouseManager.AnimateItems(() =>
        {
            this.PlaySfx();
            this.PromptWin();
            this.OnWinEvent.Invoke(this);
        });
    }

    private void OnStartDestination(Ground destination, BaseItem item, MovementType movement)
    {
        if (destination.occupant != null && !destination.occupant.IsPassiveOccupant)
        {
            destination.occupant.warehouseDestination =
                this.warehouseManager.GetGround(destination.occupant.WarehouseIndex.Value + movement.GetAngle().AsVector2Int());
        }
    }

    // private void OnEditorSelection(UiSpritePanel panel, string buttonText)
    // {
    //     if (buttonText == this.Context.Text.E_Editor)
    //     {
    //         SceneManager.LoadScene(EditorManager.Scene);
    //     }
    //     else if (buttonText == this.Context.Text.E_Reset)
    //     {
    //         SceneManager.LoadScene(Scene);
    //     }
    // }

    private void AdjustStaticCamera()
    {
        if (this.lastCameraAdjustment.x == Screen.width && this.lastCameraAdjustment.y == Screen.height)
        {
            return;
        }

        var warehouseBounds = this.warehouseManager.WorldBounds;
        this.gameplayCamera.transform.position = warehouseBounds.center
            .WithZ(this.gameplayCamera.transform.position.z);

        // Reminder: OrthographicSize resizes the camera's height to the world units of value * 2
        var ratioH = warehouseBounds.size.y / Screen.height;
        var ratioW = warehouseBounds.size.x / Screen.width;
        if (ratioW > ratioH)
        {
            this.gameplayCamera.orthographicSize = warehouseBounds.size.y / 2 * (ratioW / ratioH) + this.cameraPadding;
        }
        else
        {
            this.gameplayCamera.orthographicSize = warehouseBounds.size.y / 2 + this.cameraPadding;
        }

        this.lastCameraAdjustment = new Vector2Int(Screen.width, Screen.height);
    }
    private void PreparePromptIap()
    {
        var currentCategory = this.Context.data?.activeWarehouseData?.runtimeCategory ?? CategoryType.Easy;
        this.alert.ToggleExtras(
            ads: !this.iapManager.HasGrant(IAPManager.IAPRemoveAds),
            levels: !this.iapManager.HasGrant(IAPLevelPak.LevelPak1) && currentCategory == CategoryType.Hard);
        // this.iapAdsButton.gameObject.SetActive(!this.iapManager.HasGrant(IAPManager.IAPRemoveAds));
        // this.iapLevelsButton.gameObject.SetActive(!this.iapManager.HasGrant(IAPLevelPak.LevelPak1) && currentCategory == CategoryType.Hard);
    }

    private void PromptWin()
    {
        this.PreparePromptIap();

        this.alert.SetDefault();
        this.alert.ToggleExtras(restart: false);
        this.alert.TitleText = this.Locale.Get(LocaleTextType.LevelCompleted, "Level Completed!");
        this.alert.DescriptionText = this.StatDescription(true);
        if (this.Context.data.enableGameplayEditor)
        {
            this.alert.AcceptText = this.Locale.Get(LocaleTextType.Editor, "Editor");
        }
        else
        {
            this.alert.AcceptText = this.Locale.Get(LocaleTextType.Next, "Next");
        }
        this.alert.DeclineText = this.Locale.Get(LocaleTextType.Replay, "Replay");
        this.alert.Prompt(result =>
        {
            switch (result.result)
            {
                case Alert.ResultType.Accepted:
                    if (this.Context.data.enableGameplayEditor)
                    {
                        SceneManager.LoadScene(EditorManager.Scene);
                    }
                    else
                    {
                        this.Context.data.MoveNext(this.Context, iapManager: this.iapManager);
                        if (iapManager.HasGrant(IAPManager.IAPRemoveAds) || !AdManager.ShouldShow(true))
                        {
                            SceneManager.LoadScene(Scene);
                        }
                        else
                        {
                            SceneManager.LoadScene(AdManager.Scene);
                        }
                    }
                    return;
                default:
                    SceneManager.LoadScene(Scene);
                    return;
            }
        }, setInvisible: true);
    }

    private void PromptMenu()
    {
        this.PreparePromptIap();

        this.alert.SetDefault();
        this.alert.TitleText = this.Locale.Get(LocaleTextType.GamePaused, "Game Paused");
        this.alert.DescriptionText = this.StatDescription(false);
        this.alert.DeclineText = this.Locale.Get(LocaleTextType.Menu, "Main Menu");
        this.alert.AcceptText = this.Locale.Get(LocaleTextType.Continue, "Continue");
        this.alert.Prompt(result =>
        {
            switch (result.result)
            {
                case Alert.ResultType.Declined:
                    AnalyticsManager.Event(() =>
                    {
                        AnalyticsEvent.LevelQuit(
                            this.Context.data.activeWarehouseData.NormalizedName,
                            new Dictionary<string, object>
                            {
                                {"id", this.Context.data.activeWarehouseData.localIdentifier},
                                {"timeMs", this.timeMs},
                                {"moveCount", this.Context.data.RecentMovementHistory.Count}
                            }
                        );
                    });

                    SceneManager.LoadScene(TitleManager.Scene);
                    return;
            }
            this.Context.SetRunning(true);
        }, setInvisible: true);

        // AnalyticsManager.Event(() => AnalyticsEvent.ScreenVisit("gameMenu"));
    }

    private string StatDescription(bool includeBest)
    {
        var builder = new StringBuilder("");

        #region time
        var currentTimeSeconds = this.timeMs / 1000.0f;
        var timeString = this.Locale.Get(LocaleTextType.Time, "Time");
        builder.Append($"{timeString}: {currentTimeSeconds.ToString("F2")} sec");
        var bestString = this.Locale.Get(LocaleTextType.Best, "Best");
        if (includeBest && this.Context.data.activeWarehouseLevelMeta.IsCompleted)
        {
            var previousTime = (this.Context.data.activeWarehouseLevelMeta.bestCompletionMs ?? 0) / 1000.0f;
            if (currentTimeSeconds > previousTime)
            {
                builder.AppendLine($" ({bestString}: {previousTime.ToString("F2")})");
            }
            else
            {
                builder.AppendLine($" ({bestString}!)");
            }
        }
        else
        {
            builder.AppendLine("");
        }
        #endregion

        #region steps
        var currentStepCount = this.Context.data.RecentMovementHistory.Count;
        var stepsString = this.Locale.Get(LocaleTextType.Steps, "Steps");
        builder.Append($"{stepsString}: {currentStepCount}");
        if (includeBest && this.Context.data.activeWarehouseLevelMeta.IsCompleted)
        {
            var previousStepCount = this.Context.data.activeWarehouseLevelMeta.completionMovementHistory.AsNotNull().Count();
            if (previousStepCount > 0 && currentStepCount > previousStepCount)
            {
                builder.AppendLine($" ({bestString}: {previousStepCount})");
            }
            else
            {
                builder.AppendLine($" ({bestString}!)");
            }
        }
        #endregion

        return builder.ToString();
    }

    public void UpdateUi()
    {
        var isNavigationInput = GameContext.IsNavigationEnabled;
        var isStandardInput = !isNavigationInput;

        this.toggleSoundButton?.gameObject?.SetActive(isStandardInput);
        this.restartButton?.gameObject?.SetActive(isStandardInput);
        this.alert.ToggleExtras(restart: isNavigationInput, sound: isNavigationInput);
        this.alert.ToggleDismiss(isStandardInput);
    }
}
