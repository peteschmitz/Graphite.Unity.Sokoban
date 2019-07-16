using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LocaleText : BaseBehavior
{
    public LocaleTextType localeTextType;

    private Text targetText;

    protected override void Start()
    {
        base.Start();
        if (this.localeTextType != LocaleTextType.None && this.Locale != null)
        {
            if (this.targetText == null)
            {
                this.targetText = this.gameObject.GetComponent<Text>();
            }
            if (this.targetText == null)
            {
                return;
            }
            var localeText = this.Locale.Get(this.localeTextType);
            if (localeText != null)
            {
                this.targetText.text = localeText;
            }
        }
    }
}
