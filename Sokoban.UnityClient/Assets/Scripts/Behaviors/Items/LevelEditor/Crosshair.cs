using UnityEngine;

[Prefab(Path = "Prefabs/Items/LevelEditor/CrosshairPrefab")]
[Sprite(Name = "crosshair_1")]
public class Crosshair : BaseItem
{
    protected override float SpriteOffsetY => 0.22f;

    public void SetVisibility(bool isVisible)
    {
        //this.canvasGroup.blocksRaycasts = isVisible;
        this.SpriteRenderer.color = new Color(1.0f, 1.0f, 1.0f, isVisible ? 1.0f : 0.0f);
    }

    public void AnchorTo(GameObject targetObject)
    {
        this.SpriteRenderer.transform.position = targetObject.transform.position
            .AddY((1.0f - Ground.groundHeightUnits) * 0.5f)
            .WithZ(this.SpriteRenderer.transform.position.z);
    }

    public void AnchorTo(Bounds targetBounds)
    {
        this.SpriteRenderer.transform.position = targetBounds.center
            .WithZ(this.SpriteRenderer.transform.position.z);
    }
}
