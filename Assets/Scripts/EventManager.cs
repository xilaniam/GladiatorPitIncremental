using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventManager 
{
    public static Action<List<Transform>> OnCircleTickEvent;
    public static Action<int> OnCoinDroppedEvent;
    public static void InvokeCircleTick(List<Transform> position)
    {
        OnCircleTickEvent?.Invoke(position);
    }
    public static void InvokeCoinDroppedEvent(int amount)
    {
        OnCoinDroppedEvent?.Invoke(amount);
    }
}
