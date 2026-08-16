using System;
using UnityEngine;

public static class EventManager 
{
    public static Action<Vector3> OnCircleTickAction;

    public static void InvokeCircleTick(Vector3 position)
    {
        OnCircleTickAction?.Invoke(position);
    }
}
