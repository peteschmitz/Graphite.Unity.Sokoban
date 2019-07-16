using System;

[AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
public class SpriteAttribute : Attribute
{
    /// <summary>
    /// Identifier useful when multiple sprite attributes are used
    /// </summary>
    public string Key { get; set; }
    public string Name { get; set; }
    public string NamePattern { get; set; }
}
