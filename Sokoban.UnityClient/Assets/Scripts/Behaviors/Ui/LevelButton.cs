using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LevelButton : BaseBehavior
{
    public GameObject levelTextObject;
    public GameObject starImageObject;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetIcon(CategoryType category, bool isPlayable, bool isPurchased, bool isCompleted, Color color)
    {
        var image = this.starImageObject.GetComponent<Image>();
        image.color = color;
        var spriteName = "";
        if (!isPlayable && !isPurchased)
        {
            spriteName = "icons_lock";
        }
        else
        {
            switch (category)
            {
                case CategoryType.Medium:
                    spriteName = "icons_twostar";
                    break;
                case CategoryType.Hard:
                    spriteName = "icons_threestar";
                    break;
                default:
                    spriteName = "icons_star_full";
                    break;
            }
        }
        if (!spriteName.Equals(image.sprite.name))
        {
            image.sprite = this.GetResources<Sprite>("Images/icons")
                .FirstOrDefault(x => x.name == spriteName);
        }
        //levelButton.starImageObject.GetComponent<Image>().color = levelData.IsCompleted ? this.completedLevelColor : this.activeLevelColor;
    }
}
