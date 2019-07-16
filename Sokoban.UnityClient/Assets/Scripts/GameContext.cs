using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

public class GameContext
{
    public static readonly bool IsDebugMode = true;

    public static bool IsNavigationEnabled => _isNavigationEnabled;
    // public static bool IsNavigationEnabled => true;

    public static bool HasRuntimeNavigation = false;
#if UNITY_ANDROID && !UNITY_EDITOR
    private static bool _isNavigationEnabled => HasRuntimeNavigation;
#else
    private static bool _isNavigationEnabled => Application.platform.Equals(RuntimePlatform.tvOS);
#endif


    public class Data : BaseData<Data>
    {
        public class SettingEvent : UnityEvent<string, bool> { }

        public SettingEvent OnSettingChanged = new SettingEvent();
        public bool enableGameplayEditor = false;
#if UNITY_EDITOR
        public bool enableQuickExport = true;
#else
        public bool enableQuickExport = false;
#endif

        public EditorManager.Data activeEditorData = new EditorManager.Data();

        private WarehouseManager.Data _activeWarehouseData;
        public WarehouseManager.Data activeWarehouseData
        {
            get
            {
                return this._activeWarehouseData.MemberwiseClone();
            }
        }

        private WarehouseManager.MetaData.LevelMetaData _activeWarehouseLevelMeta;
        public WarehouseManager.MetaData.LevelMetaData activeWarehouseLevelMeta
        {
            get
            {
                return this._activeWarehouseLevelMeta.MemberwiseClone();
            }
        }

        private bool _isSoundEnabled;
        public bool IsSoundEnabled
        {
            get
            {
                return this._isSoundEnabled;
            }
            set
            {
                this._isSoundEnabled = value;
                this.OnSettingChanged?.Invoke(nameof(this.IsSoundEnabled), value);
            }
        }

        private List<BaseItem.MovementHistory> _recentMovementHistory = new List<BaseItem.MovementHistory>();
        public List<BaseItem.MovementHistory> RecentMovementHistory
        {
            set
            {
                this._recentMovementHistory = value;
            }
            get
            {
                return this._recentMovementHistory.AsNotNull()
                    .Select(x => x.MemberwiseClone())
                    .ToList();
            }
        }

        public void LogMovement(BaseItem item, MovementType movementType, uint timeMs)
        {
            this.LogMovement((BaseItem.MovementHistory)new BaseItem.MovementHistory
            {
                movementType = movementType,
                timeMs = timeMs
            }
            .SetDataReference(item.origin));
        }

        public void LogMovement(BaseItem.MovementHistory movement)
        {
            this._recentMovementHistory.Add(movement);
        }

        public void UpdateLevelMetaData(GameContext.Data contextData, BaseItem.ITimeProvider time)
        {
            this._activeWarehouseLevelMeta.Update(contextData, time);
        }

        public void SetActiveData(GameContext context, WarehouseManager.Data warehouseData, string relativePath = null)
        {
            var previousData = this._activeWarehouseData;
            WarehouseManager.MetaData.LevelMetaData levelMetaData = null;

            //var previousItems = (previousData?.buildItems ?? new List<WarehouseBuildItemRequest>())
            //    .OrderBy(x => x.column).ThenBy(x => x.row)
            //    .Select(x => x.Name)
            //    .ToList();
            //var newItems = (warehouseData?.buildItems ?? new List<WarehouseBuildItemRequest>())
            //    .OrderBy(x => x.column).ThenBy(x => x.row)
            //    .Select(x => x.Name)
            //    .ToList();

            // rotate data guid if there's a change (probably from editor)
            if (previousData?.Identifier == warehouseData.Identifier)
            {
                var comparer = new WarehouseManager.Data.Comparer();
                if (!comparer.Equals(previousData, warehouseData))
                {
                    warehouseData.RotateGuid();
                }
                else
                {
                    levelMetaData = this._activeWarehouseLevelMeta;
                }
            }

            // fallback to  existing meta
            if (levelMetaData == null)
            {
                var metaData = context.file.Get<WarehouseManager.MetaData>();
                levelMetaData = metaData.levelData.FirstOrDefault(x => x.Identifier == warehouseData.Identifier);
            }

            // finally fallback to new
            if (levelMetaData == null)
            {
                levelMetaData = new WarehouseManager.MetaData.LevelMetaData(warehouseData);
            }
            levelMetaData.relativePath = relativePath;

            this._activeWarehouseData = warehouseData;
            this._activeWarehouseLevelMeta = levelMetaData;
            this._recentMovementHistory = new List<BaseItem.MovementHistory>();
            Debug.Log($"Active data set to level localIdentifier: {this._activeWarehouseData?.localIdentifier}");
        }

