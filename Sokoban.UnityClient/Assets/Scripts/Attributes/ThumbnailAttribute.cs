using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
public class ThumbnailAttribute : Attribute
{
    /// <summary>
    /// Identifier useful when multiple sprite attributes are used
    /// </summary>
    public string Key { get; set; }
    public string Name { get; set; }
}
