using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class LocaleManager : BaseBehavior
{
    private static LocaleManager _instance;

    private LocaleLookup activeLocale;
    private SystemLanguage? activeLanguage;

    // Start is called before the first frame update
    protected override async void Start()
    {
        base.Start();
        if (_instance != null && _instance.isActiveAndEnabled)
        {
            GameObject.Destroy(this);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this);
        }
        await this.PrepareLocale();
    }

    void OnDestroy()
    {
        this.enabled = false;
    }

    public string Get(LocaleTextType textType, string defaultText = "")
    {
        // await this.PrepareLocale();
        var locale = this.GetLocaleLookup();
        return locale.Lookup(textType, defaultText);
    }

    public string Get(string textTypeString)
    {
        // await this.PrepareLocale();
        var locale = this.GetLocaleLookup();
        return locale.StringLookup(textTypeString, textTypeString);
    }

    private async Task PrepareLocale()
    {
        var targetLanguage = Application.systemLanguage;
        // var targetLanguage = SystemLanguage.English;
        // var targetLanguage = SystemLanguage.Finnish;
        // var targetLanguage = SystemLanguage.French;
        // var targetLanguage = SystemLanguage.German;
        // var targetLanguage = SystemLanguage.Korean;
        // var targetLanguage = SystemLanguage.Russian;
        // var targetLanguage = SystemLanguage.Spanish;
        // var targetLanguage = SystemLanguage.Swedish;
        if (this.activeLocale != null &&
            activeLanguage.HasValue &&
            activeLanguage.Value == targetLanguage)
        {
            return;
        }

        this.activeLanguage = targetLanguage;
        this.activeLocale = await this.LoadLocale(this.activeLanguage.Value);
        Debug.Log($"LocaleManager.PrepareLocale-> Set to {targetLanguage.ToString()}, found lookup: {this.activeLocale != null}");
        if (this.activeLocale == null || !this.activeLocale.HasItems)
        {
            this.activeLocale = new EnglishLocale();
        }
    }

    private async Task<LocaleLookup> LoadLocale(SystemLanguage language)
    {
        if (language == SystemLanguage.English)
        {
            return new EnglishLocale();
        }

        try
        {
            var localePath = LocaleMapping.GetLocalePath(language);
            if (String.IsNullOrEmpty(localePath))
            {
                return null;
            }
            var mapping = await this.File.GetStreamedFile<LocaleMapping>(localePath);
            return new LocaleLookup(mapping);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
        return null;
    }

    private LocaleLookup GetLocaleLookup()
    {
        return this.activeLocale;
    }
}
