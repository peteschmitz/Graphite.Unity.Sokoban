using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

//[RequireComponent(typeof(SpriteRenderer))]
public abstract class BaseItem : BaseBehavior, IPointerDownHandler, IPointerUpHandler
{
    public interface IMovementValidation
    {
        bool IsTraversable(Vector2Int position, MovementType movement, int pushStrength);
    }

    public interface ITimeProvider
    {
        uint TimeMs { get; }
    }

    public class Event : UnityEvent<BaseItem> { }

    public class MovementHistory : SimpleBaseData<MovementHistory>
    {
        public MovementType movementType { get; set; }
        public uint timeMs { get; set; }
    }

    public static readonly TimeSpan MinimumInputSpan = TimeSpan.FromMilliseconds(300);

    public Event OnMouseDownEvent = new Event();
    public Event OnMouseUpEvent = new Event();
    public Ground.MovementEvent OnStartDestinationEvent = new Ground.MovementEvent();
    public Ground.OccupantEvent OnArriveDestinationEvent = new Ground.OccupantEvent();

    public float movementVelocity = 3.5f;
    public ParticleSystem particles;
    protected ParticleSystemRenderer particlesRenderer;
    public SpriteRenderer spriteRenderer;

    public Ground warehouseDestination { get; set; }
    public Ground warehouseDestinationQueue { get; set; }
    public bool isMoving { get; private set; } = false;
    public MovementType? movementDirection { get; private set; }
    public bool isInputEnabled { get; private set; } = false;
    public BaseItem parent { get; private set; }
    public IMovementValidation validation { get; set; }
    public ITimeProvider timeProvider { get; set; }
    public WarehouseBuildItemRequest origin { get; set; }

    protected Animation animationObject;
    protected AnimationEvents animationEvents;

    private UnityAction<AnimationEvent> animationEndCallback { get; set; }
    private bool isSpriteResolved { get; set; } = false;
    private Bounds activeInputBounds { get; set; }
    private bool isPausedParticles { get; set; } = false;
    private DateTime lastInput { get; set; }

    #region properties
    protected virtual string SpriteName => this.GetType().GetCustomAttribute<SpriteAttribute>().Name;
    protected virtual string Spritesheet { get; } = "Images/spritesheet";
    public virtual ThumbnailAttribute Thumbnail => this.GetType().GetCustomAttribute<ThumbnailAttribute>();
    public virtual string ThumbnailSheet { get; } = "Images/thumbnails";
    protected virtual string SpriteKey { get; }
    protected virtual float SpriteOffsetY { get; } = 0.0f;
    protected virtual float InputHeightOverride { get; } = 0.0f;
    public virtual bool IsPushable { get; } = true;
    public virtual bool IsPassiveOccupant { get; } = false;
    public virtual int PushStrength { get; } = 0;
    public virtual int SortAdjustment { get; } = 0;
    protected virtual string OutAnimation => "PulseUp";
    protected virtual string InAnimation => "PulseIn";
    protected virtual string MovementAudio => "";
    protected virtual string ParticleAudio => "";

    private SpriteRenderer _spriteRenderer;
    public SpriteRenderer SpriteRenderer
    {
        get
        {
            if (this._spriteRenderer == null)
            {
                this._spriteRenderer = this.spriteRenderer;
                //this.LazyGet(ref this._spriteRenderer);
                this.ResolveSprite();
            }
            return this._spriteRenderer;
        }
    }

    protected BoxCollider2D inputCollider;// => this.LazyGet(ref this._inputCollider);

    private Vector2Int? _warehouseIndex;
    public virtual Vector2Int? WarehouseIndex
    {
        get
        {
            return this._warehouseIndex;
        }

        set
        {
            this._warehouseIndex = value;
            this.RootTransform.gameObject.name = $"{this.GetType()} {this.WarehouseIndex?.ToString()}";
        }
    }

    public int ZSort
    {
        get
        {
            var y = this.WarehouseIndex.HasValue ? this.WarehouseIndex.Value.y : -1;
            return (int)(WarehouseManager.MaxSize - y) * 100 + this.SortAdjustment;
        }
    }

    public Bounds WorldBounds
    {
        get
        {
            var bounds = this.SpriteRenderer.bounds;
            bounds.center = this.RootTransform.position;
            return bounds;
        }
    }

    public virtual WarehouseBuildItemRequest AsBuildItem
    {
        get
        {
            // preferably we use origin so the guid isn't recreated
            var buildItem = this.origin ?? WarehouseBuildItemRequest.FromSprite(this.SpriteRenderer.sprite, this.WarehouseIndex.Value);
            buildItem.column = this.WarehouseIndex.HasValue ? this.WarehouseIndex.Value.x : -1;
            buildItem.row = this.WarehouseIndex.HasValue ? this.WarehouseIndex.Value.y : -1;
            return buildItem;
        }
    }

