using System.Linq;
using UnityEngine;

[Prefab(Path = "Prefabs/Items/Gameplay/BoxPrefab2")]
public class Box : BaseItem
{
    private static class AnimationNames
    {
        public static readonly string Pulse = "Pulse";
    }

    public const string DefaultSpriteKey = "box";

    public BoxType boxType = BoxType.Brown;

    #region properties
    protected override string SpriteName => this.boxType.GetCustomTypeAttributes<SpriteAttribute>()
        .First(x => x.Key == DefaultSpriteKey)
        .Name;
    protected override string SpriteKey => DefaultSpriteKey;
    protected override float SpriteOffsetY => 0.38f;
    public override ThumbnailAttribute Thumbnail => this.boxType.GetCustomTypeAttributes<ThumbnailAttribute>()
        .First(x => x.Key == DefaultSpriteKey);
    public Material ParticleMaterial =>
        this.GetResource<Material>(this.boxType.GetCustomTypeAttribute<ResourceAttribute>().Path);
    protected override string ParticleAudio => "powerup5";

    public override bool IsPushable
    {
        get
        {
            switch (this.boxType)
            {
                case BoxType.Red:
                case BoxType.Gray:
                    return false;
                default:
                    return true;
            }
        }
    }

    public override int PushStrength
    {
        get
        {
            switch (this.boxType)
            {
                case BoxType.Blue:
                    return WarehouseManager.MaxSize;
                default:
                    return 0;
            }
        }
    }

    protected override string MovementAudio
    {
        get
        {
            switch (this.boxType)
            {
                case BoxType.Blue:
                    return "ice1";
                default:
                    return "wood2";
            }
        }
    }
    #endregion

    protected override void Start()
    {
        base.Start();
        if (this.particlesRenderer != null)
        {
            var newMat = this.ParticleMaterial;
            this.particlesRenderer.material = newMat;
        }
    }

    public void StartMarkerAnimation()
    {
        this.animationObject?.Play(AnimationNames.Pulse);
        this.EnableParticles(true);
    }

    public void StopMarkerAnimation()
    {
        this.animationObject?.Stop();
        this.EnableParticles(false);
    }
}
