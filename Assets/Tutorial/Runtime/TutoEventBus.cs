using System;
using System.Collections.Generic;
using Tutorial.Runtime.Data;

namespace Tutorial.Runtime
{
    /// <summary>
    /// Class use as event to Raised the tutorial step
    /// </summary>
    public class OnRaised
    {
        public StepSO Step => step;
        public StepSequenceSO Sequence => sequence;

        /// <summary>
        /// Step we just raised to be completed
        /// </summary>
        private StepSO step = null; 

        /// <summary>
        /// Sequence we want to raise
        /// </summary>
        private StepSequenceSO sequence = null;

        /// <summary>
        /// Constructor with one step
        /// </summary>
        /// <param name="so"></param>
        public OnRaised(StepSO so)
        {
            step = so;
        }

        /// <summary>
        /// Constructor with a sequence of step
        /// </summary>
        /// <param name="list"></param>
        public OnRaised(StepSequenceSO _sequence)
        {
            sequence = _sequence;
        }
    }

    /// <summary>
    /// Class use as event to Skipped the tutorial step
    /// </summary>
    public class OnSkipped
    {
        public StepSO Step => step;
        public StepSequenceSO Sequence => sequence;

        /// <summary>
        /// Step we just raised to be completed
        /// </summary>
        private StepSO step = null;

        /// <summary>
        /// Sequence we want to raise
        /// </summary>
        private StepSequenceSO sequence = null;

        /// <summary>
        /// Constructor with one step
        /// </summary>
        /// <param name="so"></param>
        public OnSkipped(StepSO so)
        {
            step = so;
        }

        /// <summary>
        /// Constructor with a sequence of step
        /// </summary>
        /// <param name="list"></param>
        public OnSkipped(StepSequenceSO _sequence)
        {
            sequence = _sequence;
        }
    }

    /// <summary>
    /// Class use as event to Trigger the tutorial step
    /// </summary>
    public class OnTrigger
    {
        public StepSO Step => step;
        public StepSequenceSO Sequence => sequence;

        /// <summary>
        /// Step we just raised to be completed
        /// </summary>
        private StepSO step = null;

        /// <summary>
        /// Sequence we want to raise
        /// </summary>
        private StepSequenceSO sequence = null;

        /// <summary>
        /// Constructor with one step
        /// </summary>
        /// <param name="so"></param>
        public OnTrigger(StepSO so)
        {
            step = so;
        }

        /// <summary>
        /// Constructor with a sequence of step
        /// </summary>
        /// <param name="list"></param>
        public OnTrigger(StepSequenceSO _sequence)
        {
            sequence = _sequence;
        }
    }

    public static class TutoEventBus
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
