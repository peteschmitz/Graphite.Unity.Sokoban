using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EnglishLocale : LocaleLookup
{
    public EnglishLocale() : base()
    {
        this.lookup = new Dictionary<LocaleTextType, string>
        {
            { LocaleTextType.Title, ""},
            { LocaleTextType.Menu, "Main Menu"},
            { LocaleTextType.Play, "Play"},
            { LocaleTextType.Create, "Create"},
            { LocaleTextType.MoreLevels, "More Levels"},
            { LocaleTextType.Okay, "Okay"},
            { LocaleTextType.Close, "Close"},
            { LocaleTextType.Easy, "Easy"},
            { LocaleTextType.Medium, "Medium"},
            { LocaleTextType.Hard, "Hard"},
            { LocaleTextType.Continue, "Continue"},
            { LocaleTextType.GamePaused, "Game Paused"},
            { LocaleTextType.RemoveAds, "Remove Ads"},
            { LocaleTextType.Purchase, "Purchase"},
            { LocaleTextType.Unlocked, "Unlocked!"},
            { LocaleTextType.LevelCompleted, "Level Completed!"},
            { LocaleTextType.Editor, "Editor"},
            { LocaleTextType.Next, "Next"},
            { LocaleTextType.Replay, "Replay"},
            { LocaleTextType.Steps, "Steps"},
            { LocaleTextType.Best, "Best"},
            { LocaleTextType.Time, "Time"},
            { LocaleTextType.Restoring, "Restoring..."},
            { LocaleTextType.RestoreFinished, "Restore Finished"},
            { LocaleTextType.Success, "Success"},
            { LocaleTextType.Failure, "Failure"},
            { LocaleTextType.RestorePurchases, "Restore Purchases"},
            { LocaleTextType.Boxes, "Boxes"},
            { LocaleTextType.CustomLevel, "Custom Level"},
            { LocaleTextType.PurchasedNote, "Purchase confirmed - thank you for supporting us!"}
        };
    }
}
