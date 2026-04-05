using System;
using System.Collections.Generic;
using UnityEngine;

namespace _02._Scripts.ReactionSystem.Event
{
    public class EventManager
    {
        private Dictionary<Type, Delegate> eventTable = new Dictionary<Type, Delegate>();

        /// <summary>
        /// 이벤트 등록
        /// </summary>
        public void AddListener<T>(Action<T> listener) where T : EventArgs
        {
            var eventType = typeof(T);
            if (eventTable.TryGetValue(eventType, out var existingDelegate))
            {
                eventTable[eventType] = Delegate.Combine(existingDelegate, listener);
            }
            else
            {
                eventTable[eventType] = listener;
                Debug.LogWarning($"[ventManager] Created new event listener for {eventType.Name}");
            }
        }

        /// <summary>
        /// 이벤트 해제
        /// </summary>
        public void RemoveListener<T>(Action<T> listener) where T : EventArgs
        {
            var eventType = typeof(T);
            if (eventTable.TryGetValue(eventType, out var existingDelegate))
            {
                var current = Delegate.Remove(existingDelegate, listener);
                if (current == null)
                {
                    eventTable.Remove(eventType);
                }
                else
                {
                    eventTable[eventType] = current;
                    Debug.LogWarning($"[EventManager] Unregistered listener for {eventType.Name}");
                }
            }
        }

        /// <summary>
        /// 이벤트 호출
        /// </summary>
        public void CallEvent<T>(T eventArgs) where T : EventArgs
        {
            BroadCastToListeners(eventArgs);
        }

        void BroadCastToListeners<T>(T eventArgs) where T : EventArgs
        {
            var eventType = eventArgs.GetType();

            if (eventTable.TryGetValue(eventType, out var del))
            {
                try
                {
                    del.DynamicInvoke(eventArgs);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventManager] Error invoking event {eventType.Name}: {ex}");
                }
            }
        }
    }
}