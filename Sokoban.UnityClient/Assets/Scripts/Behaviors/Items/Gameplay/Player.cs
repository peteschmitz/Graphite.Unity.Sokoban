using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Prefab(Path = "Prefabs/Items/Gameplay/PlayerPrefab3")]
[Sprite(Name = "player_d0", NamePattern = "player_")]
[Thumbnail(Name = "thumbnail_player")]
public class Player : BaseItem
{
    private static int animateFps = 10;
    private static float animateDelta = 1.0f / animateFps;
    private static Dictionary<MovementType, string[]> animateSeries = new Dictionary<MovementType, string[]>
    {
        { MovementType.Down, new string[]{ "player_d1", "player_d0", "player_d2" } },
        { MovementType.Right, new string[]{ "player_r1", "player_r0", "player_r2" } },
        { MovementType.Up, new string[]{ "player_u1", "player_u0", "player_u2" } },
        { MovementType.Left, new string[]{ "player_l1", "player_l0", "player_l2" } }
    };

    public SpriteRenderer shadow;

    protected override float SpriteOffsetY => 0.4f;
    public override int PushStrength => 1;
    protected override string MovementAudio => "step2";

    private float animateStep = 0.0f;
    private float frameResetStep = 0.0f;
    private int animateIndex = animateSeries[MovementType.Down].Length - 1;
    private MovementType? lastMovementDirection;

    protected override void Update()
    {
        base.Update();

        if (this.Context.IsPaused)
        {
            return;
        }

        if (this.isMoving)
        {
            this.EnableParticles(true);
            this.Animate(Time.deltaTime);
        }
        else if (this.frameResetStep > 0)
        {
            this.frameResetStep -= Time.deltaTime;
            if (frameResetStep >= 0)
            {
                this.SetSprite(this.lastMovementDirection.HasValue ? animateSeries[this.lastMovementDirection.Value][1] : this.SpriteName);
                this.SpriteRenderer.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
                this.EnableParticles(false);
            }
        }
    }

    public override void ArriveDestination()
    {
        this.lastMovementDirection = this.movementDirection;
        if (this.movementDirection.HasValue)
        {
            this.Context.data.LogMovement(this, this.movementDirection.Value, this.timeProvider.TimeMs);
        }

        base.ArriveDestination();

        if (this.warehouseDestination == null)
        {
            this.frameResetStep = animateDelta;
        }
    }

    private void Animate(float delta)
    {
        if (!this.movementDirection.HasValue)
        {
            return;
        }
        this.animateStep += delta;
        if (this.animateStep > animateDelta)
        {
            ++animateIndex;
            if (animateIndex >= animateSeries[this.movementDirection.Value].Length)
            {
                animateIndex = 0;
            }
            this.SpriteRenderer.transform.localScale = animateIndex == 0 ? new Vector3(1.0f, 1.0f, 1.0f) : new Vector3(0.9f, 1.1f, 1.0f);
            this.SetSprite(animateSeries[this.movementDirection.Value][animateIndex]);
            this.animateStep -= animateDelta;
        }
    }

    protected override void OnAnimationStart(AnimationEvent animationEvent)
    {
        base.OnAnimationStart(animationEvent);
        this.ToggleShadow(false);
    }

    protected override void OnAnimationEnd(AnimationEvent animationEvent)
    {
        base.OnAnimationEnd(animationEvent);
        this.ToggleShadow(animationEvent.animationState.name.Equals(this.InAnimation));
    }

    public override void SetVisible()
    {
        base.SetVisible();
        //this.ToggleShadow(true);
    }

    public override void SetInvisible()
    {
        base.SetInvisible();
        this.ToggleShadow(false);
    }

    protected void ToggleShadow(bool show)
    {
        if (this.shadow != null)
        {
            this.shadow.color = this.shadow.color.WithA(show ? 0.6f : 0.0f);
        }
    }
}
