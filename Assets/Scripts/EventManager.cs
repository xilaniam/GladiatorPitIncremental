using System;
using System.Collections.Generic;
using UnityEngine;

public static class EventManager 
{
    public static Action<List<Transform>> OnCircleTickEvent;
    public static Action<int> OnCoinDroppedEvent;
    public static Action<int> OnBalanceUpdatedEvent;

    //Enemy
    public static Action<Enemy> OnEnemyDeadEvent;
    public static Action<Enemy> OnEnemyDamagedEvent;

    public static void InvokeEnemyDeadEvent(Enemy enemy)
    {
        OnEnemyDeadEvent?.Invoke(enemy);
    }
    public static void InvokeEnemyDamagedEvent(Enemy damageable)
    {
        OnEnemyDamagedEvent?.Invoke(damageable);
    }
    public static void InvokeCircleTick(List<Transform> position)
    {
        OnCircleTickEvent?.Invoke(position);
    }
    public static void InvokeCoinDroppedEvent(int amount)
    {
        OnCoinDroppedEvent?.Invoke(amount);
    }
    public static void InvokeBalanceUpdatedEvent(int amount)
    {
        OnBalanceUpdatedEvent?.Invoke(amount);
    }
}
