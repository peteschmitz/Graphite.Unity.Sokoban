using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Tabber : BaseBehavior
{
    //private class TabItem
    //{
    //    public Text text { get; private set; }
    //    public BoxCollider2D boxCollider { get; private set; }
    //    public string tabName { get; set; }

    //    public TabItem(GameObject gameObject, string tabName)
    //    {
    //        this.tabName = tabName;
    //        this.text = gameObject.GetComponent<Text>();
    //        this.text.text = tabName;
    //        this.text.GetComponent<RectTransform>().sizeDelta = this.text.GetPreferredSize(25, 3);
    //        this.boxCollider = gameObject.GetComponent<BoxCollider2D>();
    //        this.boxCollider.size = this.text.GetComponent<RectTransform>().sizeDelta;
    //        this.boxCollider.transform.position = this.boxCollider.transform.position
    //            .WithX(this.text.transform.position.x)
    //            .AddX(-this.boxCollider.size.x * 0.5f);
    //    }
    //}

    public class Event : UnityEvent<string, bool> { }

    public Event OnTabSelectedEvent = new Event();

    public GameObject tabPrefab;
    public Image tabUnderline;
    public float tabSpacing = 3.0f;
    public string[] tabNames;

    private Dictionary<string, TabberItem> tabItems = new Dictionary<string, TabberItem>();
    private TabberItem activeTab;
    private Vector3? originalPosition;
    private float boundsWidth;
    private List<TabberItem> tabbers = new List<TabberItem>();
    private bool isInitialized = false;
    private string activeTabName;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        if (!this.tabNames.HasItems())
        {
            throw new ArgumentException("TabNames was empty, should contain at least one element.");
        }
        this.BuildTabs();
        this.activeTabName = this.activeTabName.OnNullOrEmpty(this.tabNames[0]); this.isInitialized = true;
        this.ActivateTab(this.activeTabName);
    }

    // Update is called once per frame
    void Update()
    {
        // if (!this.isInitialized && this.tabbers.All(x => x.IsInitialized))
        // {
        //     this.isInitialized = true;
        //     var listX = 0.0f;
        //     foreach (var tabItem in this.tabbers)
        //     {
        //         tabItem.transform.localPosition = tabItem.transform.localPosition
        //             .WithX(listX);
        //         tabItem.SetName(tabItem.tabName);
        //         tabItem.OnPointerDownEvent.AddListener((item) => this.ActivateTab(item.tabName));
        //         listX += this.tabSpacing + tabItem.BoxCollider.size.x;
        //         this.tabItems.Add(tabItem.tabName, tabItem);
        //     }

        //     this.boundsWidth = listX - this.tabSpacing;
        //     this.tabNames.ForEach(x => this.DeactivateTab(x));
        //     this.ActivateTab(this.activeTabName);
        // }
    }

    private void BuildTabs()
    {
        var listX = 0.0f;
        var currentIndex = 0;
        foreach (var tabName in this.tabNames)
        {
            var gameObject = currentIndex == 0 ? this.tabPrefab : Instantiate(this.tabPrefab, this.tabPrefab.transform.parent);
            gameObject.name = $"TabItem ({tabName})";
            var tabItem = gameObject.GetComponent<TabberItem>();
            tabItem.Initialize();
            tabItem.tabName = tabName;
            this.tabbers.Add(tabItem);
            tabItem.transform.localPosition = tabItem.transform.localPosition
                .WithX(listX);
            tabItem.SetName(tabItem.tabName);
            tabItem.OnPointerDownEvent.AddListener((item) => this.ActivateTab(item.tabName));
            listX += this.tabSpacing + tabItem.BoxCollider.size.x;
            this.tabItems.Add(tabItem.tabName, tabItem);

            // tabItem.SetName(tabName);
            // tabItem.OnPointerDownEvent.AddListener((item) => this.ActivateTab(item.tabName));
            // gameObject.transform.localPosition = gameObject.transform.localPosition
            //     .WithX(listX);
            // listX += this.tabSpacing + tabItem.BoxCollider.size.x;
            // this.tabItems.Add(tabName, tabItem);
            ++currentIndex;
        }
        this.boundsWidth = listX - this.tabSpacing;
        this.tabNames.ForEach(x => this.DeactivateTab(x));

        // this.boundsWidth = listX;
        // this.tabNames.ForEach(x => this.DeactivateTab(x));
    }

    public void ActivateTab(string tabName, bool forceUpdate = false, bool toFirst = true)
    {
        this.activeTabName = tabName;
        if (!this.isInitialized)
        {
            return;
        }
        // if (!this.tabItems.HasItems())
        // {
        //     this.BuildTabs();
        // }

        var previousTab = this.activeTab;
        var targetTab = this.tabItems[tabName];

        if (previousTab == targetTab && !forceUpdate)
        {
            return;
        }

        targetTab.Text.color = targetTab.Text.color
            .WithA(1.0f);
        this.tabUnderline.transform.localPosition = this.tabUnderline.transform.localPosition
            .WithX(targetTab.transform.localPosition.x);
        var underlineTransform = this.tabUnderline.GetComponent<RectTransform>();
        underlineTransform.sizeDelta = underlineTransform.sizeDelta
            .WithX(targetTab.BoxCollider.size.x);
        if (previousTab != null && previousTab != targetTab)
        {
            this.DeactivateTab(previousTab.tabName);
        }

        this.activeTab = targetTab;

        if (!this.originalPosition.HasValue)
        {
            this.originalPosition = this.transform.localPosition;
        }
        this.transform.localPosition = this.originalPosition.Value
            .WithX(-this.boundsWidth * 0.5f * this.gameObject.transform.localScale.x);

        this.OnTabSelectedEvent.Invoke(this.activeTab.tabName, toFirst);
    }

    private void DeactivateTab(string tabName)
    {
        var tab = this.tabItems[tabName];
        tab.Text.color = tab.Text.color
            .WithA(0.5f);
    }
}
