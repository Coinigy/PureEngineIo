using PureEngineIo.Interfaces;
using System;
using System.Collections.Immutable;

namespace PureEngineIo.EmitterImp
{
    /// <summary>
    /// The event emitter which is ported from the JavaScript module.
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/component/emitter">https://github.com/component/emitter</see>
    /// </remarks>
    public class Emitter
    {
        private ImmutableDictionary<string, ImmutableList<IListener>> callbacks;
        private ImmutableDictionary<IListener, IListener> _onceCallbacks;

        public Emitter() => Off();

        /// <summary>
        /// Executes each of listeners with the given args.
        /// </summary>
        /// <param name="eventString">an event name.</param>
        /// <param name="args"></param>
        /// <returns>a reference to this object.</returns>
        public virtual Emitter Emit(string eventString, params object[] args)
        {
            if (callbacks.ContainsKey(eventString))
            {
                try
                {
                    //handle in try/catch the emit
                    foreach (var fn in callbacks[eventString])
                    {
                        fn.Call(args);
                    }
                }
                catch { }
            }
            return this;
        }

        /// <summary>
        ///  Listens on the event.
        /// </summary>
        /// <param name="eventString">event name</param>
        /// <param name="fn"></param>
        /// <returns>a reference to this object</returns>
        public Emitter On(string eventString, IListener fn)
        {
            if (!callbacks.ContainsKey(eventString))
            {
                callbacks = callbacks.Add(eventString, ImmutableList<IListener>.Empty);
            }
            var callbacksLocal = callbacks[eventString];
            callbacksLocal = callbacksLocal.Add(fn);
            callbacks = callbacks.Remove(eventString).Add(eventString, callbacksLocal);
            return this;
        }

        /// <summary>
        ///  Listens on the event.
        /// </summary>
        /// <param name="eventString">event name</param>
        /// <param name="fn"></param>
        /// <returns>a reference to this object</returns>
        public Emitter On(string eventString, Action fn) => On(eventString, new ListenerImpl(fn));

        /// <summary>
        ///  Listens on the event.
        /// </summary>
        /// <param name="eventString">event name</param>
        /// <param name="fn"></param>
        /// <returns>a reference to this object</returns>
        public Emitter On(string eventString, Action<object> fn) => On(eventString, new ListenerImpl(fn));

        /// <summary>
        /// Adds a one time listener for the event.
        /// </summary>
        /// <param name="eventString">an event name.</param>
        /// <param name="fn"></param>
        /// <returns>a reference to this object</returns>
        public Emitter Once(string eventString, IListener fn)
        {
            var on = new OnceListener(eventString, fn, this);

            _onceCallbacks = _onceCallbacks.Add(fn, on);
            On(eventString, on);
            return this;
        }

        /// <summary>
        /// Adds a one time listener for the event.
        /// </summary>
        /// <param name="eventString">an event name.</param>
        /// <param name="fn"></param>
        /// <returns>a reference to this object</returns>
        public Emitter Once(string eventString, Action fn) => Once(eventString, new ListenerImpl(fn));

        /// <summary>
        /// Removes all registered listeners.
        /// </summary>
        /// <returns>a reference to this object.</returns>
        public Emitter Off()
        {
            callbacks = ImmutableDictionary.Create<string, ImmutableList<IListener>>();
            _onceCallbacks = ImmutableDictionary.Create<IListener, IListener>();
            return this;
        }

        /// <summary>
        /// Removes all listeners of the specified event.
        /// </summary>
        /// <param name="eventString">an event name</param>
        /// <returns>a reference to this object.</returns>
        public Emitter Off(string eventString)
        {
            try
            {
                if (!callbacks.TryGetValue(eventString, out ImmutableList<IListener> retrievedValue))
                {
                    Logger.Log($"Emitter.Off Could not remove {eventString}");
                }

                if (retrievedValue != null)
                {
                    callbacks = callbacks.Remove(eventString);

                    foreach (var listener in retrievedValue)
                    {
                        _onceCallbacks.Remove(listener);
                    }
                }
            }
            catch (Exception)
            {
                Off();
            }

            return this;
        }

        /// <summary>
        /// Removes the listener
        /// </summary>
        /// <param name="eventString">an event name</param>
        /// <param name="fn"></param>
        /// <returns>a reference to this object.</returns>
        public Emitter Off(string eventString, IListener fn)
        {
            try
            {
                if (callbacks.ContainsKey(eventString))
                {
                    var callbacksLocal = callbacks[eventString];
                    _onceCallbacks.TryGetValue(fn, out var offListener);
                    _onceCallbacks = _onceCallbacks.Remove(fn);

                    if (callbacksLocal.Count > 0 && callbacksLocal.Contains(offListener ?? fn))
                    {
                        callbacksLocal = callbacksLocal.Remove(offListener ?? fn);
                        callbacks = callbacks.Remove(eventString);
                        callbacks = callbacks.Add(eventString, callbacksLocal);
                    }
                }
            }
            catch (Exception)
            {
                Off();
            }

            return this;
        }

        /// <summary>
        ///  Returns a list of listeners for the specified event.
        /// </summary>
        /// <param name="eventString">an event name.</param>
        /// <returns>a reference to this object</returns>
        public ImmutableList<IListener> Listeners(string eventString)
        {
            if (callbacks.ContainsKey(eventString))
            {
                var callbacksLocal = callbacks[eventString];
                return callbacksLocal ?? ImmutableList<IListener>.Empty;
            }
            return ImmutableList<IListener>.Empty;
        }

        /// <summary>
        /// Check if this emitter has listeners for the specified event.
        /// </summary>
        /// <param name="eventString">an event name</param>
        /// <returns>bool</returns>
        public bool HasListeners(string eventString) => Listeners(eventString).Count > 0;
    }
}
