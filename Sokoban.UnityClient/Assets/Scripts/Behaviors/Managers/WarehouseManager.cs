using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class WarehouseManager : BaseBehavior, BaseItem.IMovementValidation
{
    public class Event : UnityEvent<WarehouseManager> { }

    /// <summary>
    /// Maximum width or height of any warehouse
    /// </summary>
    public const int MaxSize = 10;

    public BaseItem.Event OnGroundSelectedEvent = new BaseItem.Event();
    public BaseItem.Event OnGroundUnselectedEvent = new BaseItem.Event();
    public BaseItem.Event OnItemAddedEvent = new BaseItem.Event();
    public BaseItem.Event OnItemRemovedEvent = new BaseItem.Event();
    public Grid.Event OnGridSelectedEvent = new Grid.Event();
    public Grid.Event OnGridUnselectedEvent = new Grid.Event();
    public Ground.OccupantEvent OnOccupantSelectedEvent = new Ground.OccupantEvent();
    public Event OnInitializedEvent = new Event();
    public Event OnAnimatedInEvent = new Event();

    public int defaultColumns = 6;
    public int defaultRows = 4;
    public bool enableGroundEvents = false;
    public bool enableOccupantEvents = false;
    public bool enableGrid = false;
    public bool enableDebugOverlay = false;

    public BaseItem.ITimeProvider timeProvider { get; set; }

    private Ground[][] grounds => this.activeData?.grounds;
    private List<BaseItem> activeItems => this.activeData?.activeItems;
    private Texture2D renderCanvas { get; set; }
    private Grid grid { get; set; }
    private GameObject itemHolder { get; set; }
    private GameObject groundHolder { get; set; }
    private Data activeData { get; set; }
    private MetaData.LevelMetaData activeLevelMeta { get; set; }
    private Action animateFinishCallback;
    private Dictionary<BaseItem, bool> animateItemsLookup { get; set; }

    #region properties
    public Data ActiveData
    {
        get
        {
            return this.activeData.MemberwiseClone();
        }
    }

    // public MetaData.LevelMetaData ActiveLevelMeta
    // {
    //     get
    //     {
    //         return this.activeLevelMeta;
    //     }
    // }

    public DataBundle ActiveDataBundle
    {
        get
        {
            var bundle = new DataBundle
            {
                levelData = this.ActiveData,
                levelMetaData = this.Context.data.activeWarehouseLevelMeta// this.ActiveLevelMeta
            };
            if (bundle?.levelMetaData?.completionMovementHistory != null)
            {
                bundle.levelData.completionMovementHistory = bundle.levelMetaData.completionMovementHistory.ToList();
            }
            return bundle;
        }
    }

    public Bounds WorldBounds
    {
        get
        {
            var bound = new Bounds();
            var bounds = this.grounds.ForEach((ground, position) => ground?.WorldBounds)
                .WithNonNull()
                .ToList();
            bounds.ForEach(x => bound.Encapsulate(x));
            return bound;
        }
    }

    public bool HasMovingItems => this.activeItems.Any(x => x?.isMoving == true);

    public int MarkerCount
    {
        get
        {
            return this.grounds.ForEach((ground, position) =>
            {
                return ground?.OccupantAsMarker != null;
            })
            .Count(x => x);
        }
    }

    public int MatchedMarkerCount
    {
        get
        {
            return this.grounds.ForEach((ground, position) =>
            {
                return ground?.OccupantAsMarker != null &&
                    ground.occupant != null &&
                    ground.occupant.GetType().IsAssignableFrom(typeof(Box)) &&
                    ((Box)ground.occupant).boxType == ground.OccupantAsMarker.markerType;
            })
            .Count(x => x);
        }
    }

    public bool HasMarkerWinCondition
    {
        get
        {
            return this.MarkerCount == this.MatchedMarkerCount;
        }
    }

    #endregion

    // Start is called before the first frame update
    async protected override void Start()
    {
        base.Start();
        this.BuildWarehouse();

        var renderer = this.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            var thumb = new ThumbnailBuilder { scale = 0.5f };
            var sprite = thumb.GetSprite(this.grounds);
            renderer.sprite = sprite;
            renderer.sortingLayerName = "Ui";
        }

        this.OnInitializedEvent.Invoke(this);

        await this.AnimateItems(() =>
        {
            this.OnAnimatedInEvent.Invoke(this);
        },
        intro: true);
    }

    public void BuildWarehouse()
    {
        this.activeData = this.Context.data.activeWarehouseData;

        if (this.itemHolder == null)
        {
            this.itemHolder = new GameObject("WarehouseHolder");
        }
        if (this.groundHolder == null)
        {
            this.groundHolder = new GameObject("GroundHolder");
            this.groundHolder.transform.parent = this.itemHolder.transform;
        }

        this.BuildGrounds();
        this.BuildItems();
        if (this.enableGrid)
        {
            this.BuildGrid();
        }
    }

    public bool PlaceItem(Sprite sprite, Ground ground) =>
        this.PlaceItem(sprite, ground.WarehouseIndex.Value.x, ground.WarehouseIndex.Value.y);
    public bool PlaceItem(Sprite sprite, int column, int row) =>
        this.BuildItem(WarehouseBuildItemRequest.FromSprite(sprite, column, row), true);
    public bool PlaceItem(BaseItem item, Ground ground) =>
        this.PlaceItem(item, ground.WarehouseIndex.Value.x, ground.WarehouseIndex.Value.y);
    public bool PlaceItem(BaseItem item, int column, int row)
    {
        if (!this.HasGround(column, row))
        {
            return false;
        }
        var ground = this.grounds[column][row];
        ground.SetOccupant(item);
        this.OnItemAddedEvent.Invoke(item);
        return true;
    }

    private bool HasGround(Vector2Int? position) => position.HasValue && this.HasGround(position.Value.x, position.Value.y);
    private bool HasGround(int column, int row)
    {
        return this.activeData.HasGround(column, row);
        // return this.grounds != null &&
        //     column >= 0 && this.grounds.Length > column &&
        //     row >= 0 && this.grounds[column].Length > row &&
        //     this.grounds[column][row] != null;
    }

    private bool HasOccupant(Vector2Int? position) => position.HasValue && this.HasOccupant(position.Value.x, position.Value.y);
    private bool HasOccupant(int column, int row)
    {
        if (!this.HasGround(column, row))
        {
            throw new ArgumentException($"Cant determine occupant, ground doesn't exist at ({column},{row})");
        }
        return this.grounds[column][row]?.occupant != null;
    }

    private BaseItem GetOccupant(Vector2Int? position) => !position.HasValue ? null : this.GetOccupant(position.Value.x, position.Value.y);
    private BaseItem GetOccupant(int column, int row)
    {
        if (!this.HasOccupant(column, row))
        {
            return null;
        }
        return this.grounds[column][row].occupant;
    }

    public BaseItem GetInboundOccupant(Vector2Int? position)
    {
        return !position.HasValue ? null : this.activeItems.FirstOrDefault(x => x.warehouseDestination?.WarehouseIndex == position);
    }

    private void BuildGrounds()
    {
        if (this.activeData.columns == 0 && this.activeData.rows == 0)
        {
            if (this.activeData.missingGround.HasItems() || this.activeData.buildItems.HasItems())
            {
                this.activeData.columns = Math.Max(
                    this.activeData.missingGround.HasItems() ? this.activeData.missingGround.Max(x => x.x) + 1 : 0,
                    this.activeData.buildItems.HasItems() ? this.activeData.buildItems.Max(x => x.column) + 1 : 0);
                this.activeData.rows = Math.Max(
                    this.activeData.missingGround.HasItems() ? this.activeData.missingGround.Max(x => x.y) + 1 : 0,
                    this.activeData.buildItems.HasItems() ? this.activeData.buildItems.Max(x => x.row) + 1 : 0);
            }
            else
            {
                this.activeData.columns = this.defaultColumns;
                this.activeData.rows = this.defaultRows;
            }
        }

        this.activeData.grounds = new Ground[this.activeData.columns][];
        for (var column = 0; column < this.activeData.columns; ++column)
        {
            this.grounds[column] = new Ground[this.activeData.rows];
            for (var row = 0; row < this.activeData.rows; ++row)
            {
                if (this.activeData.missingGround == null || !this.activeData.missingGround.Contains(new WarehouseMissingGround(column, row)))
                {
                    this.AddGround(column, row);
                }
            }
        }
        this.CalculateMissingGround();
    }

    private void CalculateMissingGround()
    {
        this.activeData?.CalculateMissingGround();
    }

    public Ground AddGround(Vector2Int relativePosition, bool allowResize = false) =>
        this.AddGround(relativePosition.x, relativePosition.y, allowResize);
    public Ground AddGround(int column, int row, bool allowResize = false)
    {
        if (allowResize && (column < 0 || row < 0 || column >= this.grounds.Length || row >= this.grounds[0].Length))
        {
            this.activeData.GrowGrounds(column, row);
            this.activeData.Validate();
            column = column < 0 ? 0 : column;
            row = row < 0 ? 0 : row;
        }

        var prefabReference = this.File.GetPrefab<Ground>();
        var groundPosition = new Vector3(Ground.groundWidthUnits * column, Ground.groundHeightUnits * row);
        var ground = Instantiate(prefabReference, groundPosition, Quaternion.identity, this.groundHolder.transform).GetComponent<Ground>();
        ground.WarehouseIndex = new Vector2Int(column, row);
        ground.OnOccupantAddedEvent.AddListener(this.OnOccupantAdded);
        ground.OnOccupantRemovedEvent.AddListener(this.OnOccupantRemoved);
        if (this.enableGroundEvents)
        {
            ground.ToggleInput(true);
            // ground.OnMouseDownEvent.AddListener(this.OnGroundSelected);
            // ground.OnMouseDownEvent.AddListener(this.OnGroundSelected);
            ground.OnMouseUpEvent.AddListener(this.OnGroundSelected);
            // ground.OnMouseUpEvent.AddListener((owner) => this.OnGroundUnselectedEvent.Invoke(owner as Ground));
        }
        this.grounds[column][row] = ground;
        if (allowResize)
        {
            this.CalculateMissingGround();
        }
        return ground;
    }

    public void OnGroundSelected(BaseItem ground)
    {
        this.OnGroundSelectedEvent.Invoke(ground as Ground);
    }

    public async Task AnimateItems(Action animateFinishedCallback, bool intro = false)
    {
        Func<BaseItem, int> orderBy = (item) =>
        {
            if (intro)
            {
                return item.GetType().Equals(typeof(Ground)) ? 0 : 1; // ground first on intro
            }
            else
            {
                return item.GetType().Equals(typeof(Ground)) ? 1 : 0; // ground last on outro
            }
        };

        this.animateFinishCallback = animateFinishedCallback;
        this.animateItemsLookup = this.grounds
            .ForEach((ground, position) => new BaseItem[] { ground, ground?.occupant, ground?.passiveOccupant })
            .SelectMany(x => x)
            .Where(x => x != null)
            .Distinct()
            .OrderBy(x => orderBy(x))
            .ToDictionary(x => x, x => false);

        var items = this.animateItemsLookup.Keys.ToList();
        // intro : start state is invisible
        if (intro)
        {
            items.ForEach(x => x.SetInvisible());
        }

        var itemDelay = TimeSpan.FromMilliseconds(intro ? 15 : 25);
        foreach (var item in items)
        {
            await Task.Delay(itemDelay);
            if (intro)
            {
                item.SetVisible();
            }
            item.AnimateItem((animationEvent) => this.OnAnimateFinished(item), intro);
        }
    }

    private void OnAnimateFinished(BaseItem item)
    {
        if (this.animateItemsLookup.ContainsKey(item))
        {
            this.animateItemsLookup[item] = true;
        }
        var remainingCount = this.animateItemsLookup.Count(x => !x.Value);
        //Debug.Log($"Animation finished: {item.gameObject.name}, remaining {remainingCount}");
        if (remainingCount == 0 && this.animateFinishCallback != null)
        {
            this.animateFinishCallback.Invoke();
            this.animateFinishCallback = null;
        }
    }

    private void OnOccupantRemoved(Ground individualGround = null, BaseItem occupant = null)
    {
        this.activeData.ValidateActiveItems(individualGround, occupant);
        if (occupant != null)
        {
            this.OnItemRemovedEvent.Invoke(occupant);
        }
    }

    private void OnOccupantAdded(Ground individualGround = null, BaseItem occupant = null)
    {
        this.activeData.ValidateBuildItems();
    }

    public void RemoveGround(Ground ground)
    {
        if (ground == null)
        {
            return;
        }
        this.grounds[ground.WarehouseIndex.Value.x][ground.WarehouseIndex.Value.y] = null;
        ground.Remove();
        this.CondenseGrounds();
        this.CalculateMissingGround();
    }

    private void CondenseGrounds()
    {
        var frontValidColumn = this.grounds.FirstOrDefault(x => !x.All(y => y == null));
        var frontColumnIndex = frontValidColumn == null ? 0 : Array.IndexOf(this.grounds, frontValidColumn);
        if (frontColumnIndex >= 0)
        {
            var condenseColumns = frontColumnIndex;
            var condenseRows = 0;
            for (var i = 0; i < this.grounds[0].Length; ++i)
            {
                if (!this.grounds.All(x => x[i] == null))
                {
                    break;
                }
                ++condenseRows;
            }
            if (condenseColumns > 0 || condenseRows > 0)
            {
                this.activeData.ResizeGrounds(-condenseColumns, 0, -condenseRows, 0);
                this.grid.SetSize(this.grounds.Length, this.grounds[0].Length, -condenseColumns, -condenseRows);
                this.groundHolder.transform.position = this.groundHolder.transform.position
                    .WithX(this.grid.gridOffset.x * Ground.groundWidthUnits)
                    .WithY(this.grid.gridOffset.y * Ground.groundHeightUnits);
                this.activeData.Validate();
            }
        }

        var backValidColumn = this.grounds.LastOrDefault(x => !x.All(y => y == null));
        var backColumnIndex = backValidColumn == null ? 0 : Array.IndexOf(this.grounds, backValidColumn);
        if (backColumnIndex <= this.grounds.Length - 1)
        {
            var condenseColumns = (this.grounds.Length - 1) - backColumnIndex;
            var condenseRows = 0;
            for (var i = this.grounds[0].Length - 1; i >= 0; --i)
            {
                if (!this.grounds.All(x => x[i] == null))
                {
                    break;
                }
                ++condenseRows;
            }
            if (condenseColumns > 0 || condenseRows > 0)
            {
                this.activeData.ResizeGrounds(0, -condenseColumns, 0, -condenseRows);
                this.grid.SetSize(this.grounds.Length, this.grounds[0].Length/*, -frontColumnPadding, -frontRowPadding*/);
                this.groundHolder.transform.position = this.groundHolder.transform.position
                    .WithX(this.grid.gridOffset.x * Ground.groundWidthUnits)
                    .WithY(this.grid.gridOffset.y * Ground.groundHeightUnits);
                this.activeData.Validate();
            }
        }
    }

    public void BuildItems()
    {
        if (this.activeData?.buildItems == null)
        {
            return;
        }
        foreach (var item in this.activeData.buildItems)
        {
            this.BuildItem(item);
        }
    }

    public bool BuildItem(WarehouseBuildItemRequest item, bool removeExisting = false)
    {
        if (!this.HasGround(item.column, item.row) || this.HasOccupant(item.column, item.row) && !removeExisting)
        {
            return false;
        }

        BaseItem newItem = null;
        if (item.boxType != null)
        {
            var prefabReference = this.File.GetPrefab<Box>();
            var newObject = Instantiate(prefabReference, this.itemHolder.transform);
            newItem = newObject.GetComponent<Box>();
            (newItem as Box).boxType = item.boxType.Value;
        }
        else if (item.markerType != null)
        {
            var prefabReference = this.File.GetPrefab<Marker>();
            var newObject = Instantiate(prefabReference, this.itemHolder.transform);
            newItem = newObject.GetComponent<Marker>();
            (newItem as Marker).markerType = item.markerType.Value;
        }
        else if (item.itemType != null)
        {
            var prefabReference = this.File.GetPrefab(Type.GetType(item.itemType));
            var newObject = Instantiate(prefabReference, this.itemHolder.transform);
            newItem = newObject.GetComponent<BaseItem>();
        }

        if (newItem != null)
        {
            newItem.validation = this;
            newItem.timeProvider = this.timeProvider;
            newItem.origin = item;
            if (this.enableOccupantEvents)
            {
                newItem.ToggleInput(true);
                // newItem.OnMouseDownEvent.AddListener((owner) => this.OnOccupantSelectedEvent.Invoke(owner.parent as Ground, newItem));
                newItem.OnMouseUpEvent.AddListener((owner) => this.OnOccupantSelectedEvent.Invoke(owner.parent as Ground, newItem));
            }
            return this.PlaceItem(newItem, item.column, item.row);
        }

        return false;
    }

    private void OnGUI()
    {
        if (!this.enableDebugOverlay || this.grounds == null || this.grounds[0] == null)
        {
            return;
        }
        var offsetY = Ground.groundHeightUnits * 0.6f;
        this.grounds.ForEach((ground, position) =>
        {
            if (ground != null)
            {
                var worldX = ground.transform.position.x;
                var worldY = ground.transform.position.y + offsetY;
                var text = $"({position.x}, {position.y})";
                DebugDraw.DrawWorldText(worldX, worldY, text);
            }
        });
    }

    public void BuildGrid()
    {
        var gridObject = Instantiate(this.File.GetPrefab<Grid>(), this.itemHolder.transform);
        this.grid = gridObject.GetComponent<Grid>();
        this.grid.SetSize(this.grounds.Length, this.grounds[0].Length);
        // this.grid.OnMouseDownEvent.AddListener((grid, vector) => this.OnGridSelectedEvent.Invoke(grid, vector));
        this.grid.OnMouseUpEvent.AddListener((grid, vector) => this.OnGridSelectedEvent.Invoke(grid, vector));

        // make the grid appear below active ground/elements
        this.grid.gameObject.transform.position = this.grid.gameObject.transform.position.WithZ(this.gameObject.transform.position.z + 1);
    }

    public T GetByOccupantType<T>() where T : BaseItem => this.GetByOccupantTypes<T>().FirstOrDefault();
    public List<T> GetByOccupantTypes<T>()
        where T : BaseItem
    {
        return this.GetGroundByOccupantTypes<T>()
            .Select(x => x.occupant)
            .Cast<T>()
            .ToList();
    }

    public Ground GetGround(Vector2Int? position) => position.HasValue ? this.GetGround(position.Value.x, position.Value.y) : null;
    public Ground GetGround(int column, int row)
    {
        return this.HasGround(column, row) ? this.grounds[column][row] : null;
    }

    public Ground GetGroundByOccupantType<T>() where T : BaseItem => this.GetGroundByOccupantTypes<T>().FirstOrDefault();
    public List<Ground> GetGroundByOccupantTypes<T>()
        where T : BaseItem
    {
        return this.grounds.AsNotNull()
            .SelectMany(x => x.AsNotNull().Where(y => y != null && y.HasOccupant && y.occupant.GetType() == typeof(T)))
            .ToList();
    }

    public bool IsTraversable(Vector2Int destination, MovementType movementType, int pushStrength = 1)
    {
        if (!this.HasGround(destination))
        {
            return false;
        }
        var direction = movementType.GetAngle();

        if (!this.HasOccupant(destination) ||
            this.GetOccupant(destination).IsPassiveOccupant ||
            this.GetOccupant(destination).isMoving ||
            this.GetOccupant(destination).GetType() == typeof(Player))
        {
            return true;
        }

        var origin = destination - direction.AsVector2Int();
        pushStrength = Mathf.Max(
            pushStrength,
            this.GetOccupant(origin)?.PushStrength ?? 0,
            this.GetInboundOccupant(origin)?.PushStrength ?? 0);
        --pushStrength;

        if (this.GetOccupant(destination).IsPushable &&
            pushStrength >= 0 &&
            this.IsTraversable(destination + direction.AsVector2Int(), movementType, pushStrength))
        {
            return true;
        }

        return false;
    }

    public List<T> GetItems<T>() where T : BaseItem
    {
        return this.activeItems.Where(x => x.GetType() == typeof(T))
            .Cast<T>()
            .ToList();
    }

    public void Clear(Data setData = null)
    {
        this.Context.data.SetActiveData(this.Context, setData ?? GameContext.DataTests.EditorDefault);
        SceneManager.LoadScene(EditorManager.Scene);
    }

    private struct PlacementScore
    {
        public bool canPlace { get; set; }
        public double score { get; set; }

        public static PlacementScore CalculateScore(List<List<bool>> currentPlacements, Vector2Int position)
        {
            if (currentPlacements[position.x][position.y])
            {
                return new PlacementScore
                {
                    canPlace = false
                };
            }
            var neighborScore = 2.0d;
            var neighborCount = 0;
            var score = 0.0d;
            var neighborIndicies = new Vector2Int[]{
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
                new Vector2Int(-1, 0),
                new Vector2Int(1, 0)
            };
            neighborIndicies.ForEach(index =>
            {
                var neighborIndex = position + index;
                if (currentPlacements.HasIndex(neighborIndex) && currentPlacements[neighborIndex.x][neighborIndex.y])
                {
                    score += neighborScore * ((neighborIndicies.Length - neighborCount) / neighborIndicies.Length);
                    ++neighborCount;
                }
            });
            if (score > 0.0d)
            {
                var startX = (int)(MaxSize * 0.5f);
                var startY = (int)(MaxSize * 0.5f);
                var distance = Math.Abs(startX - position.x) + Math.Abs(startY - position.y);
                if (distance > 0)
                {
                    score *= 1 + (distance / MaxSize);
                }
            }
            return new PlacementScore
            {
                canPlace = true,
                score = score
            };
        }

        public static Vector2Int? ChoosePlacement(Dictionary<Vector2Int, PlacementScore> placementOptions, System.Random random)
        {
            var placeables = placementOptions.Where(x => x.Value.canPlace).ToList();
            if (!placeables.HasItems())
            {
                return null;
            }
            var totalScore = placeables.Sum(x => x.Value.score);
            var winningScore = random.NextDouble() * totalScore;
            foreach (var placeable in placeables)
            {
                winningScore -= placeable.Value.score;
                if (winningScore <= 0)
                {
                    return placeable.Key;
                }
            }
            return placeables.Last().Key;
        }
    }

    public Data GenerateFloor(int floorCount)
    {
        var data = new Data();
        data.buildItems = new List<WarehouseBuildItemRequest>();

        var newGrounds = Enumerable.Repeat(MaxSize, MaxSize)
            .Select(x => Enumerable.Repeat(false, x).ToList())
            .ToList();

        // Start with middle
        newGrounds[(int)(MaxSize * 0.5f)][(int)(MaxSize * 0.5f)] = true;

        var placementScores = new Dictionary<Vector2Int, PlacementScore>();
        var randSeed = (int)(DateTime.UtcNow - DateTime.UtcNow.Date).TotalMilliseconds;
        Debug.Log("seed: " + randSeed);
        var rand = new System.Random(randSeed);
        var skipCount = 0;
        for (var i = 1; i < floorCount; ++i)
        {
            newGrounds.ForEach((val, position) =>
            {
                placementScores[position] = PlacementScore.CalculateScore(newGrounds, position);
            });
            var newPlacementPosition = PlacementScore.ChoosePlacement(placementScores, rand);
            if (newPlacementPosition == null)
            {
                ++skipCount;
                continue;
            }
            newGrounds[newPlacementPosition.Value.x][newPlacementPosition.Value.y] = true;
        }
        if (skipCount > 0)
        {
            Debug.Log($"WarehouseManager.GenerateFloor-> Skipped {skipCount} attempts.");
        }
        // newGrounds[2][2] = false;
        // newGrounds[1][1] = true;
        // newGrounds[2][1] = true;

        newGrounds = newGrounds.Where(x => x.Any(y => y)).ToList();
        if (newGrounds.HasItems())
        {
            var count = newGrounds[0].Count - 1;
            for (var i = count; i >= 0; --i)
            {
                if (newGrounds.All(x => !x[i]))
                {
                    newGrounds.ForEach(x => x.RemoveAt(i));
                }
            }
        }
        data.columns = newGrounds.Count;
        data.rows = newGrounds[0].Count;

        data.missingGround = newGrounds.ForEach((ground, position) =>
            {
                return ground ? null : new WarehouseMissingGround(position.x, position.y);
            })
            .WithNonNull()
            .ToHashSet();
        return data;
    }

    [RelativePath(Directory = "Data/Bundles/", FileName = "level")]
    public class DataBundle
    {
        public Data levelData { get; set; }

        public MetaData.LevelMetaData levelMetaData { get; set; }

        [JsonIgnore]
        public string PostfixString
        {
            get
            {
                var moveCount = this.levelMetaData?.completionMovementHistory?.Count ?? 0;
                var buildCount = this.levelData?.buildItems?.Count ?? 0;
                var columnCount = this.levelData?.columns ?? 0;
                var rowCount = this.levelData?.rows ?? 0;
                var groundCount = (rowCount) * (columnCount) - this.levelData.missingGround.AsNotNull().Count();
                return $"ground-{groundCount}" +
                    $"_moves-{moveCount}" +
                    $"_items-{buildCount}" +
                    $"_cols-{columnCount}" +
                    $"_rows-{rowCount}";
            }
        }

    }

    [RelativePath(Directory = "Data/Levels/", FileName = "level", IsResource = true)]
    public class Data : BaseData<Data>
    {
        public class MetaEvent : UnityEvent<Data, MetaData.LevelMetaData> { }

        public List<WarehouseBuildItemRequest> buildItems { get; set; }
        public HashSet<WarehouseMissingGround> missingGround { get; set; }
        public int columns { get; set; }
        public int rows { get; set; }
        public string author { get; set; }
        public string authorDeviceIdentifier { get; set; }
        public List<BaseItem.MovementHistory> completionMovementHistory { get; set; }

        [JsonIgnore]
        public int runtimeIndex { get; set; } = -1;
        [JsonIgnore]
        public CategoryType? runtimeCategory { get; set; }
        [JsonIgnore]
        public Ground[][] grounds { get; set; }
        [JsonIgnore]
        public List<BaseItem> activeItems { get; set; } = new List<BaseItem>();

        #region properties
        [JsonIgnore]
        public string DisplayName
        {
            get
            {
                if (this.runtimeIndex >= 0)
                {
                    var categoryName = !this.runtimeCategory.HasValue ? "" : GameContext.Instance.Locale.Get(this.runtimeCategory.Value.ToString());
                    return $"{categoryName}: {this.runtimeIndex + 1}";
                }
                else
                {
                    return GameContext.Instance.Locale.Get(LocaleTextType.CustomLevel);
                }
            }
        }
        [JsonIgnore]
        public string NormalizedName
        {
            get
            {
                if (this.runtimeIndex >= 0)
                {
                    var categoryName = !this.runtimeCategory.HasValue ? "" : this.runtimeCategory.Value.ToString();
                    return $"{categoryName}: {this.runtimeIndex + 1}";
                }
                else
                {
                    return GameContext.Instance.Locale.Get(LocaleTextType.CustomLevel);
                }
            }
        }
        #endregion

        public Data() : base()
        {
            this.authorDeviceIdentifier = this.GetIdentifier();
        }

        public void CalculateMissingGround()
        {
            this.missingGround = this.grounds.ForEach((ground, position) =>
            {
                return this.HasGround(position) ? null : new WarehouseMissingGround(position.x, position.y);
            })
            .WithNonNull()
            .ToHashSet();
        }

        public bool HasGround(Vector2Int? position) => position.HasValue && this.HasGround(position.Value.x, position.Value.y);
        public bool HasGround(int column, int row)
        {
            return this.grounds != null &&
                column >= 0 && this.grounds.Length > column &&
                row >= 0 && this.grounds[column].Length > row &&
                this.grounds[column][row] != null;
        }



        public void GrowGrounds(int columns, int rows)
        {
            var frontColumnPadding = -Math.Min(0, columns);
            var frontRowPadding = -Math.Min(0, rows);
            var backColumnPadding = Math.Max(this.grounds.Length - 1, columns) - (this.grounds.Length - 1);
            var backRowPadding = Math.Max(this.grounds[0].Length - 1, rows) - (this.grounds[0].Length - 1);
            this.ResizeGrounds(frontColumnPadding, backColumnPadding, frontRowPadding, backRowPadding);
        }

        public void ResizeGrounds(int frontColumnPadding, int backColumnPadding, int frontRowPadding, int backRowPadding)
        {
            var newGrounds = new Ground[this.grounds.Length + frontColumnPadding + backColumnPadding][];
            Enumerable.Range(0, newGrounds.Length).ForEach(x => newGrounds[x] = new Ground[this.grounds[0].Length + frontRowPadding + backRowPadding]);
            Debug.Log($"Modifying grounds array (Front: ({frontColumnPadding}, {frontRowPadding}), Back: ({backColumnPadding}, {backRowPadding})");

            this.grounds.ForEach((ground, position) =>
            {
                var newX = position.x + frontColumnPadding;
                var newY = position.y + frontRowPadding;
                if (newX < 0 || newY < 0 || newX >= newGrounds.Length || newY >= newGrounds[0].Length)
                {
                    if (ground != null)
                    {
                        throw new ArgumentException("Ground needs to be disposed before truncated out of grid.");
                    }
                    return;
                }
                newGrounds[position.x + frontColumnPadding][position.y + frontRowPadding] = this.grounds[position.x][position.y];
            });

            this.grounds = newGrounds;
            // this.grid.SetSize(this.grounds.Length, this.grounds[0].Length, -frontColumnPadding, -frontRowPadding);
            // this.groundHolder.transform.position = this.groundHolder.transform.position
            //     .WithX(this.grid.gridOffset.x * Ground.groundWidthUnits)
            //     .WithY(this.grid.gridOffset.y * Ground.groundHeightUnits);

            this.columns = this.grounds.Length;
            this.rows = this.grounds[0].Length;
        }

        public void Validate()
        {
            this.ValidateIndicies();
            this.ValidateBuildItems();
        }


        public void ValidateIndicies()
        {
            this.grounds.ForEach((ground, position) => ground != null ? ground.WarehouseIndex = position : null);
        }

        public void ValidateBuildItems(Ground individualGround = null, BaseItem occupant = null)
        {
            if (individualGround != null)
            {
                var buildItems = this.buildItems
                    .Where(x => x.Position != individualGround.WarehouseIndex)
                    .ToList();

                var buildItem = individualGround.AsBuildItem;
                if (buildItem != null)
                {
                    buildItems.Add(buildItem);
                }

                this.buildItems = buildItems.ToList();
            }
            else
            {
                this.buildItems = this.grounds.ForEach((ground, position) => ground?.AsBuildItem)
                    .WithNonNull()
                    .ToList();
            }

            this.ValidateActiveItems(individualGround, occupant);
        }

        public void ValidateActiveItems(Ground individualGround = null, BaseItem occupant = null)
        {
            if (occupant != null)
            {
                // invokee was likely from a removed occupant
                if (individualGround.occupant != occupant)
                {
                    this.activeItems.Remove(occupant);
                    var occupantBuildItem = occupant.AsBuildItem;
                    this.buildItems.Remove(occupantBuildItem);
                }
                else if (!this.activeItems.Contains(occupant))
                {
                    this.activeItems.Add(occupant);
                }
            }
            else
            {
                this.activeItems = this.grounds.ForEach((ground, position) => ground?.occupant)
                    .WithNonNull()
                    .ToList();
            }
        }

        public static Data FromJson(string exportData)
        {
            //return null;
            return JsonConvert.DeserializeObject<Data>(exportData);
        }

        public new Data MemberwiseClone()
        {
            var data = base.MemberwiseClone();
            data.buildItems = data.buildItems.AsNotNull()
                .Select(x => x.MemberwiseClone())
                .ToList();
            data.missingGround = data.missingGround.AsNotNull()
                .Select(x => x.MemberwiseClone())
                .ToHashSet();
            return data;
        }

        public class Comparer : IEqualityComparer<Data>
        {
            public bool Equals(Data dataOne, Data dataTwo)
            {
                if (dataOne == null && dataTwo == null)
                {
                    return true;
                }
                else if (dataOne == null || dataTwo == null)
                {
                    return false;
                }
                var buildItemEquality = Enumerable.SequenceEqual(
                    dataOne.buildItems.AsNotNull().OrderBy(x => x.row).ThenBy(x => x.column),
                    dataTwo.buildItems.AsNotNull().OrderBy(x => x.row).ThenBy(x => x.column),
                    new WarehouseBuildItemRequest.Comparer());
                var missingGroundEquality = Enumerable.SequenceEqual(
                    dataOne.missingGround.AsNotNull().OrderBy(x => x.x).ThenBy(x => x.y),
                    dataTwo.missingGround.AsNotNull());
                return buildItemEquality && missingGroundEquality;
            }

            public int GetHashCode(Data obj)
            {
                return obj?.buildItems?.GetHashCode() ?? -1;
            }
        }
    }

    [RelativePath(Directory = "Data/Meta/", FileName = "warehouse")]
    public class MetaData : BaseData<MetaData>
    {
        public List<LevelMetaData> levelData { get; set; }
        public UserSettings userSettings { get; set; }
        public UserData userData { get; set; }

        public MetaData()
        {
            this.levelData = new List<LevelMetaData>();
            this.userData = new UserData();
            this.userSettings = new UserSettings();
        }

        public class UserSettings : BaseData<UserSettings>
        {
            public List<String> shownNotices { get; set; }
        }

        public class UserData : BaseData<UserData>
        {
            public string authToken { get; set; }
            public string id { get; set; }
            public string generatedName { get; set; }
        }

        public class LevelMetaData : BaseData<LevelMetaData>
        {
            public DateTime? createdAtDate { get; set; }
            public DateTime? lastPlayedDate { get; set; }
            public uint? bestCompletionMs { get; set; }
            public DateTime? bestCompletionDate { get; set; }
            public DateTime? firstCompletionDate { get; set; }
            public string relativePath { get; set; }
            public List<BaseItem.MovementHistory> completionMovementHistory { get; set; }

            #region properties
            [JsonIgnore]
            public bool IsCompleted => this.firstCompletionDate.HasValue;

            [JsonIgnore]
            public string FileName => this.relativePath.AsFileName();
            #endregion

            public LevelMetaData() : this(null) { }
            public LevelMetaData(Data levelData) : base()
            {
                if (levelData != null)
                {
                    this.SetDataReference(levelData);
                    this.createdAtDate = DateTime.Now;
                }
            }

            public void Update(GameContext.Data contextData, BaseItem.ITimeProvider time)
            {
                this.lastPlayedDate = DateTime.Now;
                if (!this.firstCompletionDate.HasValue)
                {
                    this.firstCompletionDate = this.lastPlayedDate;
                }
                if (!this.bestCompletionMs.HasValue || this.bestCompletionMs.Value > time.TimeMs)
                {
                    this.bestCompletionDate = this.lastPlayedDate;
                    this.bestCompletionMs = time.TimeMs;
                    this.completionMovementHistory = contextData.RecentMovementHistory;
                }
            }
        }
    }
}

