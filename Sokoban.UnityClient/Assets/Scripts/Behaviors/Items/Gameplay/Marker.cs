using System.Linq;

[Prefab(Path = "Prefabs/Items/Gameplay/MarkerPrefab2")]
public class Marker : BaseItem
{
    public const string DefaultSpriteKey = "marker";

    public BoxType markerType = BoxType.Brown;

    #region properties
    protected override string SpriteName => this.markerType.GetCustomTypeAttributes<SpriteAttribute>()
        .FirstOrDefault(x => x.Key == Marker.DefaultSpriteKey)
        .Name;
    protected override string SpriteKey => DefaultSpriteKey;
    protected override float SpriteOffsetY => 0.22f;
    public override bool IsPassiveOccupant => true;
    public override ThumbnailAttribute Thumbnail => this.markerType.GetCustomTypeAttributes<ThumbnailAttribute>()
        .FirstOrDefault(x => x.Key == Marker.DefaultSpriteKey);
    public override int SortAdjustment => -1;
    #endregion
}
