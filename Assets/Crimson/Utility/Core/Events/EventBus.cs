using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crimson.Core
{
    public static class EventBus
    {
        #region Public Fields
        #endregion

        #region Private Fields

        /// <summary>
        /// Container of all events
        /// </summary>
        private static Dictionary<Type, Delegate> events = new Dictionary<Type, Delegate>();

        #endregion

        #region MonoBehaviour Callbacks
        #endregion

        #region Public Methods

        /// <summary>
        /// Use to subscribe an action in the container
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listener"></param>
        public static void Subscribe<T>(Action<T> listener)
        {
            if (events.TryGetValue(typeof(T), out Delegate existing))
            {
                events[typeof(T)] = Delegate.Combine(existing, listener);
            }
            else
            {
                events[typeof(T)] = listener;
            }
        }

        /// <summary>
        /// Remove a listener in the container
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listener"></param>
        public static void Unsubscribe<T>(Action<T> listener)
        {
            if (events.TryGetValue(typeof(T), out Delegate existing))
            {
                events[typeof(T)] = Delegate.Remove(existing, listener);
            }

        }

        /// <summary>
        /// Use to create an event
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="eventData"></param>
        public static void Publish<T>(T eventData)
        {
            if (events.TryGetValue(typeof(T), out Delegate del))
            {
                (del as Action<T>)?.Invoke(eventData);
            }
        }

        #endregion

        #region Private Methods
        #endregion
    }
}
