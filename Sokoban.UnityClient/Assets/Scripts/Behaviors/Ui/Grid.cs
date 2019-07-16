using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[Prefab(Path = "Prefabs/Items/LevelEditor/GridPrefab")]
[RequireComponent(typeof(SpriteRenderer))]
public class Grid : BaseBehavior, IPointerDownHandler, IPointerUpHandler, PanController.IConstraintProvider
{
    public class Event : UnityEvent<Grid, Vector2Int> { }

    public static readonly TimeSpan MinimumInputSpan = TimeSpan.FromMilliseconds(300);

    public Event OnMouseDownEvent = new Event();
    public Event OnMouseUpEvent = new Event();

    public int boxBufferWidth = 4;
    public int boxBufferHeight = 4;
    public int borderSize = 2;
    public int columns = 6;
    public int rows = 4;
    public Color borderColor = new Color(0x4f / 255f, 0x45 / 255f, 0x3d / 255f);

    public Vector2Int gridOffset { get; private set; } = new Vector2Int();

    private SpriteRenderer spriteRenderer { get; set; }
    private Texture2D texture { get; set; }
    private Sprite sprite { get; set; }
    private BoxCollider2D boxCollider { get; set; }
    private Vector2Int centerOrigin { get; set; }
    private int truncatedColumns { get; set; }
    private int truncatedRows { get; set; }
    private DateTime lastInput { get; set; }

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        var cameraController = FindObjectOfType<PanController>();
        if (cameraController != null)
        {
            cameraController.constraintProvider = this;
        }

