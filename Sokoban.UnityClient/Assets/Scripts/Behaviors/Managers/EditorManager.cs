using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EditorManager : BaseBehavior
{
    public class Data
    {
        public int floorCount;
        public int grayCount;
        public int brownCount;
        public int blueCount;
        public string loadName;
    }

    public const string Scene = "EditorScene";
    public const int DefaultWarehouseSize = 4;

    public UiSpritePanel popupPanel;
    //public UiSpritePanel actionPanel;
    public Alert alert;
    //public CustomButton exportButton;
    public CustomButton playButton;
    public CustomButton resetButton;
    public CustomButton saveButton;
    public CustomButton menuButton;
    public CustomButton generateItemsButton;
    public CustomButton generateFloorButton;
    public InputField blueBoxInput;
    public InputField brownBoxInput;
    public InputField grayBoxInput;
    public InputField floorCountInput;
    // public UnityEngine.UI.Text
    // public CustomButton loadButton;
    public Dropdown loadDropdown;

    private Crosshair crosshair { get; set; }
    private WarehouseManager warehouseManager { get; set; }
    private Ground activeGround { get; set; }
    private Vector2Int? activeGridPosition { get; set; }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        this.crosshair = FindObjectOfType<Crosshair>();
        this.warehouseManager = FindObjectOfType<WarehouseManager>();
        // alert will hide once initialized
        this.alert.ToggleActive(true);

        this.popupPanel.OnSpriteSelectedEvent.AddListener(this.OnPanelSelection);
        //this.actionPanel.OnNameSelectedEvent.AddListener(this.OnPanelSelection);
        this.warehouseManager.OnGroundSelectedEvent.AddListener(this.OnGroundSelected);
        this.warehouseManager.OnGroundUnselectedEvent.AddListener(this.OnGroundUnselected);
        this.warehouseManager.OnOccupantSelectedEvent.AddListener(this.OnOccupantSelected);
        this.warehouseManager.OnGridSelectedEvent.AddListener(this.OnGridSelection);



        playButton.onClick.AddListener(() => this.PlayActiveLevel());
        resetButton.onClick.AddListener(() => this.PromptReset());
        saveButton.onClick.AddListener(() => this.ExportActive());
        menuButton.onClick.AddListener(() => this.PrompMenu());

        generateItemsButton.onClick.AddListener(() => this.GenerateItems());
        generateFloorButton.onClick.AddListener(() => this.GenerateFloor());
        //loadButton.onClick.AddListener(() => Debug.Log("EditorManager-> Load clicked"));
        //this.exportButton.gameObject.SetActive(this.Context.data.enableQuickExport);
        //if (this.exportButton.gameObject.activeSelf)
        //{
        //    this.exportButton.onClick.AddListener(() => this.ExportActive());
        //}

        var files = new List<string> { "-" }
            .Concat(this.File.GetDirectoryFilePaths<WarehouseManager.Data>(allowCache: false))
            .ToList();
        //foreach (var type in Enum.GetValues(typeof(CategoryType)))
        //{
        //    files.AddRange(this.File.GetDirectoryFilePaths<WarehouseManager.Data>(appendedDirectory: type.ToString(), allowCache: false));
        //}
        this.loadDropdown.AddOptions(files.Select(x => new Dropdown.OptionData(x)).ToList());
        this.loadDropdown.onValueChanged.AddListener((newVal) => this.LoadAndReplace(this.loadDropdown.options[newVal].text));

        this.SetActiveGridPosition(null);
        this.SetActiveGround(null);

        this.Context.SetRunning(true);
        this.SetSettings(this.Context.data.activeEditorData);
    }

    private void LoadAndReplace(string relativePath)
    {
        if (!String.IsNullOrEmpty(this.Context.data?.activeEditorData?.loadName))
        {
            if (this.Context.data.activeEditorData.loadName.Equals(relativePath))
            {
                return;
            }
        }
        this.SaveSettings();
        var data = this.File.LoadJson<WarehouseManager.Data>(relativePath);
        this.warehouseManager.Clear(data);
    }

    private void OnGridSelection(Grid grid, Vector2Int gridPosition)
    {
        // deselect on double-tap
        if (this.activeGridPosition != null && this.activeGridPosition.Value == gridPosition)
        {
            this.SetActiveGridPosition(null);
        }
        else
        {
            this.SetActiveGridPosition(grid, gridPosition);
        }
        this.SetActiveGround(null);
    }

    private void OnPanelSelection(UiSpritePanel panel, Sprite sprite) => this.OnPanelSelection(panel, sprite, null);
    private void OnPanelSelection(UiSpritePanel panel, string buttonText) => this.OnPanelSelection(panel, null, buttonText);
    private void OnPanelSelection(UiSpritePanel panel, Sprite sprite, string buttonText)
    {
        //if (!string.IsNullOrEmpty(buttonText))
        //{
        //    if (buttonText == this.Context.Text.E_Play)
        //    {
        //        this.PlayActiveLevel();
        //    }
        //    else if (buttonText == this.Context.Text.E_ExportJson)
        //    {
        //        var result = this.File.Save(this.warehouseManager.ActiveData, true, "", false);
        //    }
        //    else if (buttonText == this.Context.Text.E_Reset)
        //    {
        //        this.PromptReset();
        //    }
        //    else if (buttonText == this.Context.Text.E_Save)
        //    {
        //        this.PromptSave();
        //    }
        //    else if (buttonText == this.Context.Text.E_Menu)
        //    {
        //        Debug.Log("Menu");
        //    }
        //    return;
        //}

        if (sprite?.name == typeof(Ground).GetCustomAttribute<SpriteAttribute>().Name)
        {
            if (this.activeGround?.occupant != null)
            {
                this.activeGround.SetOccupant(null);
            }
            else if (this.activeGridPosition.HasValue)
            {
                this.warehouseManager.AddGround(this.activeGridPosition.Value, true);
            }
            this.SetActiveGridPosition(null);
            this.SetActiveGround(null);
            return;
        }
        else if (sprite?.name == typeof(EditorDelete).GetCustomAttribute<SpriteAttribute>().Name)
        {
            if (this.activeGround != null)
            {
                this.warehouseManager.RemoveGround(this.activeGround);
            }
            this.SetActiveGridPosition(null);
            this.SetActiveGround(null);
            return;
        }
        else if (sprite?.name == typeof(Player).GetCustomAttribute<SpriteAttribute>().Name)
        {
            // we only support a single player
            var existingPlayers = this.warehouseManager.GetGroundByOccupantTypes<Player>();
            foreach (var existingPlayer in existingPlayers)
            {
                existingPlayer.SetOccupant(null);
            }
        }

        if (this.activeGround != null)
        {
            this.warehouseManager.PlaceItem(sprite, this.activeGround);
        }
        if (this.activeGridPosition.HasValue)
        {
            var newGround = this.warehouseManager.AddGround(this.activeGridPosition.Value, true);
            this.warehouseManager.PlaceItem(sprite, newGround);
        }
        this.SetActiveGridPosition(null);
        this.SetActiveGround(null);
    }

    private void PlayActiveLevel()
    {
        if (this.warehouseManager.ActiveData?.buildItems.AsNotNull().FirstOrDefault(x => x?.itemType != null && x.itemType.Equals("Player")) == null)
        {
            Debug.Log("EditorManager.PlayActiveLevel-> Player item required");
            return;
        }
        this.Context.data.SetActiveData(this.Context, this.warehouseManager.ActiveData);
        this.Context.data.enableGameplayEditor = true;
        SceneManager.LoadScene(GameplayManager.Scene);
    }

    private void PromptReset()
    {
        this.alert.SetDefault();
        this.alert.TitleText = "New Level?";
        this.alert.DescriptionText = "This will clear all items currently in the editor.";
        this.alert.Prompt(result => result.WithAccepted(() =>
        {
            this.warehouseManager.Clear();
        }));
    }

    private void PrompMenu()
    {
        this.alert.SetDefault();
        this.alert.TitleText = "Exit Editor?";
        this.alert.DescriptionText = "Any unsaved progress will be lost.";
        this.alert.Prompt(result => result.WithAccepted(() =>
        {
            SceneManager.LoadScene(TitleManager.Scene);
        }));
    }

    private void SetSettings(Data data)
    {
        floorCountInput.text = data.floorCount.ToString();
        blueBoxInput.text = data.blueCount.ToString();
        brownBoxInput.text = data.brownCount.ToString();
        grayBoxInput.text = data.grayCount.ToString();
        if (data.loadName.IsValid())
        {
            var item = this.loadDropdown.options.FirstOrDefault(x => x.text.Equals(data.loadName));
            if (item != null)
            {
                this.loadDropdown.value = this.loadDropdown.options.IndexOf(item);
            }
        }
    }

    private void SaveSettings()
    {
        var data = this.Context.data.activeEditorData;
        if (!String.IsNullOrEmpty(floorCountInput?.text))
        {
            data.floorCount = int.Parse(floorCountInput.text);
        }
        if (!String.IsNullOrEmpty(blueBoxInput?.text))
        {
            data.blueCount = int.Parse(blueBoxInput.text);
        }
        if (!String.IsNullOrEmpty(brownBoxInput?.text))
        {
            data.brownCount = int.Parse(brownBoxInput.text);
        }
        if (!String.IsNullOrEmpty(grayBoxInput?.text))
        {
            data.grayCount = int.Parse(grayBoxInput.text);
        }
        if (this.loadDropdown.value > 0)
        {
            data.loadName = this.loadDropdown.options.ElementAt(this.loadDropdown.value).text;
        }
    }

    private void GenerateFloor()
    {
        if (String.IsNullOrEmpty(floorCountInput?.text))
        {
            return;
        }
        this.SaveSettings();

        var floorCount = int.Parse(floorCountInput.text);
        var generation = this.warehouseManager.GenerateFloor(floorCount);

        this.warehouseManager.Clear(generation);

        Debug.Log($"Generate floor - Count: {floorCount}");
    }

    private void GenerateItems()
    {
        this.SaveSettings();

        var blueBoxCount = blueBoxInput.text;
        var brownBoxCount = brownBoxInput.text;
        var grayBoxCount = grayBoxInput.text;

        Debug.Log($"Generate items - blue: {blueBoxCount}, brown: {brownBoxCount}, gray: {grayBoxCount} ");
    }

    private void PromptSave()
    {
        var comparer = new WarehouseManager.Data.Comparer();
        var isDataSame = comparer.Equals(this.Context.data.activeWarehouseData, this.warehouseManager.ActiveData);
        var isLevelCompleted = isDataSame && this.Context.data.activeWarehouseLevelMeta.firstCompletionDate.HasValue;

        this.alert.SetDefault();
        this.alert.TitleText = isLevelCompleted ? "Upload" : "Completion Required";
        this.alert.DescriptionText = isLevelCompleted ? "Would you like to upload this level so others may play?" : "You must complete this level before uploading.";
        this.alert.AcceptText = isLevelCompleted ? "Upload & Save" : "Play Now";
        this.alert.DeclineText = "Nevermind";
        this.alert.Prompt(result =>
        {
            switch (result.result)
            {
                case Alert.ResultType.Accepted:
                    if (isLevelCompleted)
                    {
                        this.SaveActive();
                        this.UploadActive();
                    }
                    else
                    {
                        this.PlayActiveLevel();
                    }
                    return;
            }
        });
    }

    private void ClearActive()
    {
        this.SetActiveGridPosition(null);
        this.SetActiveGround(null);
        this.warehouseManager.Clear();
    }

    private void ExportActive()
    {
        var bundle = this.warehouseManager.ActiveDataBundle;
        var result = this.File.Save(bundle.levelData, true, bundle.PostfixString, false);
        if (result.IsValid())
        {
            this.loadDropdown?.AddOptions(new List<Dropdown.OptionData> { new Dropdown.OptionData(result.Replace(".json", "")) });
        }
        //var result = this.File.SaveJson(this.warehouseManager.ActiveData, "Data/Levels/level", true, bundle.PostfixString, false);
    }

    private void ExportActiveBundle()
    {
        var bundle = this.warehouseManager.ActiveDataBundle;
        var result = this.File.Save(bundle, true, bundle.PostfixString, false);
    }

    private void UploadActive()
    {

    }

    private void SaveActive()
    {

    }

    private void OnOccupantSelected(Ground ground, BaseItem occupant) => this.OnGroundSelected(ground);
    private void OnGroundSelected(BaseItem item)
    {
        var ground = item as Ground;

        // deselect on double-tap
        if (ground != null && this.activeGround == ground)
        {
            this.SetActiveGround(null);
        }
        else
        {
            this.SetActiveGround(ground);
        }
        this.SetActiveGridPosition(null);
    }

    private void OnGroundUnselected(BaseItem item)
    {
        //this.SetActiveGround(null);
    }

    private void SetActiveGround(Ground ground)
    {
        this.activeGround = ground;
        this.UpdatePanel();
    }

    private void SetActiveGridPosition(Grid grid, Vector2Int? gridPosition = null)
    {
        Debug.Log($"Selected grid position {gridPosition?.ToString()}");
        this.activeGridPosition = gridPosition;
        this.UpdatePanel(grid);
    }

    private void UpdatePanel(Grid grid = null)
    {
        if (this.activeGround != null)
        {
            this.popupPanel.AnchorTo(this.activeGround);
            this.crosshair.AnchorTo(this.activeGround.gameObject);
        }
        if (this.activeGridPosition.HasValue && grid != null)
        {
            var bounds = grid.GetBounds(this.activeGridPosition.Value);
            this.popupPanel.AnchorTo(bounds);
            this.crosshair.AnchorTo(bounds);
        }

        var isVisible = this.activeGround != null || this.activeGridPosition.HasValue;
        this.popupPanel.SetVisibility(isVisible);
        this.crosshair.SetVisibility(isVisible);
    }
}
