using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AnimationEvents : BaseBehavior
{
    public class Event : UnityEvent<AnimationEvent> { }

    public Event OnAnimationEndEvent = new Event();
    public Event OnAnimationStartEvent = new Event();

    public void OnAnimationEnd(AnimationEvent animationEvent)
    {
        this.OnAnimationEndEvent?.Invoke(animationEvent);
    }

    public void OnAnimationStart(AnimationEvent animationEvent)
    {
        this.OnAnimationStartEvent?.Invoke(animationEvent);
    }
}
