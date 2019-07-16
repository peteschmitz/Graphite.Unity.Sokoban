using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomButton : Button
{
    public class ValueEvent : UnityEvent<CustomButton, bool> { };

    public ValueEvent OnValueChangeEvent = new ValueEvent();
    public bool defaultValue = true;
    public bool toggleEnabled = false;
    public bool toggleFromClick = true;
    public Sprite untoggleImage;
    public Color untoggleColor;
    public Graphic targetToggleGraphic;

    public Sprite toggleBackground { get; private set; }
    public Image targetBackground { get; private set; }

    private Color? toggleColor;
    private bool? currentValue = null;
    private AudioSource audioSource;

    private GameContext Context => GameContext.Instance;

    protected override void Start()
    {
        base.Start();
        //this.currentValue = false;
        this.targetBackground = this.targetGraphic?.GetComponent<Image>();
        this.toggleBackground = targetBackground?.sprite;
        this.toggleColor = this.targetToggleGraphic?.color;
        this.audioSource = this.GetComponent<AudioSource>();
        this.Toggle(this.defaultValue);
        if (GameContext.IsNavigationEnabled && this.toggleFromClick)
        {
            this.onClick.AddListener(() =>
            {
                this.Toggle(this.currentValue.HasValue ? !this.currentValue.Value : false);
                this.PlaySfx();
            });
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (this.toggleFromClick)
        {
            this.Toggle(this.currentValue.HasValue ? !this.currentValue.Value : false);
        }
        this.PlaySfx();
        base.OnPointerClick(eventData);
    }

    public void Toggle(bool value)
    {
        if (this.currentValue == value || !this.toggleEnabled)
        {
            return;
        }
        if (this.toggleBackground == null)
        {
            this.defaultValue = value;
            return;
        }
        if (this.toggleBackground != null && this.targetGraphic != null)
        {
            this.targetBackground.sprite = value ? this.toggleBackground : this.untoggleImage ?? this.toggleBackground;
        }
        if (this.toggleColor != null)
        {
            if (this.targetToggleGraphic != null)
            {
                this.targetToggleGraphic.color = value ? this.toggleColor.Value : this.untoggleColor;
            }
        }
        this.currentValue = value;
        this.OnValueChangeEvent?.Invoke(this, value);
    }

    public void PlaySfx()
    {
        if (this.audioSource == null || !this.Context.data.IsSoundEnabled)
        {
            return;
        }
        this.audioSource.Play();
    }
}
