
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LocaleMapping
{
    private static readonly Dictionary<SystemLanguage, string> SupportedLocales =
        new Dictionary<SystemLanguage, string>()
        {
            { SystemLanguage.English, "english.json" },
            { SystemLanguage.Finnish, "finnish.json" },
            { SystemLanguage.French, "french.json" },
            { SystemLanguage.German, "german.json" },
            { SystemLanguage.Korean, "korean.json" },
            { SystemLanguage.Russian, "russian.json" },
            { SystemLanguage.Spanish, "spanish.json" },
            { SystemLanguage.Swedish, "swedish.json" }
        };

    public string[][] lookup { get; set; }

    public static string GetLocalePath(SystemLanguage language)
    {
        if (SupportedLocales.ContainsKey(language))
        {
            return SupportedLocales[language];
        }
        return null;
    }
}

public class LocaleLookup
{
    public bool HasItems => this.lookup.HasItems();

    protected Dictionary<LocaleTextType, string> lookup =
        new Dictionary<LocaleTextType, string>();

    private Dictionary<string, string> _stringLookup;
    private Dictionary<string, string> stringLookup
    {
        get
        {
            if (this._stringLookup == null)
            {
                this._stringLookup = Enum.GetValues(typeof(LocaleTextType))
                    .Cast<LocaleTextType>()
                    .Where(x => this.lookup.ContainsKey(x))
                    .ToDictionary(x => x.ToString(), x => this.lookup[x]);
            }
            return this._stringLookup;
        }
    }

    public LocaleLookup() : this(null) { }
    public LocaleLookup(LocaleMapping mapping)
    {
        if (mapping == null || !mapping.lookup.HasItems())
        {
            return;
        }
        foreach (var map in mapping.lookup)
        {
            if (!map.HasItems() || map.Length < 2)
            {
                continue;
            }
            if (Enum.TryParse(map[0], out LocaleTextType key))
            {
                if (!this.lookup.ContainsKey(key))
                {
                    this.lookup.Add(key, map[1]);
                }
            }

        }
    }

    public string Lookup(LocaleTextType type, string defaultValue)
    {
        return this.lookup.ContainsKey(type) ? this.lookup[type] : defaultValue;
    }

    public string StringLookup(string typeString, string defaultValue)
    {
        return this.stringLookup.ContainsKey(typeString) ? this.stringLookup[typeString] : defaultValue;
    }
}