        public bool MoveNext(GameContext context, CategoryType? categoryOverride = null, IAPManager iapManager = null)
        {
            var levelPath = String.Empty;
            var currentPath = this._activeWarehouseLevelMeta?.relativePath;
            var currentCategory = categoryOverride ?? this._activeWarehouseData?.runtimeCategory ?? CategoryType.Easy;

            // get level list
            var levels = context.file.GetDirectoryFilePaths<WarehouseManager.Data>(appendedDirectory: currentCategory.ToString(), allowCache: true);
            Array.Sort(levels);

            var runtimeIndex = 0;
            if (!currentPath.IsValid() || Array.IndexOf(levels, currentPath) < 0)
            {
                levelPath = levels.First();
            }
            else
            {
                var currentIndex = Array.IndexOf(levels, currentPath);
                runtimeIndex = currentIndex + 1;
                var iapKey = runtimeIndex >= levels.Length ? "" : IAPLevelPak.GetIAPRequirement(levels[runtimeIndex]);
                if (runtimeIndex >= levels.Length || iapKey.IsValid() && !iapManager.HasGrant(iapKey))
                {
                    var nextCategory = currentCategory.GetNext();
                    if (nextCategory.Equals(currentCategory))
                    {
                        runtimeIndex = currentIndex; // we're on the last available level already
                    }
                    else
                    {
                        return this.MoveNext(context, nextCategory); // recursive into next category
                    }
                }
                levelPath = levels[runtimeIndex];
            }

            // move to elected level
            var levelData = context.file.LoadJson<WarehouseManager.Data>(levelPath);
            levelData.runtimeIndex = runtimeIndex;
            levelData.runtimeCategory = currentCategory;
            this.SetActiveData(context, levelData, levelPath);

            // true if we were able to progress
            return levelPath.Equals(currentPath);
        }
    }

    private static GameContext _instance;
    public static GameContext Instance => _instance = _instance ?? new GameContext();

    public const int PixelPerUnit = 128;

    public bool isRunning { get; private set; } = true;
    public Data data { get; private set; } = new Data();
    // public NetworkManager network { get; private set; } = new NetworkManager();
    public FileManager file { get; private set; } = new FileManager();

    #region properties
    public bool IsPaused
    {
        get
        {
            return !this.isRunning;
        }
    }

    private LocaleManager _locale;
    public LocaleManager Locale
    {
        get
        {
            if (this._locale == null || !this._locale.isActiveAndEnabled)
            {
                this._locale = GameObject.FindObjectsOfType<LocaleManager>()
                    .FirstOrDefault(x => x.isActiveAndEnabled);
            }
            return this._locale;
        }
    }
    #endregion

    public GameContext()
    {
        this.data.IsSoundEnabled = true;
        this.data.OnSettingChanged.AddListener((name, value) => Debug.Log($"Setting '{name}' changed to {value}"));
        //this.data.SetActiveData(this, DataTests.BlueTest);
        this.data.SetActiveData(this, DataTests.MarkerTest);
    }

    public void SetRunning(bool isRunning)
    {
        if (this.isRunning == isRunning)
        {
            return;
        }

        this.isRunning = isRunning;
    }

    public void ResetData()
    {
        Debug.Log("Reset data requested");
    }

    #region data tests
    public static class DataTests
    {
        public static WarehouseManager.Data EditorDefault => new WarehouseManager.Data
        {
            rows = EditorManager.DefaultWarehouseSize,
            columns = EditorManager.DefaultWarehouseSize,
            buildItems = new List<WarehouseBuildItemRequest>
            {
                new WarehouseBuildItemRequest
                {
                    column = 1,
                    row = 1,
                    itemType = typeof(Player).ToString()
                }
            }
        };

        public static WarehouseManager.Data BlueTest => new WarehouseManager.Data
        {
            buildItems = new List<WarehouseBuildItemRequest>
            {
                new WarehouseBuildItemRequest
                {
                    column = 1,
                    row = 1,
                    boxType = BoxType.Brown
                },
                new WarehouseBuildItemRequest
                {
                    column = 1,
                    row = 2,
                    boxType = BoxType.Brown
                },
                new WarehouseBuildItemRequest
                {
                    column = 0,
                    row = 2,
                    itemType = typeof(Player).ToString()
                },
                new WarehouseBuildItemRequest
                {
                    column = 1,
                    row = 5,
                    boxType = BoxType.Blue
                },
                new WarehouseBuildItemRequest
                {
                    column = 3,
                    row = 5,
                    boxType = BoxType.Blue
                },
                new WarehouseBuildItemRequest
                {
                    column = 5,
                    row = 5,
                    boxType = BoxType.Blue
                },
                new WarehouseBuildItemRequest
                {
                    column = 7,
                    row = 5,
                    boxType = BoxType.Blue
                },
                new WarehouseBuildItemRequest
                {
                    column = 0,
                    row = 6,
                    boxType = BoxType.Brown
                },
                new WarehouseBuildItemRequest
                {
                    column = 4,
                    row = 3,
                    markerType = BoxType.Brown
                },
                new WarehouseBuildItemRequest
                {
                    column = 8,
                    row = 7,
                    markerType = BoxType.Brown
                },
                new WarehouseBuildItemRequest
                {
                    column = 4,
                    row = 2,
                    boxType = BoxType.Red
                }
            },
            missingGround = new HashSet<WarehouseMissingGround>()
            {
                //new WarehouseMissingGround(2, 3)
            }
        };

        public static WarehouseManager.Data MarkerTest => new WarehouseManager.Data
        {
            localIdentifier = "b1476ade-a391-4562-b086-725b92dfb214",
            rows = 5,
            columns = 3,
            buildItems = new List<WarehouseBuildItemRequest>
            {
                new WarehouseBuildItemRequest
                {
                    column = 1,
                    row = 2,
                    boxType = BoxType.Brown
                },
                new WarehouseBuildItemRequest
                {
                    column = 1,
                    row = 1,
                    itemType = typeof(Player).ToString()
                },
                new WarehouseBuildItemRequest
                {
                    column = 1,
                    row = 3,
                    //boxType = BoxType.Brown
                    markerType = BoxType.Brown
                }
            },
            missingGround = new HashSet<WarehouseMissingGround>()
            {
                new WarehouseMissingGround(2, 3)
            }
        };

    }
    #endregion
}