        this.spriteRenderer = this.GetComponent<SpriteRenderer>();
        this.boxCollider = this.GetComponent<BoxCollider2D>();
        this.BuildGrid();
    }

    public void SetSize(int columns, int rows, int originColumnPadding = 0, int originRowPadding = 0)
    {
        this.columns = columns;
        this.rows = rows;
        this.gridOffset = this.gridOffset.AddX(originColumnPadding).AddY(originRowPadding);
        if (this.spriteRenderer == null)
        {
            return;
        }
        this.BuildGrid();
    }

    public Bounds GetConstraints()
    {
        if (this.sprite == null)
        {
            return new Bounds();
        }
        var bounds = this.sprite.bounds;
        bounds.center = bounds.center + this.transform.localPosition;
        return bounds;
    }

    private void BuildGrid()
    {
        this.truncatedColumns = 0;
        this.truncatedRows = 0;

        var boxWidth = GameContext.PixelPerUnit;
        var boxHeight = (int)(Ground.groundHeightUnits * GameContext.PixelPerUnit);
        var requestedColumns = this.columns + this.boxBufferWidth * 2;
        if (requestedColumns > WarehouseManager.MaxSize)
        {
            this.truncatedColumns = requestedColumns - WarehouseManager.MaxSize;
            requestedColumns -= this.truncatedColumns;
        }
        var requestedRows = this.rows + this.boxBufferHeight * 2;
        if (requestedRows > WarehouseManager.MaxSize)
        {
            this.truncatedRows = requestedRows - WarehouseManager.MaxSize;
            requestedRows -= this.truncatedRows;
        }
        this.texture = new Texture2D(boxWidth * requestedColumns, boxHeight * requestedRows);
        this.texture.requestedMipmapLevel = 0;
        this.texture.filterMode = FilterMode.Point;
        this.sprite = Sprite.Create(this.texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero, GameContext.PixelPerUnit);
        this.spriteRenderer.sprite = this.sprite;
        for (var x = 0; x < requestedColumns + 1; ++x)
        {
            var pixelsX = (x == requestedColumns ? texture.width - this.borderSize : x * boxWidth);
            var pixelsHeight = boxHeight * requestedRows;
            var colors = Enumerable.Repeat(this.borderColor, this.borderSize * pixelsHeight).ToArray();
            this.texture.SetPixels(pixelsX, 0, this.borderSize, pixelsHeight, colors);
        }
        for (var y = 0; y < requestedRows + 1; ++y)
        {
            var pixelsY = (y == requestedRows ? texture.height - this.borderSize : y * boxHeight);
            var pixelsWidth = boxWidth * requestedColumns;
            var colors = Enumerable.Repeat(this.borderColor, this.borderSize * pixelsWidth).ToArray();
            this.texture.SetPixels(0, pixelsY, pixelsWidth, this.borderSize, colors);
        }
        this.texture.Apply(true);
        this.boxCollider.size = this.sprite.bounds.size;
        this.boxCollider.offset = new Vector2(this.boxCollider.size.x * 0.5f, this.boxCollider.size.y * 0.5f);
        this.CenterOn(requestedColumns, requestedRows);
    }

    public void CenterOn(int columns, int rows)
    {
        var groundOffset = new Vector2(-Ground.groundWidthUnits * 0.5f, -(1.0f - Ground.groundHeightUnits) * 0.5f);
        var centerColumn = (int)Math.Max(0, Math.Ceiling(columns * 0.5f));
        var centerRow = (int)Math.Max(0, Math.Ceiling(rows * 0.5f));

        if (this.centerOrigin.x == 0 && this.centerOrigin.y == 0)
        {
            this.centerOrigin = new Vector2Int(centerColumn, centerRow);
        }

        var truncatedColumnAdjustment = this.truncatedColumns > 0 ? (int)((WarehouseManager.MaxSize - this.columns) * 0.5f) : this.boxBufferWidth;
        var truncatedRowAdjustment = this.truncatedRows > 0 ? (int)((WarehouseManager.MaxSize - this.rows) * 0.5f) : this.boxBufferHeight;
        this.transform.position = this.transform.position
            .WithX(groundOffset.x + (this.gridOffset.x - truncatedColumnAdjustment) * Ground.groundWidthUnits)
            .WithY(groundOffset.y + (this.gridOffset.y - truncatedRowAdjustment) * Ground.groundHeightUnits);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        this.lastInput = DateTime.UtcNow;
        // var groundOffset = new Vector2(-Ground.groundWidthUnits * 0.5f, -(1.0f - Ground.groundHeightUnits) * 0.5f);
        // var boxWidth = Ground.groundWidthUnits;
        // var boxHeight = Ground.groundHeightUnits;
        // var gridPosition = eventData.pointerCurrentRaycast.worldPosition
        //     .AddX(groundOffset.x)
        //     .AddY(groundOffset.y);
        // var gridColumn = (int)(Math.Ceiling(gridPosition.x / boxWidth)) - this.gridOffset.x;
        // var gridRowFloat = (gridPosition.y - boxHeight * 0.5f) / boxHeight;
        // var gridRow = (int)(Math.Ceiling((gridPosition.y - boxHeight * 0.5f) / boxHeight)) - this.gridOffset.y;
        // this.OnMouseDownEvent.Invoke(this, new Vector2Int(gridColumn, gridRow));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (DateTime.UtcNow - this.lastInput > MinimumInputSpan)
        {
            return;
        }

        var groundOffset = new Vector2(-Ground.groundWidthUnits * 0.5f, -(1.0f - Ground.groundHeightUnits) * 0.5f);
        var boxWidth = Ground.groundWidthUnits;
        var boxHeight = Ground.groundHeightUnits;
        var gridPosition = eventData.pointerCurrentRaycast.worldPosition
            .AddX(groundOffset.x)
            .AddY(groundOffset.y);
        var gridColumn = (int)(Math.Ceiling(gridPosition.x / boxWidth)) - this.gridOffset.x;
        var gridRowFloat = (gridPosition.y - boxHeight * 0.5f) / boxHeight;
        var gridRow = (int)(Math.Ceiling((gridPosition.y - boxHeight * 0.5f) / boxHeight)) - this.gridOffset.y;
        // this.OnMouseDownEvent.Invoke(this, new Vector2Int(gridColumn, gridRow));
        this.OnMouseUpEvent.Invoke(this, new Vector2Int(gridColumn, gridRow));
    }

    public Bounds GetBounds(Vector2Int gridPosition)
    {
        return new Bounds(
            center: new Vector3
            {
                x = (gridPosition.x + this.gridOffset.x) * Ground.groundWidthUnits,
                y = (gridPosition.y + this.gridOffset.y) * Ground.groundHeightUnits + (1.0f - Ground.groundHeightUnits) * 0.5f
            },
            size: new Vector3
            {
                x = Ground.groundWidthUnits,
                y = Ground.groundHeightUnits
            });
    }
}
