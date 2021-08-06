using System;
using System.Linq;
using UnityEditor;

namespace Unity.Play.Publisher.Editor
{
    /// <summary>
    /// Defines the structure of a method that can be called when an action is dispatched
    /// </summary>
    /// <param name="action">The action to dispatch</param>
    /// <returns></returns>
    public delegate object Dispatcher(object action);

    /// <summary>
    /// Defines the structure of a method that can be called when the State reducer
    /// </summary>
    /// <typeparam name="State"></typeparam>
    /// <param name="previousState"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public delegate State Reducer<State>(State previousState, object action);

    /// <summary>
    /// Defines the structure of a method that can be called in order to alter the state of the application
    /// </summary>
    /// <typeparam name="State"></typeparam>
    /// <param name="store"></param>
    /// <returns></returns>
    public delegate Func<Dispatcher, Dispatcher> Middleware<State>(Store<State> store);

    /// <summary>
    /// Defines the structure of a method that can be called when the state of an object changes
    /// </summary>
    /// <typeparam name="State"></typeparam>
    /// <param name="action"></param>
    public delegate void StateChangedHandler<State>(State action);

    /// <summary>
    /// Manages the communication between all the application components
    /// </summary>
    /// <typeparam name="State"></typeparam>
    public class Store<State>
    {
        /// <summary>
        /// Delegate that reacts on state change
        /// </summary>
        public StateChangedHandler<State> stateChanged;
        State _state;
        readonly Dispatcher _dispatcher;
        readonly Reducer<State> _reducer;

        /// <summary>
        /// Initializes and returns an instance of Store
        /// </summary>
        /// <param name="reducer"></param>
        /// <param name="initialState"></param>
        /// <param name="middlewares"></param>
        public Store(
            Reducer<State> reducer, State initialState = default(State),
            params Middleware<State>[] middlewares)
        {
            this._reducer = reducer;
            this._dispatcher = this.ApplyMiddlewares(middlewares);
            this._state = initialState;
        }

        /// <summary>
        /// Dispatches an action
        /// </summary>
        /// <param name="action"></param>
        /// <returns>Returns an object affected by the action</returns>
        public object Dispatch(object action)
        {
            return this._dispatcher(action);
        }

        /// <summary>
        /// The state
        /// </summary>
        public State state
        {
            get { return this._state; }
        }

        Dispatcher ApplyMiddlewares(params Middleware<State>[] middlewares)
        {
            return middlewares.Reverse().Aggregate<Middleware<State>, Dispatcher>(this.InnerDispatch,
                (current, middleware) => middleware(this)(current));
        }

        object InnerDispatch(object action)
        {
            this._state = this._reducer(this._state, action);

            if (this.stateChanged != null)
            {
                this.stateChanged(this._state);
            }

            return action;
        }
    }
}