public class WarehouseMissingGround : SimpleBaseData<WarehouseMissingGround>
{
    public int x { get; set; }
    public int y { get; set; }

    public WarehouseMissingGround(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public override bool Equals(object obj)
    {
        var other = (WarehouseMissingGround)obj;
        return other != null && other.x == this.x && other.y == this.y;
    }

    public override int GetHashCode()
    {
        return this.x.GetHashCode() + this.y.GetHashCode();
    }
}

public class WarehouseBuildItemRequest : SimpleBaseData<WarehouseBuildItemRequest>
{
    public int column { get; set; }
    public int row { get; set; }
    public BoxType? boxType { get; set; }
    public BoxType? markerType { get; set; }
    public string itemType { get; set; }

    #region properties
    [JsonIgnore]
    public Vector2Int Position => new Vector2Int(this.column, this.row);

    [JsonIgnore]
    public string Name => $"{(this.boxType?.ToString() ?? this.markerType?.ToString()).OnNullOrEmpty(this.itemType)} ({this.column}, {this.row})";
    #endregion

    public static WarehouseBuildItemRequest FromSprite(Sprite sprite, Vector2Int position) =>
        FromSprite(sprite, position.x, position.y);
    public static WarehouseBuildItemRequest FromSprite(Sprite sprite, int column, int row)
    {
        var item = new WarehouseBuildItemRequest
        {
            column = column,
            row = row
        };
        var behavior = AttributeExtensions.GetAssemblyTypes<SpriteAttribute>()
            .FirstOrDefault(x =>
                x.attribute.Name == sprite.name ||
                x.attribute.NamePattern.IsValid() && sprite.name.Contains(x.attribute.NamePattern))
            .type;
        if (behavior != null)
        {
            item.itemType = behavior.ToString();
            return item;
        }
        else
        {
            // let's try to match on enum-types
            var enumSprites = AttributeExtensions.GetAssemblyEnumMembers<SpriteAttribute>();
            var enumMatch = enumSprites.FirstOrDefault(x => x.matches.AsNotNull().Any(y => y.attributes.AsNotNull().Any(z => (z as SpriteAttribute)?.Name == sprite.name)));
            if (enumMatch.matches.HasItems())
            {
                var enumType = enumMatch.enumType;
                var (enumName, attributes) = enumMatch.matches.First(x => x.attributes.Any(y => (y as SpriteAttribute)?.Name == sprite.name));
                var spriteAttr = (SpriteAttribute)attributes.First(x => (x as SpriteAttribute).Name == sprite.name);
                switch (spriteAttr.Key)
                {
                    case Marker.DefaultSpriteKey:
                        item.markerType = (BoxType)Enum.Parse(enumType, enumName);
                        break;
                    case Box.DefaultSpriteKey:
                        item.boxType = (BoxType)Enum.Parse(enumType, enumName);
                        break;
                    default:
                        throw new NotImplementedException($"Can't resolve PlaceItem from '{spriteAttr.Key}' (sprite: {sprite?.name}).");
                }
                return item;
            }
        }

        throw new ArgumentException($"Couldn't PlaceItem for sprite '{sprite?.name}'.");
    }

    public class Comparer : IEqualityComparer<WarehouseBuildItemRequest>
    {
        public bool Equals(WarehouseBuildItemRequest x, WarehouseBuildItemRequest y)
        {
            if (x == null && y == null)
            {
                return true;
            }
            else if (x == null || y == null)
            {
                return false;
            }
            return x.column == y.column &&
                x.row == y.row &&
                x.boxType == y.boxType &&
                x.markerType == y.markerType &&
                x.itemType == y.itemType;
        }

        public int GetHashCode(WarehouseBuildItemRequest obj)
        {
            return obj?.GetHashCode() ?? -1;
        }
    }
}
