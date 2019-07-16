using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TabberItem : BaseBehavior, IPointerDownHandler
{
    public class Event : UnityEvent<TabberItem> { };

    public Event OnPointerDownEvent = new Event();

    public bool IsInitialized => this.isInitialized;

    private Text _text;
    public Text Text => this._text;//this.LazyGet(ref this._text);
    private BoxCollider2D _boxCollider;
    public BoxCollider2D BoxCollider => this._boxCollider;//this.LazyGet(ref this._boxCollider);
    public string tabName { get; set; }

    private bool isInitialized = false;

    // protected override void Start()
    public void Initialize()
    {
        // base.Start();

        this.isInitialized = true;
        this._text = this.GetComponent<Text>();
        this._boxCollider = this.GetComponent<BoxCollider2D>();

        //base.Start();
        //this.text = this.GetComponent<Text>();
        ////this.text.text = tabName;
        //this.Text.GetComponent<RectTransform>().sizeDelta = this.Text.GetPreferredSize(25, 3);
        ////this.boxCollider = this.GetComponent<BoxCollider2D>();
        //this.BoxCollider.size = this.Text.GetComponent<RectTransform>().sizeDelta;
        //this.BoxCollider.transform.position = this.BoxCollider.transform.position
        //    .WithX(this.Text.transform.position.x)
        //    .AddX(-this.BoxCollider.size.x * 0.5f);

        if (this.tabName.IsValid())
        {
            this.SetName(this.tabName);
            //this.Text.text = tabName;
        }
    }

    public void SetName(string tabName)
    {
        this.tabName = tabName;
        // if (!this.isInitialized)
        // {
        //     this.Initialize();
        //     return;
        // }

        if (this.Text != null)
        {
            this.Text.text = this.Locale.Get(tabName);
            this.Text.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 50f);//this.Text.GetPreferredSize(25, 3);
            //this.boxCollider = this.GetComponent<BoxCollider2D>();
            this.BoxCollider.size = this.Text.GetComponent<RectTransform>().sizeDelta;
            this.BoxCollider.offset = new Vector2(this.BoxCollider.size.x * 0.5f, 0.0f);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        this.OnPointerDownEvent?.Invoke(this);
    }
}
