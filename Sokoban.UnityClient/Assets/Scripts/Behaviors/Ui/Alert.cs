using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Alert : BaseBehavior, IPointerClickHandler
{
    public VisibilityEvent OnVisibilityEvent = new VisibilityEvent();

    public GameObject panel;
    public GameObject dismissObject;
    public GameObject titleObject;
    public GameObject descriptionObject;
    public GameObject acceptObject;
    public GameObject declineObject;
    public GameObject defaultSelection;
    // public Button[] otherNavigationButtons;
    public bool isVisible = false;

    protected Animation animationObject;
    protected ResultHandler lastResult;

    private Action<ResultHandler> activeAction;
    private string titleDefault;
    private string descriptionDefault;
    private string acceptDefault;
    private string declineDefault;
    private Dictionary<Graphic, float> defaultAlphas = new Dictionary<Graphic, float>();

    #region properties
    public string TitleText
    {
        set
        {
            var text = this.titleObject?.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = value;
            }
        }
        get
        {
            return this.titleObject?.GetComponentInChildren<Text>()?.text;
        }
    }

    public string DescriptionText
    {
        set
        {
            var text = this.descriptionObject?.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = value;
            }
        }
        get
        {
            return this.descriptionObject?.GetComponentInChildren<Text>()?.text;
        }
    }

    public string AcceptText
    {
        set
        {
            var text = this.acceptObject?.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = value;
            }
        }
        get
        {
            return this.acceptObject?.GetComponentInChildren<Text>()?.text;
        }
    }

    public string DeclineText
    {
        set
        {
            var text = this.declineObject?.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = value;
            }
        }
        get
        {
            return this.declineObject?.GetComponentInChildren<Text>()?.text;
        }
    }

    public bool IsActive
    {
        get
        {
            return this.gameObject.activeSelf;
        }
    }
    #endregion

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        this.titleDefault = this.TitleText;
        this.descriptionDefault = this.DescriptionText;
        this.acceptDefault = this.AcceptText;
        this.declineDefault = this.DeclineText;

        this.animationObject = this.gameObject.GetComponentInChildren<Animation>();
        if (this.animationObject != null)
        {
            var animationEvents = this.gameObject.GetComponentInChildren<AnimationEvents>();
            animationEvents?.OnAnimationEndEvent?.AddListener((animationEvent) =>
            {
                if (animationEvent.animationState.name.Equals(PanelAnimationNames.BounceOut))
                {
                    this.activeAction?.Invoke(this.lastResult);
                    this.activeAction = null;
                    this.ToggleActive(false);
                }
                else if (animationEvent.animationState.name.Equals(PanelAnimationNames.BounceIn))
                {
                    this.SetInvisible(false);
                }
            });
        }
        this.audioSource = this.GetComponent<AudioSource>();

        this.dismissObject?.GetComponent<Button>()?.onClick.AddListener(() => this.OnResult(ResultType.Dismissed));
        this.acceptObject?.GetComponent<Button>()?.onClick.AddListener(() => this.OnResult(ResultType.Accepted));
        this.declineObject?.GetComponent<Button>()?.onClick.AddListener(() => this.OnResult(ResultType.Declined));
        // this.SetInvisible();
        this.ToggleActive(this.isVisible, false);
    }

    public void SetDefault()
    {
        this.TitleText = this.titleDefault;
        this.DescriptionText = this.descriptionDefault;
        this.AcceptText = this.acceptDefault;
        this.DeclineText = this.declineDefault;
    }

    protected virtual void OnResult(ResultType result)
    {
        if (this.lastResult != null)
        {
            return;
        }
        this.lastResult = new ResultHandler
        {
            alert = this,
            result = result
        };

        if (this.animationObject != null)
        {
            this.animationObject.Play(PanelAnimationNames.BounceOut);
        }
        else
        {
            this.activeAction?.Invoke(this.lastResult);
            this.activeAction = null;
            this.ToggleActive(false);
        }
        this.PlaySfx();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ToggleActive(bool visibility, bool setNavigation = true)
    {
        this.gameObject.SetActive(visibility);
        if (GameContext.IsNavigationEnabled)
        {
            if (visibility)
            {
                this.SelectDefault();
            }
            else
            {
                this.EventSystem.SetSelectedGameObject(null);
            }
        }
        this.OnVisibilityEvent.Invoke(visibility);
    }

    public virtual void SelectDefault()
    {
        this.EventSystem.SetSelectedGameObject(this.acceptObject);

        // var accept = this.acceptObject.GetComponent<Button>();
        // var decline = this.declineObject.GetComponent<Button>();
        // Button firstOther = null;
        // if (this.otherNavigationButtons.HasItems())
        // {
        //     var otherActives = this.otherNavigationButtons.Where(x => x?.gameObject?.activeInHierarchy == true).ToList();
        //     if (otherActives.HasItems())
        //     {
        //         firstOther = otherActives.First();
        //         var previousTarget = accept;
        //         foreach (var otherActive in otherActives)
        //         {
        //             // Navigate back up
        //             otherActive.navigation = new Navigation
        //             {
        //                 mode = UnityEngine.UI.Navigation.Mode.Explicit,
        //                 selectOnUp = previousTarget
        //             };

        //             // Navigate down to this item
        //             if (previousTarget != null && previousTarget != accept) // accept navigation set below
        //             {
        //                 var currentUp = previousTarget.navigation.selectOnUp;
        //                 previousTarget.navigation = new Navigation
        //                 {
        //                     mode = UnityEngine.UI.Navigation.Mode.Explicit,
        //                     selectOnUp = currentUp,
        //                     selectOnDown = otherActive
        //                 };
        //             }

        //             previousTarget = otherActive;
        //         }
        //     }
        // }
        // accept.navigation = new Navigation
        // {
        //     mode = UnityEngine.UI.Navigation.Mode.Explicit,
        //     selectOnRight = decline,
        //     selectOnDown = firstOther
        // };
        // decline.navigation = new Navigation
        // {
        //     mode = UnityEngine.UI.Navigation.Mode.Explicit,
        //     selectOnLeft = accept,
        //     selectOnDown = firstOther
        // };
    }

    public void Prompt(Action<ResultHandler> action, bool setInvisible = false)
    {
        this.activeAction = action;
        this.lastResult = null;

        // this.SetInvisible();
        this.ToggleActive(true);
        // this.SelectDefault();
        if (this.animationObject != null)
        {
            var isAnimating = this.animationObject.Play(PanelAnimationNames.BounceIn);
            if (isAnimating && setInvisible)
            {
                this.SetInvisible();
            }
        }
        // this.ToggleActive(true);
    }

    public void SetInvisible(bool isInvisible = true)
    {
        var texts = new Text[]
        {
            this.dismissObject?.GetComponentInChildren<Text>(),
            this.acceptObject?.GetComponentInChildren<Text>(),
            this.declineObject?.GetComponentInChildren<Text>(),
            this.titleObject?.GetComponentInChildren<Text>(),
            this.descriptionObject?.GetComponentInChildren<Text>()
        };
        foreach (var text in texts)
        {
            if (text != null)
            {
                if (!defaultAlphas.ContainsKey(text))
                {
                    defaultAlphas.Add(text, text.color.a);
                }
                var defaultAlpha = defaultAlphas.ContainsKey(text) ? defaultAlphas[text] : 1.0f;
                text.color = text.color.WithA(isInvisible ? 0.0f : defaultAlpha);
            }
        }

        var images = new Image[]
        {
            this.panel.GetComponent<Image>(),
            this.dismissObject?.GetComponent<Image>(),
            this.dismissObject?.GetComponentInChildren<Image>(),
            this.acceptObject?.GetComponent<Image>(),
        };
        foreach (var image in images)
        {
            if (image != null)
            {
                if (!defaultAlphas.ContainsKey(image))
                {
                    defaultAlphas.Add(image, image.color.a);
                }
                var defaultAlpha = defaultAlphas.ContainsKey(image) ? defaultAlphas[image] : 1.0f;
                image.color = image.color.WithA(isInvisible ? 0.0f : defaultAlpha);
            }
        }

        var buttons = new Button[]
        {
            this.declineObject?.GetComponentInChildren<Button>(),
        };
        foreach (var button in buttons)
        {
            if (button != null)
            {
                var colors = button.colors;
                colors.normalColor = button.colors.normalColor.WithA(isInvisible ? 0.0f : 1.0f);
                button.colors = colors;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // dismiss only if top-most background is tapped
        if (eventData.rawPointerPress == this.gameObject)
        {
            this.OnResult(ResultType.Dismissed);
        }
    }

    public class ResultHandler
    {
        public ResultType result;
        public Alert alert;

        public ResultHandler WithAccepted(Action action)
        {
            if (result == ResultType.Accepted)
            {
                action?.Invoke();
            }
            return this;
        }
    }

    public void ToggleDismiss(bool isEnabled)
    {
        this.dismissObject?.SetActive(isEnabled);
    }

    public enum ResultType
    {
        Dismissed,
        Accepted,
        Declined
    }
}
