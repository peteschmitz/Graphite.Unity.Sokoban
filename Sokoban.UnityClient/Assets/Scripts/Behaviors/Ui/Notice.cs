using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Notices;

public static class PanelAnimationNames
{
    public static readonly string BounceIn = "BounceIn2";
    public static readonly string BounceOut = "BounceOut";
}

public static class Notices
{
    public struct NoticeItem
    {
        public string localIdentifier;
        public string noticeKey;
        public string description;
        public string gamepadDescription;
        public TimeSpan duration;
        public bool showOnce;
    }

    public static readonly List<NoticeItem> LevelNotices = new List<NoticeItem>
    {
        new NoticeItem{
            localIdentifier= "80d072a9-2820-4037-b4ea-ebe2d8ed7d30",
            noticeKey = "basicMovement",
            description = "Tap nearby to move your character to adjacent tiles. \nTry pushing the crate to the goal.",
            gamepadDescription = "Push arrow keys to move your character to adjacent tiles. \nTry pushing the crate to the goal.",
            duration = TimeSpan.FromSeconds(3),
            showOnce = true
        },
        new NoticeItem{
            localIdentifier= "04959342-7f09-422c-8d25-e61f55b2b50d",
            noticeKey = "grayBoxes",
            description = "Steel (gray) boxes are immovable!",
            duration = TimeSpan.FromSeconds(3),
            showOnce = true
        },
        new NoticeItem{
            localIdentifier= "830d84ea-2ffe-48c3-a6f2-ef813286c328",
            noticeKey = "blueBoxes",
            description = "Blue boxes will continue to slide in the direction they are pushed.",
            duration = TimeSpan.FromSeconds(3),
            showOnce = true
        },
        new NoticeItem{
            localIdentifier= "ff82da4d-55f4-4e1e-b743-cca744b7b08a",
            noticeKey = "collateral",
            description = "Blue boxes will also push other boxes in their path.",
            duration = TimeSpan.FromSeconds(3),
            showOnce = true
        }
    };
}

public class Notice : BaseBehavior
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(5);

    public Text descriptionText;
    public Image panelBackground;
    public bool isVisible = false;

    private TimeSpan remainingDuration;
    private Animation animationObject;

    #region properties
    public string DescriptionText
    {
        set
        {
            var text = this.descriptionText;
            if (text != null)
            {
                text.text = value;
            }
        }
        get
        {
            return this.descriptionText?.text;
        }
    }


    #endregion

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        // this.SetInvisible();
        this.animationObject = this.gameObject.GetComponentInChildren<Animation>();
        if (this.animationObject != null)
        {
            var animationEvents = this.gameObject.GetComponentInChildren<AnimationEvents>();
            animationEvents?.OnAnimationEndEvent?.AddListener((animationEvent) =>
            {
                if (animationEvent.animationState.name.Equals(PanelAnimationNames.BounceOut))
                {
                    this.ToggleActive(false);
                }
            });
        }
        this.ToggleActive(this.isVisible);
    }

    public void SetInvisible()
    {
        this.panelBackground.color = this.panelBackground.color.WithA(0.0f);
        this.descriptionText.color = this.descriptionText.color.WithA(0.0f);
    }

    public void SetDefault()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!this.Context.isRunning)
        {
            return;
        }

        if (this.gameObject.activeSelf && this.remainingDuration.TotalMilliseconds > 0)
        {
            this.remainingDuration = this.remainingDuration.Subtract(TimeSpan.FromSeconds(Time.deltaTime));
            if (this.remainingDuration.TotalMilliseconds <= 0)
            {
                this.PlaySfx();
                if (this.animationObject != null)
                {
                    this.animationObject?.Play(PanelAnimationNames.BounceOut);
                }
                else
                {
                    this.ToggleActive(false);
                }
            }
        }
    }

    public void ToggleActive(bool visibility)
    {
        this.gameObject.SetActive(visibility);
    }

    public void Show(NoticeItem noticeItem)
    {
        var preferredDescription = noticeItem.description;
        if (GameContext.IsNavigationEnabled && !String.IsNullOrEmpty(noticeItem.gamepadDescription))
        {
            preferredDescription = noticeItem.gamepadDescription;
        }
        this.Show(preferredDescription, noticeItem.duration);
    }

    public void Show(string description, TimeSpan duration = default(TimeSpan))
    {
        if (duration == default(TimeSpan))
        {
            duration = DefaultDuration;
        }
        this.DescriptionText = description;
        this.remainingDuration = duration;
        // this.SetInvisible();
        this.ToggleActive(true);
        this.animationObject?.Play(PanelAnimationNames.BounceIn);
        this.PlaySfx();
    }

    public void OnAnimationEnd(Animation endingAnimation)
    {

    }
}
