using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

[Prefab(Path = "Prefabs/Items/Gameplay/GroundPrefab2")]
[Sprite(Name = "ground")]
[Thumbnail(Name = "thumbnail_ground")]
public class Ground : BaseItem
{
    public class OccupantEvent : UnityEvent<Ground, BaseItem> { }
    public class MovementEvent : UnityEvent<Ground, BaseItem, MovementType> { }

    public const float groundWidthUnits = 1.0f;
    public const float groundHeightUnits = 0.64f;

    public OccupantEvent OnOccupantAddedEvent = new OccupantEvent();
    public OccupantEvent OnOccupantRemovedEvent = new OccupantEvent();

    public BaseItem occupant { get; private set; }
    public BaseItem passiveOccupant { get; private set; }

    protected override float InputHeightOverride => groundHeightUnits;
    protected override string OutAnimation => "PulseOut";

    #region properties
    public bool HasOccupant => this.occupant != null;

    public Marker OccupantAsMarker
    {
        get
        {
            // Debug.Log($"Occupant at {this.WarehouseIndex.ToString()} is {this.occupant?.ToString()} (passive is {this.passiveOccupant?.ToString()})");
            if (this.occupant != null && this.occupant.GetType().IsAssignableFrom(typeof(Marker)))
            {
                return (Marker)this.occupant;
            }
            else if (this.passiveOccupant != null && this.passiveOccupant.GetType().IsAssignableFrom(typeof(Marker)))
            {
                return (Marker)this.passiveOccupant;
            }
            return null;
        }
    }

    public new Vector2Int? WarehouseIndex
    {
        get
        {
            return base.WarehouseIndex;
        }
        set
        {
            base.WarehouseIndex = value;
            var x = value.HasValue ? value.Value.x : -1;
            var y = value.HasValue ? value.Value.y : -1;
            this.transform.localPosition = new Vector3(groundWidthUnits * x, groundHeightUnits * y);
            this.SpriteRenderer.sortingOrder = this.ZSort;
            this.occupant?.SetParent(this);
        }
    }

    public new WarehouseBuildItemRequest AsBuildItem
    {
        get
        {
            if (this.occupant == null)
            {
                return default;
            }

            // preferably we use origin so the guid isn't recreated
            var buildItem = this.occupant.origin ?? WarehouseBuildItemRequest.FromSprite(this.occupant.SpriteRenderer.sprite, this.WarehouseIndex.Value);
            buildItem.column = this.occupant.WarehouseIndex.HasValue ? this.occupant.WarehouseIndex.Value.x : -1;
            buildItem.row = this.occupant.WarehouseIndex.HasValue ? this.occupant.WarehouseIndex.Value.y : -1;
            return buildItem;
        }
    }
    #endregion

    public void SetOccupant(BaseItem item, bool removePrevious = true)
    {
        var previousOccupant = this.occupant;
        if (previousOccupant != null && previousOccupant.isMoving)
        {
            previousOccupant.ArriveDestination();
        }

        if (item?.parent != null && ((Ground)item.parent).occupant == item)
        {
            ((Ground)item.parent).SetOccupant(null, false);
        }

        // Debug.Log($"Set {this.WarehouseIndex.ToString()} to {item?.ToString()}, previous is {previousOccupant?.ToString()} (passive: {previousOccupant?.IsPassiveOccupant})");
        this.occupant = item;
        this.occupant?.SetParent(this);

        if (previousOccupant != null && !removePrevious && previousOccupant.IsPassiveOccupant)
        {
            // Debug.Log($"Passive {this.WarehouseIndex.ToString()} set to {previousOccupant.ToString()}");
            this.passiveOccupant = previousOccupant;
        }

        if (item != null)
        {
            this.OnOccupantAddedEvent.Invoke(this, item);
        }
        else if (this.passiveOccupant != null && !removePrevious)
        {
            var passive = this.passiveOccupant;
            // this.passiveOccupant = null;
            this.occupant = passive;
            this.occupant.SetParent(this);
            // Debug.Log($"Set {this.WarehouseIndex.ToString()} to {passive.ToString()}");
        }

        if (previousOccupant != null && removePrevious)
        {
            this.OnOccupantRemovedEvent.Invoke(this, previousOccupant);
            previousOccupant.Remove();
        }
    }

    public override void Remove()
    {
        this.OnOccupantAddedEvent.RemoveAllListeners();
        this.OnOccupantRemovedEvent.RemoveAllListeners();
        if (this.HasOccupant)
        {
            this.occupant.Remove();
        }
        this.WarehouseIndex = new Vector2Int(-1, -1);
        base.Remove();
    }
}
