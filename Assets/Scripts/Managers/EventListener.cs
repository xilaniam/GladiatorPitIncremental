using UnityEngine;

public abstract class GameEventListener : MonoBehaviour
{
   protected abstract void SubscribeEvents();
   protected abstract void UnsubscribeEvents();
}
