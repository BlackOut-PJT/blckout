using UnityEngine;
using System;
using UnityEngine.Events;

//
[Serializable]
public class LockStateChangedEvent : UnityEvent<bool, int>
{
}
