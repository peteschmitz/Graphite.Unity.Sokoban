using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UiSpritePanel : MonoBehaviour
{
    public class SpriteEvent : UnityEvent<UiSpritePanel, Sprite> { }
    public class StringEvent : UnityEvent<UiSpritePanel, string> { }

    public SpriteEvent OnSpriteSelectedEvent = new SpriteEvent();
    public StringEvent OnNameSelectedEvent = new StringEvent();

    public float padding = 10.0f;
    public float anchorPadding = 10.0f;
    public int buttonSize = 128;
    public Vector2Int offset;
    public Color textColor = Color.white;
    public GameObject targetButtonObject;
    public Sprite targetSprite;
    public List<Sprite> spriteButtons;
    public List<string> nameButtons;
    public bool keyboardShortcutsEnabled = false;

    private Dictionary<GameObject, UnityAction> buttonListeners = new Dictionary<GameObject, UnityAction>();
    private List<RectTransform> activeButtons { get; set; }
    private static readonly List<KeyCode> KeyCodes = new List<KeyCode>
    {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.Alpha5,
        KeyCode.Alpha6,
        KeyCode.Alpha7
    };
    private HashSet<KeyCode> activatedKeys = new HashSet<KeyCode>();

    #region properties
    private GameObject _targetPanel;
    private GameObject TargetPanel
    {
        get
        {
            return this._targetPanel = this._targetPanel ?? this.gameObject.WithChild("Panel");
        }
    }

    private float ButtonPadding => this.padding * 0.5f;

    public bool IsVisible => this.gameObject?.GetComponent<CanvasGroup>()?.alpha != 0.0f;
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        this.Calculate();
    }

    void Update()
    {
        if (this.keyboardShortcutsEnabled && this.IsVisible)
        {
            var newActivatedKeys = KeyCodes.Where(x => Input.GetKey(x) && !this.activatedKeys.Contains(x));
            if (newActivatedKeys.HasItems())
            {
                foreach (var activatedKey in newActivatedKeys)
                {
                    var index = KeyCodes.IndexOf(activatedKey);
                    if (this.spriteButtons.HasItems() && index < this.spriteButtons.Count)
                    {
                        this.OnSpriteSelectedEvent?.Invoke(this, spriteButtons[index]);
                    }
                    this.activatedKeys.Add(activatedKey);
                }
            }
            else if (this.activatedKeys.HasItems())
            {
                this.activatedKeys.RemoveWhere(x => !Input.GetKey(x));
            }
        }
    }

    // public void OnValidate()
    // {
    //     //#if UNITY_EDITOR
    //     //        if (!Application.isPlaying)
    //     //        {
    //     //        }
    //     //#endif
    //     this.Calculate();
    // }

    public void Calculate()
    {
        if (this.TargetPanel == null)
        {
            return;
        }
        for (var i = 0; i < this.TargetPanel.transform.childCount; ++i)
        {
            var childObject = this.TargetPanel.transform.GetChild(i).gameObject;
            StartCoroutine(Destroy(childObject));
        }
        this.activeButtons = new List<RectTransform>();
        foreach (var sprite in this.spriteButtons.AsNotNull())
        {
            if (sprite == null)
            {
                continue;
            }
            var buttonObject = this.AddButton(sprite, "");
            var button = buttonObject.GetComponent<CustomButton>();
            var onSelectAction = new UnityAction(() =>
            {
                this.OnSpriteSelectedEvent?.Invoke(this, sprite);
            });
            button.onClick.AddListener(onSelectAction);
            this.buttonListeners.Add(buttonObject, onSelectAction);
        }

        foreach (var buttonName in this.nameButtons.AsNotNull())
        {
            if (buttonName.IsInvalid())
            {
                continue;
            }
            var buttonObject = this.AddButton(this.targetSprite, buttonName);
            var button = buttonObject.GetComponent<CustomButton>();
            var onSelectAction = new UnityAction(() => { this.OnNameSelectedEvent?.Invoke(this, buttonName); });
            button.onClick.AddListener(onSelectAction);
            this.buttonListeners.Add(buttonObject, onSelectAction);
        }

        this.ValidatePosition();
    }

    private GameObject AddButton(Sprite sprite, string buttonName)
    {
        this.activeButtons = this.activeButtons ?? new List<RectTransform>();

        var buttonObject = Instantiate(this.targetButtonObject, this.TargetPanel.gameObject.transform);
        var image = buttonObject.GetComponent<Image>();
        var transform = buttonObject.GetComponent<RectTransform>();
        image.sprite = sprite;

        var text = buttonObject.GetComponent<Button>()
            .gameObject.WithChild("Text")
            .GetComponent<Text>();

        text.text = buttonName;
        text.color = this.textColor;

        if (buttonName.IsValid())
        {
            var currentWidth = text.GetComponent<RectTransform>().rect.size.x;
            var settings = text.GetGenerationSettings(text.GetComponent<RectTransform>().rect.size);
            var preferredWidth = text.cachedTextGeneratorForLayout.GetPreferredWidth(text.text, settings);
            var preferredHeight = text.cachedTextGeneratorForLayout.GetPreferredHeight(text.text, settings);
            var buffer = 30;
            var width = preferredWidth + buffer * 2;
            var height = preferredHeight;
            text.rectTransform.sizeDelta = new Vector2(width, transform.sizeDelta.y);
            transform.sizeDelta = new Vector2(width, transform.sizeDelta.y);
        }
        else
        {
            var widthScale = sprite.rect.width > sprite.rect.height ? buttonSize / sprite.rect.width : buttonSize / sprite.rect.height;
            var heightScale = sprite.rect.height > sprite.rect.width ? buttonSize / sprite.rect.height : buttonSize / sprite.rect.width;
            var width = sprite.rect.width * widthScale;
            var height = sprite.rect.height * heightScale;
            image.rectTransform.sizeDelta = new Vector2(width, height);
        }

        buttonObject.name = $"{targetButtonObject.name} ({(string.IsNullOrEmpty(buttonName) ? sprite.name : buttonName)})";

        this.activeButtons.Add(image.rectTransform);
        return buttonObject;
    }

    // http://answers.unity.com/answers/1318643/view.html
    private IEnumerator Destroy(GameObject targetObject)
    {
        // clean up listeners
        if (this.buttonListeners.ContainsKey(targetObject))
        {
            var button = targetObject.GetComponent<CustomButton>();
            if (button != null)
            {
                button.onClick.RemoveListener(this.buttonListeners[targetObject]);
            }
            this.buttonListeners.Remove(targetObject);
        }

        yield return new WaitForEndOfFrame();
        DestroyImmediate(targetObject);
    }

    public void SetVisibility(bool isVisible)
    {
        var canvasGroup = this.gameObject?.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = isVisible;
            canvasGroup.alpha = isVisible ? 1.0f : 0.0f;
        }
    }

    private void ValidatePosition()
    {
        var totalButtonWidth = 0.0f;
        var totalButtonHeight = 0.0f;
        foreach (var button in this.activeButtons)
        {
            var transform = button.GetComponent<RectTransform>();
            totalButtonWidth += transform.sizeDelta.x + (this.activeButtons.Last() == button ? 0.0f : this.ButtonPadding);
            totalButtonHeight = Mathf.Max(totalButtonHeight, transform.sizeDelta.y);
        }

        var panelRect = this.TargetPanel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(totalButtonWidth + this.padding * 2, totalButtonHeight + this.padding * 2);

        var currentButtonX = -totalButtonWidth * panelRect.pivot.x + this.offset.x;
        var currentButtonY = -totalButtonHeight * panelRect.pivot.y + this.offset.y;
        foreach (var button in this.activeButtons)
        {
            var transform = button.GetComponent<RectTransform>();
            transform.localPosition = new Vector2(currentButtonX, currentButtonY);
            currentButtonX += transform.sizeDelta.x + this.ButtonPadding;
        }
    }

    public void AnchorTo(BaseItem targetObject) =>
        this.AnchorTo(targetObject.SpriteRenderer.bounds);
    public void AnchorTo(Bounds targetBounds)
    {
        var rect = this.gameObject.GetComponent<RectTransform>();
        rect.position = targetBounds.center
            .AddX(rect.sizeDelta.x * rect.lossyScale.x * rect.pivot.x)
            .AddY(targetBounds.size.y * 0.5f + targetBounds.size.y * rect.lossyScale.y * rect.pivot.y + this.anchorPadding / GameContext.PixelPerUnit)
            .WithZ(rect.position.z);
    }
}
