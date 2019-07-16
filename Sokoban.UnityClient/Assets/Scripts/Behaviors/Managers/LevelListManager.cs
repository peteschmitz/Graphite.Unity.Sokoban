using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LevelListManager : BaseBehavior
{
    public static readonly bool IsAllAvailable = false;

    public WarehouseManager.Data.MetaEvent OnLevelSelectedEvent = new WarehouseManager.Data.MetaEvent();

    public GameObject listPanel;
    public GameObject listLevelPrefab;
    public int elementSpacing = 4;
    public Color activeLevelColor;
    public Color completedLevelColor;
    public TitleManager titleManager;
    public GameObject levelHolder;
    public Tabber categoryTabber;
    public IAPManager iapManager;
    public AlertIAP iapAlert;

    private int levelsPerRow;
    private int levelsPerColumn;
    private float levelWidth;
    private float levelHeight;
    private int currentIndex;
    private WarehouseManager.MetaData warehouseMetaData { get; set; }
    private Dictionary<CategoryType, List<List<WarehouseManager.MetaData.LevelMetaData>>> pagedLevelData =
        new Dictionary<CategoryType, List<List<WarehouseManager.MetaData.LevelMetaData>>>();
    private List<List<WarehouseManager.MetaData.LevelMetaData>> activePagedLevelData { get; set; }
    private UiPager pager { get; set; }
    private List<GameObject> levelPrefabs = new List<GameObject>();
    private Vector2 startListingPosition = new Vector2(0, 0);
    private CategoryType activeCategory = CategoryType.Easy;
    private bool isInitialized = false;

    #region properties
    public int LevelsPerPage => this.levelsPerRow * this.levelsPerColumn;
    #endregion

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        this.Recalculate();

        this.audioSource = this.GetComponent<AudioSource>();

        this.pager = this.gameObject.GetComponentInChildren<UiPager>();
        this.pager.OnPageSelectedEvent.AddListener((caller, pageNumber) => this.ListPage(pageNumber));
        this.pager.OnMoveNextEvent.AddListener((uiPager) => this.categoryTabber.ActivateTab(this.activeCategory.GetNext().ToString()));
        this.pager.OnMovePreviousEvent.AddListener((uiPager) => this.categoryTabber.ActivateTab(this.activeCategory.GetPrevious().ToString(), toFirst: false));

        this.categoryTabber.OnTabSelectedEvent.AddListener(this.ChangeCategory);
        this.categoryTabber.ActivateTab(this.activeCategory.ToString());

        this.iapManager?.OnGrantEvent.AddListener((iapGrant, fromLocal) =>
        {
            this.categoryTabber.ActivateTab(this.activeCategory.ToString(), true);
        });
        this.iapAlert.OnVisibilityEvent.AddListener((isVisible) =>
        {
            if (!isVisible && GameContext.IsNavigationEnabled && this.gameObject.activeInHierarchy)
            {
                this.SelectDefault();
            }
        });
    }

    public void ToggleActive(bool visibility)
    {
        this.gameObject.SetActive(visibility);

        if (GameContext.IsNavigationEnabled && isInitialized && visibility)
        {
            this.SelectDefault();
        }
    }

    public void SelectDefault()
    {
        if (this.levelPrefabs.HasItems())
        {
            this.EventSystem.SetSelectedGameObject(this.levelPrefabs.First());
        }
        else
        {
            Debug.LogError("LevelListManager->Invalid selection state");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            this.titleManager?.NavigateTitle();
        }
    }

    private void BuildPages()
    {
        var category = this.activeCategory;
        this.warehouseMetaData = this.warehouseMetaData ?? this.File.Get<WarehouseManager.MetaData>();
        var levels = this.File.GetDirectoryFilePaths<WarehouseManager.Data>(appendedDirectory: category.ToString(), allowCache: true);
        Array.Sort(levels);

        if (!this.pagedLevelData.ContainsKey(category))
        {
            var pagedData = levels.GroupBy(x => Array.IndexOf(levels, x) / this.LevelsPerPage)
                .Select(group =>
                    group.Select(relativePath =>
                    this.warehouseMetaData.levelData.FirstOrDefault(z => z.relativePath == relativePath) ?? new WarehouseManager.MetaData.LevelMetaData
                    {
                        relativePath = relativePath
                    })
                    .ToList())
                .ToList();
            this.pagedLevelData.Add(category, pagedData);
        }
        this.activePagedLevelData = this.pagedLevelData[category];

        //this.warehouseMetaData.levelData = new List<WarehouseManager.MetaData.LevelMetaData>
        //{
        //    new WarehouseManager.MetaData.LevelMetaData
        //    {
        //        relativePath = this.pagedLevelData[0][0].relativePath,
        //        fastestCompletionMs = 30 * 1000,
        //        lastPlayed = DateTime.Now.AddHours(-1)
        //    },
        //    new WarehouseManager.MetaData.LevelMetaData
        //    {
        //        relativePath = this.pagedLevelData[0][1].relativePath,
        //        fastestCompletionMs = 45 * 1000,
        //        lastPlayed = DateTime.Now.AddHours(-0.5)
        //    }
        //};
        //this.warehouseMetaData.levelData[0].SetDataReference(this.pagedLevelData[0][0]);
        //this.warehouseMetaData.levelData[1].SetDataReference(this.pagedLevelData[0][1]);
        //this.File.Save(this.warehouseMetaData);
    }

    private void ListPage(int pageNumber)
    {
        var previousIndex = this.currentIndex;
        var pageIndex = pageNumber - 1;
        this.currentIndex = pageIndex;

        foreach (var prefab in this.levelPrefabs)
        {
            prefab.transform.SetParent(null, false);
        }

        var activeButtons = new List<GameObject>();
        for (var i = 0; i < this.LevelsPerPage && i < this.activePagedLevelData[pageIndex].Count; ++i)
        {
            var prefab = i < this.levelPrefabs.Count ? this.levelPrefabs[i] : null;
            if (prefab == null)
            {
                prefab = Instantiate(this.listLevelPrefab, Vector3.zero, Quaternion.identity);
                this.levelPrefabs.Add(prefab);
            }
            activeButtons.Add(prefab);
            prefab.transform.SetParent(this.levelHolder.transform, false);
            prefab.name = $"levelButton{i + 1}";

            var text = prefab.GetComponentInChildren<Text>();
            var runtimeIndex = pageIndex * this.LevelsPerPage + i;
            text.text = $"#{runtimeIndex + 1}";

            var levelData = this.activePagedLevelData[pageIndex][i];
            var button = prefab.GetComponent<CustomButton>();
            var isPurchased = this.IsPurchased(levelData);

            var isPlayable = false;
            if (IsAllAvailable)
            {
                isPlayable = true;// && isPurchased; // test all purchased levels
            }
            else
            {
                isPlayable = this.IsPlayableLevel(levelData) && isPurchased;
            }

            button.Toggle(isPlayable);
            button.interactable = true;//isPlayable || !isPurchased;

            var levelButton = prefab.GetComponent<LevelButton>();
            levelButton.SetIcon(
                this.activeCategory,
                isPlayable,
                isPurchased,
                levelData.IsCompleted,
                levelData.IsCompleted ? this.completedLevelColor : this.activeLevelColor);

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (isPlayable)
                {

                    var data = this.File.LoadJson<WarehouseManager.Data>(levelData.relativePath);
                    data.runtimeIndex = runtimeIndex;
                    data.runtimeCategory = this.activeCategory;
                    this.PlaySfx();
                    this.OnLevelSelectedEvent.Invoke(data, levelData);
                }
                else if (!isPurchased && iapAlert != null)
                {
                    this.PlaySfx();
                    iapAlert.PromptIap(IAPLevelPak.GetIAPRequirement(levelData.FileName));
                }
                else if (!GameContext.IsNavigationEnabled)
                {
                    this.EventSystem.SetSelectedGameObject(null);
                }
            });

            var row = i / this.levelsPerRow;
            var col = i - row * this.levelsPerRow;
            var rect = prefab.GetComponent<RectTransform>();
            rect.localPosition = rect.localPosition
                .WithX(this.startListingPosition.x + col * this.levelWidth)
                .WithY(this.startListingPosition.y - row * this.levelHeight);
        }

        if (GameContext.IsNavigationEnabled)
        {
            if (!this.isInitialized)
            {
                this.SelectDefault();
            }
            else if (this.EventSystem.currentSelectedGameObject?.activeInHierarchy != true)
            {
                if (activeButtons.HasItems())
                {
                    var selectTarget = previousIndex > this.currentIndex ? activeButtons.First() : activeButtons.Last();
                    this.EventSystem.SetSelectedGameObject(selectTarget);
                }
                else
                {
                    this.SelectDefault();
                }
            }
        }

        this.isInitialized = true;
    }

    private bool IsPurchased(WarehouseManager.MetaData.LevelMetaData metaData)
    {
        var fileName = metaData?.FileName;
        if (this.iapManager == null || fileName.IsInvalid())
        {
            return false;
        }
        var iapKey = IAPLevelPak.GetIAPRequirement(metaData.FileName);
        return iapKey.IsInvalid() || iapManager.HasGrant(iapKey);
    }

    private void Recalculate()
    {
        var prefabTransform = this.listLevelPrefab.GetComponent<RectTransform>();
        this.levelWidth = prefabTransform.sizeDelta.x + this.elementSpacing;
        this.levelHeight = prefabTransform.sizeDelta.y + this.elementSpacing;

        var parentTransform = this.gameObject.GetComponent<RectTransform>();
        this.levelsPerRow = (int)(parentTransform.sizeDelta.x / this.levelWidth) - 1;
        this.levelsPerColumn = 1;// (int)(parentTransform.sizeDelta.y / this.levelHeight) - 1;

        var totalWidth = this.levelWidth * this.levelsPerRow - this.elementSpacing;
        var totalHeight = this.levelHeight * this.levelsPerColumn - this.elementSpacing;
        this.startListingPosition = new Vector2(
            parentTransform.sizeDelta.x * (0.5f - parentTransform.pivot.x) - totalWidth * 0.5f,
            parentTransform.sizeDelta.y * (0.95f - parentTransform.pivot.y)/* + totalHeight * 0.5f*/);
    }

    private bool IsPlayableLevel(WarehouseManager.MetaData.LevelMetaData levelData)
    {
        var levelIndex = this.GetPagedIndex(levelData);
        if (levelIndex.pageIndex > 0)
        {
            return this.activePagedLevelData[levelIndex.page][levelIndex.pageIndex - 1].IsCompleted;
        }
        else if (levelIndex.page > 0)
        {
            return this.activePagedLevelData[levelIndex.page - 1].Last().IsCompleted;
        }
        return true;
    }

    private (int page, int pageIndex) GetPagedIndex(WarehouseManager.MetaData.LevelMetaData levelData)
    {
        for (var page = 0; page < this.activePagedLevelData.Count; ++page)
        {
            for (var pageIndex = 0; pageIndex < this.activePagedLevelData[page].Count; ++pageIndex)
            {
                if (this.activePagedLevelData[page][pageIndex] == levelData)
                {
                    return (page, pageIndex);
                }
            }
        }
        throw new ArgumentException($"Paged level data doesn't contain {levelData?.Identifier}");
    }

    private void ChangeCategory(string categoryName, bool firstPage = true)
    {
        var category = (CategoryType)Enum.Parse(typeof(CategoryType), categoryName);
        Debug.Log("Changed to category: " + category);
        this.activeCategory = category;
        this.BuildPages();
        this.pager.allowMoveNext = category.GetNext() != category;
        this.pager.allowMovePrevious = category.GetPrevious() != category;
        this.pager.SetPages(this.activePagedLevelData.Count);
        this.pager.SetActive(firstPage ? 1 : this.pager.pagesCount, true);
        this.ListPage(this.pager.activePageNumber);
    }
}