    public Vector2 CentralPosition
    {
        get
        {
            return this.RootTransform.position.AddY(-this.SpriteOffsetY);
        }
    }

    public Transform RootTransform
    {
        get
        {
            return this.gameObject.transform;
        }
    }
    #endregion

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        this.animationObject = this.gameObject.GetComponentInChildren<Animation>();
        this.animationEvents = this.gameObject.GetComponentInChildren<AnimationEvents>();
        if (this.animationEvents != null)
        {
            animationEvents.OnAnimationStartEvent.AddListener(this.OnAnimationStart);
            animationEvents.OnAnimationEndEvent.AddListener(this.OnAnimationEnd);
        }
        this.inputCollider = this.gameObject.GetComponentInChildren<BoxCollider2D>();
        if (this.inputCollider != null && this.InputHeightOverride != 0.0f)
        {
            var diffY = (this.inputCollider.size.y - this.InputHeightOverride) * 0.5f;
            this.inputCollider.size = this.inputCollider.size.WithY(this.InputHeightOverride);
            this.inputCollider.offset = this.inputCollider.offset.WithY(diffY);
        }
        this.ResolveSprite();
        this.ToggleInput(this.isInputEnabled);

        if (this.particles != null)
        {
            this.particlesRenderer = this.particles.gameObject.GetComponent<ParticleSystemRenderer>();
        }
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (!this.Context.isRunning)
        {
            if (this.particles != null && this.particles.isPlaying)
            {
                this.isPausedParticles = true;
                this.particles.Stop();
            }
            return;
        }

        if (this.particles != null && this.isPausedParticles)
        {
            this.isPausedParticles = false;
            this.particles.Play();
        }

        if (this.warehouseDestination?.WarehouseIndex != null &&
            this.warehouseDestination.WarehouseIndex != this.WarehouseIndex &&
            !this.isMoving)
        {
            this.StartDestination();
        }

