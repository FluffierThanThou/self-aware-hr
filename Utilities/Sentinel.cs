// Copyright Karel Kroeze, 2021-2021.
// SelfAwareHR/SelfAwareHR/Sentinel.cs

using System;
using UnityEngine;

namespace SelfAwareHR.Utilities
{
    public class Sentinel : MonoBehaviour
    {
        public event Action onDisable;

        public void OnDisable()
        {
            onDisable?.Invoke();
            onStatusChanged?.Invoke(false);
        }

        public event Action onEnable;

        public void OnEnable()
        {
            onEnable?.Invoke();
            onStatusChanged?.Invoke(true);
        }

        public event Action<bool> onStatusChanged;
        public event Action       onUpdate;

        public void Update()
        {
            onUpdate?.Invoke();
        }
    }
}