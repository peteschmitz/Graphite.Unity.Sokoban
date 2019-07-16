using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AlertGameplay : Alert
{
    public Button iapRemoveAdsButton;
    public Button iapMoreLevelsButton;
    public Button restartButton;
    public CustomButton soundToggleButton;

    protected override void Start()
    {
        base.Start();
    }

    public void ToggleExtras(bool? ads = null, bool? levels = null, bool? restart = null, bool? sound = null)
    {
        if (ads.HasValue)
        {
            this.iapRemoveAdsButton?.gameObject?.SetActive(ads.Value);
        }
        if (levels.HasValue)
        {
            this.iapMoreLevelsButton?.gameObject?.SetActive(levels.Value);
        }
        if (restart.HasValue)
        {
            this.restartButton?.gameObject?.SetActive(restart.Value);
        }
        if (sound.HasValue)
        {
            this.soundToggleButton?.gameObject?.SetActive(sound.Value);
        }
    }
}
