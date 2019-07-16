using System;

public class RelativePathAttribute : Attribute
{
    public string FileName;
    public string Directory;
    public bool IsResource;

    #region properties
    public string RelativeFileName
    {
        get
        {
            return $"{this.Directory}{this.FileName}";
        }
    }
    #endregion
}