        if (this.isMoving)
        {
            if (!this.validation.IsTraversable(this.warehouseDestination.WarehouseIndex.Value, this.movementDirection.Value, this.PushStrength))
            {
                this.CancelDestination();
            }
            else
            {
                this.MoveToDestination(Time.deltaTime);
            }
        }
    }

    private void MoveToDestination(float timeSeconds)
    {
        var maxDistance = timeSeconds * this.movementVelocity;
        var currentPosition = this.RootTransform.position.AddY(-this.SpriteOffsetY);
        var remainingDistance = Vector2.Distance(this.CentralPosition, this.warehouseDestination.CentralPosition);
        if (maxDistance > Math.Abs(remainingDistance))
        {
            this.ArriveDestination();
            return;
        }
        this.RootTransform.position = Vector2.MoveTowards(this.CentralPosition, this.warehouseDestination.CentralPosition, maxDistance)
            .AddY(this.SpriteOffsetY);
    }

    public virtual void ArriveDestination()
    {
        var destination = this.warehouseDestination;
        this.isMoving = false;
        this.warehouseDestination = null;
        this.OnArriveDestinationEvent.Invoke(destination, this);
        this.movementDirection = null;

        if (this.warehouseDestinationQueue != null)
        {
            this.warehouseDestination = this.warehouseDestinationQueue;
            this.warehouseDestinationQueue = null;
            this.StartDestination();
        }
    }

    private void StartDestination()
    {
        var direction = (this.warehouseDestination.WarehouseIndex.Value - this.WarehouseIndex.Value).AsMovementType();
        if (direction.HasValue)
        {
            this.isMoving = true;
            this.movementDirection = direction;
            this.OnStartDestinationEvent.Invoke(this.warehouseDestination, this, direction.Value);
            if (this.MovementAudio.IsValid())
            {
                this.PlaySfx(this.MovementAudio);
            }
        }
    }

    public void CancelDestination()
    {
        this.movementDirection = null;
        this.isMoving = false;
        this.warehouseDestination = null;
        this.warehouseDestinationQueue = null;
    }

    protected virtual void OnValidate()
    {
        this.ResolveSprite();
    }

    protected void ResolveSprite()
    {
        var spriteName = this.SpriteName;
        if (this.isSpriteResolved || spriteName.IsInvalid())
        {
            return;
        }
        var renderer = this.SpriteRenderer;
        if (renderer.sprite?.name != spriteName)
        {
            renderer.sprite = this.GetResources<Sprite>(this.Spritesheet)
                .FirstOrDefault(x => x.name == spriteName);
        }
        //this.CenterOn(renderer.transform.position, this.WarehouseIndex);
        this.isSpriteResolved = true;
    }

    public void CenterOn(Vector3 position, Vector2Int? WarehouseIndex)
    {
        this.RootTransform.position = this.RootTransform.position
             .WithX(position.x)
             .WithY(position.y + this.SpriteOffsetY);
        this.WarehouseIndex = WarehouseIndex;
        this.SpriteRenderer.sortingOrder = this.ZSort;
    }

    public virtual void ToggleInput(bool enableInput)
    {
        this.isInputEnabled = enableInput;
        if (this.inputCollider != null)
        {
            this.inputCollider.isTrigger = this.isInputEnabled;
            this.inputCollider.enabled = this.isInputEnabled;
        }

        var currentBounds = this.inputCollider != null ? this.inputCollider.bounds : this.SpriteRenderer.bounds;
        if (this.isInputEnabled)
        {
            var center = currentBounds.center;
            var size = currentBounds.size;
            this.activeInputBounds = new Bounds(center, size);
        }
        else
        {
            this.activeInputBounds = new Bounds(currentBounds.center, Vector3.zero);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (this.Context == null || !this.isInputEnabled)
        {
            return;
        }
        if (this.Context.IsPaused)
        {
            return;
        }
        this.lastInput = DateTime.UtcNow;
        this.OnMouseDownEvent?.Invoke(this);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (this.Context == null || !this.isInputEnabled)
        {
            return;
        }
        if (this.Context.IsPaused)
        {
            return;
        }
        if (DateTime.UtcNow - this.lastInput > MinimumInputSpan)
        {
            return;
        }
        this.OnMouseUpEvent?.Invoke(this);
    }

    public virtual void Remove()
    {
        this.parent = null;
        this.OnMouseDownEvent.RemoveAllListeners();
        this.OnMouseUpEvent.RemoveAllListeners();
        Destroy(this.gameObject);
    }

    public Vector2Int Distance(BaseItem other)
    {
        return this.WarehouseIndex.Value - other.WarehouseIndex.Value;
    }

    public void SetParent(BaseItem parent)
    {
        this.parent = parent;
        if (this.parent != null)
        {
            this.CenterOn(parent.RootTransform.position, parent.WarehouseIndex);
        }
    }

    protected void SetSprite(string spriteName)
    {
        this.SpriteRenderer.sprite = this.GetResources<Sprite>(this.Spritesheet)
           .FirstOrDefault(x => x.name == spriteName);
    }

    public void EnableParticles(bool setEnabled)
    {
        if (this.particles == null)
        {
            return;
        }
        if (setEnabled && !this.particles.isPlaying)
        {
            this.particles.Play();
            if (this.ParticleAudio.IsValid())
            {
                this.PlaySfx(this.ParticleAudio);
            }
        }
        else if (!setEnabled && this.particles.isPlaying)
        {
            this.particles.Stop();
        }
    }

    public void AnimateItem(UnityAction<AnimationEvent> onFinished = null, bool intro = false)
    {
        this.animationEndCallback = onFinished;
        this.animationObject.Play(intro ? this.InAnimation : this.OutAnimation);
    }

    protected virtual void OnAnimationStart(AnimationEvent animationEvent)
    {
    }

    protected virtual void OnAnimationEnd(AnimationEvent animationEvent)
    {
        this.animationEndCallback?.Invoke(animationEvent);
        this.animationEndCallback = null;
    }

    public virtual void SetVisible()
    {
        this.SpriteRenderer.color = this.SpriteRenderer.color.WithA(1.0f);
    }

    public virtual void SetInvisible()
    {
        this.SpriteRenderer.color = this.SpriteRenderer.color.WithA(0.0f);
    }

    //public void AnimateIn(UnityAction<AnimationEvent> onFinished = null)
    //{
    //    if (onFinished != null)
    //    {
    //        var animationEvents = this.gameObject.GetComponentInChildren<AnimationEvents>();
    //        animationEvents.OnAnimationEndEvent.RemoveAllListeners();
    //        animationEvents.OnAnimationEndEvent.AddListener(onFinished);
    //    }
    //    this.animationObject.Play(this.OutAnimation);
    //}

    //protected virtual void OnGUI()
    //{
    //    if (!this.isInputEnabled)
    //    {
    //        return;
    //    }
    //    if (this.inputCollider != null)
    //    {
    //        DebugDraw.DrawWorldBox(this.inputCollider.bounds);
    //    }
    //}
}
