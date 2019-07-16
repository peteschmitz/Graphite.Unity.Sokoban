using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MainMenuManager : BaseBehavior
{
    public class PageEvent : UnityEvent<MainMenuManager, MenuPageType> { }

    public PageEvent OnPageSelectedEvent = new PageEvent();

    public GameObject levelListButton;
    public GameObject createButton;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        levelListButton.GetComponent<CustomButton>()
            .onClick.AddListener(() => this.OnPageSelectedEvent.Invoke(this, MenuPageType.LevelList));
        createButton.GetComponent<CustomButton>()
            .onClick.AddListener(() => this.OnPageSelectedEvent.Invoke(this, MenuPageType.Create));

        this.Context.SetRunning(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    public enum MenuPageType
    {
        Create,
        LevelList
    }
}
