// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/Sentinel.cs

using System;
using UnityEngine;

namespace SelfAwareHR
{
    public class Sentinel : MonoBehaviour
    {
        public event Action onDisable;
        public event Action onEnable;
        public event Action onUpdate;
        public event Action<bool> onStatusChanged;

        public void OnEnable()
        {
            onEnable?.Invoke();
            onStatusChanged?.Invoke(true);
        }

        public void OnDisable()
        {
            onDisable?.Invoke();
            onStatusChanged?.Invoke(false);
        }

        public void Update()
        {
            onUpdate?.Invoke();
        }
    }
}