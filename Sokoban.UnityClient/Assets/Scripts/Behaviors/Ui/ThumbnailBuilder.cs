using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ThumbnailBuilder
{
    public const int TileSize = 64;

    private static Dictionary<(Type, int), Texture2D> textureCache = new Dictionary<(Type, int), Texture2D>();
    private static Dictionary<string, Sprite[]> spriteCache = new Dictionary<string, Sprite[]>();

    public float scale = 0.3f;
    public int columnPadding = 2;
    public int rowPadding = 2;
    public int pixelPadding = 2;
    public Color background = new Color(42.0f / 255.0f, 42.0f / 255.0f, 42.0f / 255.0f);

    public Sprite GetSprite(Ground[][] grounds)
    {
        var scaledTileSize = (int)(scale * TileSize);
        var region = this.GetRegion(grounds);
        var texture = new Texture2D(region.Length * scaledTileSize + pixelPadding * 2, region[0].Length * scaledTileSize + pixelPadding * 2);
        texture.requestedMipmapLevel = 0;
        texture.filterMode = FilterMode.Point;
        texture.SetPixels(Enumerable.Repeat(this.background, texture.width * texture.height).ToArray());
        for (var column = 0; column < region.Length; ++column)
        {
            for (var row = 0; row < region.Length; ++row)
            {
                var ground = region[column][row];
                Texture2D scaledTexture = null;
                if (ground?.occupant != null)
                {
                    scaledTexture = this.GetScaledTexture(ground.occupant, scaledTileSize);
                }
                else if (ground != null)
                {
                    scaledTexture = this.GetScaledTexture(ground, scaledTileSize);
                }
                if (scaledTexture != null)
                {
                    texture.SetPixels(pixelPadding + column * scaledTileSize, pixelPadding + row * scaledTileSize, scaledTexture.width, scaledTexture.height, scaledTexture.GetPixels());
                }
            }
        }
        texture.Apply();
        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero, GameContext.PixelPerUnit);
        return sprite;
    }

    private Texture2D GetScaledTexture(BaseItem item, int scaledTileSize)
    {
        var cacheKey = (item.GetType(), scaledTileSize);
        Texture2D copy = null;
        if (!textureCache.TryGetValue(cacheKey, out copy))
        {
            Sprite[] spritesheet = null;
            if (!spriteCache.TryGetValue(item.ThumbnailSheet, out spritesheet))
            {
                spriteCache[item.ThumbnailSheet] = Resources.LoadAll<Sprite>(item.ThumbnailSheet);
                spritesheet = spriteCache[item.ThumbnailSheet];
            }
            var thumbnailSettings = item.Thumbnail;
            var sprite = spritesheet.First(x => x.name == thumbnailSettings.Name);
            textureCache[cacheKey] = TextureScaler.scaled(sprite.texture, sprite.textureRect, scaledTileSize, scaledTileSize, FilterMode.Trilinear);
            copy = textureCache[cacheKey];
        }
        return copy;
    }

    private Ground[][] GetRegion(Ground[][] grounds)
    {
        var region = new Ground[this.columnPadding * 2 + 1][];
        Enumerable.Range(0, region.Length).ForEach(i => region[i] = new Ground[this.rowPadding * 2 + 1]);
        var playerGround = grounds.SelectMany(ground => ground).First(ground => ground?.occupant?.GetType() == typeof(Player));
        var center = playerGround.WarehouseIndex.Value;
        var x = 0;
        for (var column = center.x - columnPadding; column <= center.x + columnPadding; ++column)
        {
            var y = 0;
            for (var row = center.y - rowPadding; row <= center.y + rowPadding; ++row)
            {
                if (column < 0 || column >= grounds.Length || row < 0 || row >= grounds[column].Length)
                {
                    ++y;
                    continue;
                }
                region[x][y] = grounds[column][row];
                ++y;
            }
            ++x;
        }
        return region;
    }
}
