using UnityEngine;

public static class GameObjectExtensions
{
    public static TComponent LazyGet<TComponent>(this Component component, ref TComponent backingField)
        where TComponent : Component
    {
        return (backingField = backingField ?? component.gameObject?.GetComponent<TComponent>() ?? component.gameObject?.GetComponentInChildren<TComponent>());
    }

    public static GameObject WithChild(this GameObject gameObject, string childName)
    {
        return gameObject?.transform.Find(childName)?.gameObject;
    }
}
