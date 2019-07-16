using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mosaic : MonoBehaviour
{
    public GameObject patternPrefab;
    public float directionDegrees = 45f;
    public float velocity = 1.0f;
    public bool isEnabled = true;

    private List<List<GameObject>> tiles = new List<List<GameObject>>();
    private int patternWidth;
    private int patternHeight;
    private Vector2Int fillCameraSize;
    private GameObject holder;

    #region properties
    private Vector3 Angle =>
        new Vector3(Mathf.Cos(Mathf.Deg2Rad * this.directionDegrees), Mathf.Sin(Mathf.Deg2Rad * this.directionDegrees));
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        var renderer = this.patternPrefab.GetComponent<SpriteRenderer>();
        this.patternWidth = renderer.sprite.texture.width;
        this.patternHeight = renderer.sprite.texture.height;
    }

    // Update is called once per frame
    void Update()
    {
        if (Camera.main == null)
        {
            return;
        }

        if (Camera.main.pixelWidth != this.fillCameraSize.x ||
            Camera.main.pixelHeight != this.fillCameraSize.y)
        {
            if (Camera.main.pixelWidth > this.fillCameraSize.x ||
                Camera.main.pixelHeight > this.fillCameraSize.y)
            {
                this.FillCanvas();
            }
            else
            {
                this.tiles.ForEach((tile, position) => tile.transform.localPosition = this.GetTilePosition(position.x, position.y));
            }
            this.fillCameraSize = new Vector2Int(Camera.main.pixelWidth, Camera.main.pixelHeight);
        }

        this.Animate(Time.deltaTime);
    }

    private void Animate(float elapsedTime)
    {
        if (!this.isEnabled)
        {
            return;
        }
        var scalar = this.Angle * (elapsedTime * this.velocity);
        this.tiles.ForEach((tile, position) =>
        {
            tile.transform.position = tile.transform.position + scalar;
        });
        this.ValidatePositions();
    }

    private void ValidatePositions()
    {
        var minX = float.MaxValue;
        var minY = float.MaxValue;
        var maxX = float.MinValue;
        var maxY = float.MinValue;

        this.tiles.ForEach((tile, position) =>
        {
            minX = Mathf.Min(minX, tile.transform.position.x);
            minY = Mathf.Min(minY, tile.transform.position.y);
            maxX = Mathf.Max(maxX, tile.transform.position.x);
            maxY = Mathf.Max(maxY, tile.transform.position.y);
        });

        var angle = this.Angle;
        var patternWorldWidth = (float)this.patternWidth / GameContext.PixelPerUnit;
        var patternWorldHeight = (float)this.patternHeight / GameContext.PixelPerUnit;
        var camWorldMin = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelRect.xMin, Camera.main.pixelRect.yMin));
        var camWorldMax = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelRect.xMax, Camera.main.pixelRect.yMax));
        this.tiles.ForEach((tile, position) =>
        {
            if (angle.x > 0.0f && tile.transform.position.x - patternWorldWidth * 0.5f > camWorldMax.x)
            {
                tile.transform.position = tile.transform.position.WithX(minX - patternWorldWidth);
            }
            else if (angle.x < 0.0f && tile.transform.position.x + patternWorldWidth * 0.5f < camWorldMin.x)
            {
                tile.transform.position = tile.transform.position.WithX(maxX + patternWorldWidth);
            }

            if (angle.y > 0.0f && tile.transform.position.y - patternWorldHeight * 0.5f > camWorldMax.y)
            {
                tile.transform.position = tile.transform.position.WithY(minY - patternWorldHeight);
            }
            else if (angle.y < 0.0f && tile.transform.position.y + patternWorldHeight * 0.5f < camWorldMin.y)
            {
                tile.transform.position = tile.transform.position.WithY(maxY + patternWorldHeight);
            }
        });
    }

    private void FillCanvas()
    {
        this.holder = this.holder ?? new GameObject("MosaicHolder");
        var bufferCount = 1;
        var intendedWidth = (Camera.main.pixelWidth * Camera.main.orthographicSize) + this.patternWidth * bufferCount;
        var intendedHeight = (Camera.main.pixelHeight * Camera.main.orthographicSize) + this.patternHeight * bufferCount;
        var width = Mathf.Max(intendedWidth, this.tiles.HasItems() ? this.tiles.Count * this.patternWidth : 0);
        var originalHeight = Mathf.Max(intendedHeight, this.tiles.HasItems() ? this.tiles[0].Count * this.patternHeight : 0);
        var column = 0;
        while (width > 0)
        {
            var height = originalHeight;
            var row = 0;
            var newTiles = this.tiles.Count > column ? this.tiles[column] : new List<GameObject>();
            while (height > 0)
            {
                var tile = newTiles.Count > row ? newTiles[row] : Instantiate(this.patternPrefab, Vector3.zero, Quaternion.identity, this.holder.transform);
                tile.transform.localPosition = this.GetTilePosition(column, row);
                height -= this.patternHeight;
                ++row;
                if (!newTiles.Contains(tile))
                {
                    newTiles.Add(tile);
                }
            }
            width -= this.patternWidth;
            ++column;
            if (!this.tiles.Contains(newTiles))
            {
                this.tiles.Add(newTiles);
            }
        }

        this.holder.transform.position = Camera.main.ScreenToWorldPoint(new Vector3(0, 0))
            .WithZ(0.0f);
    }

    private Vector3 GetTilePosition(int column, int row)
    {
        return new Vector3(this.patternWidth * (column + 0.5f), this.patternHeight * (row + 0.5f)) / GameContext.PixelPerUnit;
    }
}
