using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public class AlertIAP : Alert
{
    private static readonly string[] IgnoredErrors = new string[]
    {
        "PurchasingUnavailable"
    };

    private IAPButton iapButton;
    public Text iapButtonText;
    public LocaleTextType buttonString;
    public LocaleTextType buttonStringComplete;
    public Text iapStatusText;
    public LocaleTextType successString;
    public Color iapSuccessColor;
    public Color iapErrorColor;
    public IAPManager iapManager;

    private string activeGrantRequest { get; set; }

    protected override void Start()
    {
        base.Start();
        this.iapButton = this.gameObject.GetComponentInChildren<IAPButton>();

        iapManager.OnGrantEvent.AddListener((grant, fromLocal) =>
        {
            if (GameContext.IsNavigationEnabled && this.IsActive)
            {
                this.SelectDefault();
            }
            if (activeGrantRequest.IsValid() && activeGrantRequest.Equals(grant))
            {
                this.IAPConfirmed();
            }
        });
        iapManager.OnGrantFailedEvent.AddListener((grant, reason) =>
        {
            if (GameContext.IsNavigationEnabled && this.IsActive)
            {
                this.SelectDefault();
            }
            if (activeGrantRequest.IsValid() && activeGrantRequest.Equals(grant))
            {
                this.IAPError(reason);
            }
        });
    }

    public override void SelectDefault()
    {
        this.EventSystem.SetSelectedGameObject(this.acceptObject);
    }

    protected override void OnResult(ResultType result)
    {
        if (result == ResultType.Accepted && this.activeGrantRequest.IsValid())
        {
            this.PlaySfx();
            return;
        }
        this.iapButton.productId = "";
        this.SetButtonAction(false);
        this.activeGrantRequest = "";
        base.OnResult(result);
    }

    public void PromptIap(string iapKey)
    {

        // AnalyticsManager.Event(() => AnalyticsEvent.ScreenVisit("iapMenu", new Dictionary<string, object>
        // {
        //     {"iapKey", iapKey}
        // }));
        Debug.Log("AlertIAP-> Prompt requested for " + iapKey);

        this.lastResult = null;
        var hasGrant = this.iapManager.HasGrant(iapKey);
        this.iapButton.productId = iapKey;
        // this.iapButton.SetProduct(this.iapManager.GetProduct(iapKey), (result) => Debug.Log("IAP Ui callback: " + result));
        this.activeGrantRequest = hasGrant ? "" : iapKey;
        this.iapButtonText.text = this.Locale.Get(this.buttonString);
        this.SetButtonAction(!hasGrant);
        this.SetText("", this.iapSuccessColor);

        // this.SetInvisible();
        this.ToggleActive(true);
        if (this.animationObject != null)
        {
            this.animationObject.Play(PanelAnimationNames.BounceIn);
        }
    }

    private void IAPConfirmed()
    {
        this.activeGrantRequest = null;
        this.SetButtonAction(false);
        this.SetText(this.Locale.Get(this.successString), this.iapSuccessColor);
    }

    private void IAPError(string reason)
    {
        IgnoredErrors.ForEach(ignoredError =>
        {
            if (ignoredError.Equals(reason))
            {
                reason = "";
            }
        });
        this.SetText(reason, this.iapErrorColor);
    }

    private void SetText(string text, Color textColor)
    {
        if (this.iapStatusText != null)
        {
            iapStatusText.text = text;
            iapStatusText.color = textColor;
        }
    }

    private void SetButtonAction(bool isPurchasable)
    {
        this.iapButtonText.text = this.Locale.Get(isPurchasable ? this.buttonString : this.buttonStringComplete);
        // this.iapButton.hasPurchased = !isPurchasable;
    }
}
