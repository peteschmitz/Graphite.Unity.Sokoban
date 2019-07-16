using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UiPager : BaseBehavior
{
    public class PagerEvent : UnityEvent<UiPager, int> { }
    public class Event : UnityEvent<UiPager> { }

    public PagerEvent OnPageSelectedEvent = new PagerEvent();
    public Event OnMovePreviousEvent = new Event();
    public Event OnMoveNextEvent = new Event();

    public GameObject buttonPrefab;
    public Sprite pageActiveSprite;
    public Sprite pageInactiveSprite;
    public int pagesCount = 2;
    public int activePageNumber = 1;
    public int elementSpacing = 8;
    public GameObject nextButton;
    public GameObject previousButton;

    [HideInInspector]
    public bool allowMovePrevious;
    [HideInInspector]
    public bool allowMoveNext;

    private int currentPage = 0;
    //private GameObject previousButton;
    //private GameObject nextButton;
    private List<GameObject> pageButtons = new List<GameObject>();

    #region properties
    #endregion

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        this.OnPageSelectedEvent.AddListener((caller, page) => Debug.Log($"Activated page {page}"));
        this.nextButton.GetComponent<CustomButton>().onClick.AddListener(this.MoveNext);
        this.previousButton.GetComponent<CustomButton>().onClick.AddListener(this.MovePrevious);
        //this.SetPages(this.pagesCount);
        //this.SetActive(this.activePageNumber);
    }

    public void SetPages(int pagesCount)
    {
        this.pagesCount = pagesCount;

        foreach (var existingButton in this.pageButtons)
        {
            existingButton.transform.SetParent(null, false);
        }

        //this.nextButton = this.nextButton ?? Instantiate(this.buttonPrefab, this.gameObject.transform);
        //this.nextButton.name = "nextButton";
        //this.nextButton.GetComponent<CustomButton>().onClick.AddListener(this.MoveNext);
        //var nextText = this.nextButton.GetComponentInChildren<Text>();
        //if (nextText != null)
        //{
        //    nextText.text = ">";
        //    nextText.GetComponent<RectTransform>().sizeDelta = nextText.GetPreferredSize(25, 5);
        //    this.nextButton.GetComponent<RectTransform>().sizeDelta = nextText.GetComponent<RectTransform>().sizeDelta;
        //}

        //this.previousButton = this.previousButton ?? Instantiate(this.buttonPrefab, this.gameObject.transform);
        //this.previousButton.name = "previousButton";
        //this.previousButton.GetComponent<CustomButton>().onClick.AddListener(this.MovePrevious);
        //var previousText = this.previousButton.GetComponentInChildren<Text>();
        //if (previousText != null)
        //{
        //    previousText.text = "<";
        //    previousText.GetComponent<RectTransform>().sizeDelta = previousText.GetPreferredSize(25, 5);
        //    this.previousButton.GetComponent<RectTransform>().sizeDelta = previousText.GetComponent<RectTransform>().sizeDelta;
        //}

        var buttons = new List<GameObject> { /*this.previousButton*/ };
        for (var i = 0; i < pagesCount; ++i)
        {
            var button = i < this.pageButtons.Count ? this.pageButtons[i] : null;
            if (button == null)
            {
                button = Instantiate(this.buttonPrefab, this.gameObject.transform);
                // button.
                this.pageButtons.Add(button);
            }
            button.transform.SetParent(this.gameObject.transform, false);
            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = "";
                var rect = text.gameObject.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2();
            }
            button.GetComponent<Image>().sprite = this.pageInactiveSprite;
            button.GetComponent<RectTransform>().sizeDelta = this.pageInactiveSprite.rect.size;
            var script = button.GetComponent<CustomButton>();
            script.navigation = new Navigation
            {
                mode = UnityEngine.UI.Navigation.Mode.None
            };
            var pageNumber = i + 1;
            script.onClick.RemoveAllListeners();
            script.onClick.AddListener(() => this.SetActive(pageNumber));
            buttons.Add(button);
        }
        //buttons.Add(this.nextButton);

        var parentTransform = this.gameObject.GetComponent<RectTransform>();
        var totalWidth = buttons.Sum(x => x.GetComponent<RectTransform>().sizeDelta.x) + (buttons.Count - 1) * elementSpacing;
        var maxHeight = buttons.Max(x => x.GetComponent<RectTransform>().sizeDelta.y);
        var listX = parentTransform.sizeDelta.x * (0.5f - parentTransform.pivot.x) - totalWidth * 0.5f;
        foreach (var button in buttons)
        {
            var rect = button.GetComponent<RectTransform>();
            var yOffset = rect.sizeDelta.y < maxHeight ? (maxHeight - rect.sizeDelta.y) * 0.5f : 0.0f;
            rect.localPosition = rect.localPosition.WithX(listX).WithY(yOffset - parentTransform.sizeDelta.y * parentTransform.pivot.y);
            listX += rect.sizeDelta.x + elementSpacing;
        }
    }

    public void SetActive(int activePage, bool force = false)
    {
        if (activePage == this.currentPage && !force)
        {
            return;
        }

        var activeIndex = activePage - 1;
        this.previousButton.SetActive(activeIndex > 0 || this.allowMovePrevious);
        this.nextButton.SetActive(activeIndex < this.pagesCount - 1 || this.allowMoveNext);

        for (var i = 0; i < this.pageButtons.Count; ++i)
        {
            var button = this.pageButtons[i];
            var isActive = i < this.pagesCount;
            button.SetActive(isActive);
            if (!isActive)
            {
                continue;
            }
            button.GetComponent<Image>().sprite = i == activeIndex ? this.pageActiveSprite : this.pageInactiveSprite;
            button.GetComponent<RectTransform>().sizeDelta = button.GetComponent<Image>().sprite.rect.size;
        }

        this.activePageNumber = this.currentPage = activePage;
        this.OnPageSelectedEvent?.Invoke(this, this.activePageNumber);
    }

    private void MoveNext()
    {
        if (!GameContext.IsNavigationEnabled)
        {
            this.EventSystem.SetSelectedGameObject(null);
        }
        if (this.activePageNumber + 1 > this.pagesCount)
        {
            if (this.allowMoveNext)
            {
                this.PlaySfx();
                this.OnMoveNextEvent.Invoke(this);
            }
            return;
        }
        this.PlaySfx();
        this.SetActive(this.activePageNumber + 1);
    }

    private void MovePrevious()
    {
        if (!GameContext.IsNavigationEnabled)
        {
            this.EventSystem.SetSelectedGameObject(null);
        }
        if (this.activePageNumber - 1 <= 0)
        {
            if (this.allowMovePrevious)
            {
                this.PlaySfx();
                this.OnMovePreviousEvent.Invoke(this);
            }
            return;
        }
        this.PlaySfx();
        this.SetActive(this.activePageNumber - 1);
    }
}
