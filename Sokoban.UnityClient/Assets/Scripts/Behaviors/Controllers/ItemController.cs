using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemController : BaseBehavior
{
    public GameObject controlPrefabType;

    private Type controlItemType { get; set; }
    private WarehouseManager warehouseManager { get; set; }
    private List<BaseItem> items { get; set; } = new List<BaseItem>();
    private Dictionary<MovementType, bool> movementCharges = new Dictionary<MovementType, bool>();

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        this.movementCharges = Enum.GetValues(typeof(MovementType))
            .Cast<MovementType>()
            .ToDictionary(x => x, x => true);
        this.controlItemType = this.controlPrefabType?.GetComponent<BaseItem>()?.GetType();
        this.warehouseManager = FindObjectOfType<WarehouseManager>();
        this.warehouseManager.OnItemAddedEvent.AddListener(this.OnItemAdded);
        this.warehouseManager.OnItemRemovedEvent.AddListener(this.OnItemRemoved);
    }

    // Update is called once per frame
    void Update()
    {
        if (this.Context == null || !this.Context.isRunning || this.controlItemType == null || !this.items.HasItems())
        {
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        int horizontalInt = horizontal >= 1.0f ? 1 : (horizontal <= -1.0f ? -1 : 0);
        int verticalInt = vertical >= 1.0f ? 1 : (vertical <= -1.0f ? -1 : 0);
        if (horizontalInt != 0 || verticalInt != 0)
        {
            var movementDirection = new Vector2Int(horizontalInt, verticalInt);
            var direction = movementDirection.AsMovementType();
            if (!direction.HasValue)
            {
                this.ResetCharges();
                return;
            }
            if (!this.movementCharges[direction.Value])
            {
                return;
            }
            foreach (var item in this.items)
            {
                var destination = item.warehouseDestination != null ? item.warehouseDestination.WarehouseIndex.Value + movementDirection : item.WarehouseIndex.Value + movementDirection;
                this.warehouseManager.OnGroundSelected(this.warehouseManager.GetGround(destination));
                if (item.warehouseDestination != null)
                {
                    this.SetCharge(direction.Value);
                }
            }
        }
        else
        {
            this.ResetCharges();
            this.SetCharge();
        }
    }

    private void ResetCharges()
    {
        var keys = this.movementCharges.Keys.ToList();
        foreach (var key in keys)
        {
            this.movementCharges[key] = true;
        }
    }

    private void SetCharge(MovementType? movement = null)
    {
        if (movement.HasValue)
        {
            this.movementCharges[movement.Value] = false;
        }
    }

    public void OnItemAdded(BaseItem item)
    {
        var shouldAdd = this.controlItemType != null && item != null &&
            (item.GetType() == this.controlItemType || item.GetType().IsSubclassOf(this.controlItemType.GetType()));
        if (shouldAdd)
        {
            if (!this.items.Contains(item))
            {
                this.items.Add(item);
                item.OnArriveDestinationEvent.AddListener(this.OnControlItemArriveDestination);
            }
        }
    }

    private void OnControlItemArriveDestination(Ground ground, BaseItem item)
    {
        if (item.warehouseDestinationQueue == null && !GameContext.IsNavigationEnabled)
        {
            this.ResetCharges();
        }
    }

    public void OnItemRemoved(BaseItem item)
    {
        if (item != null && this.items.Contains(item))
        {
            this.items.Remove(item);
        }
    }
}
