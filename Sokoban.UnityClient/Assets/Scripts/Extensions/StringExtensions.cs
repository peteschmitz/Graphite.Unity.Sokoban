using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class StringExtensions
{
    public static bool IsValid(this string value)
    {
        return !string.IsNullOrEmpty(value);
    }

    public static bool IsInvalid(this string value)
    {
        return !IsValid(value);
    }

    public static string OnNullOrEmpty(this string value, string replacement)
    {
        return value.IsValid() ? value : replacement;
    }

    public static bool HasFileExtension(this string value) => HasFileExtension(value, "");
    public static bool HasFileExtension(this string value, string specificExtension)
    {
        var extension = value.GetFileExtension();
        return extension.IsValid() && (specificExtension.IsInvalid() || extension.ToLower() == specificExtension.ToLower());
    }

    public static string GetFileExtension(this string value)
    {
        if (value.IsInvalid())
        {
            return string.Empty;
        }
        var split = value.Split('/').Last();
        if (split.IsInvalid() || !split.Contains('.') || split.EndsWith("."))
        {
            return string.Empty;
        }
        var index = split.LastIndexOf('.') + 1;
        return split.Substring(index, split.Length - index);
    }

    public static string RemoveFileExtension(this string value)
    {
        return value.HasFileExtension() ? value.Substring(0, value.LastIndexOf('.')) : value;
    }

    public static string AsFileName(this string value)
    {
        var path = value.OnNullOrEmpty("");
        if (path.Contains("/"))
        {
            var index = path.LastIndexOf("/");
            if (index + 1 > path.Length)
            {
                path = path.Substring(index + 1, path.Length - (index + 1));
            }
        }
        if (path.Contains(".json"))
        {
            path.Replace(".json", "");
        }
        return path;
    }
}
