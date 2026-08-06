namespace Tutorial
{
    /// <summary>
    /// Contains the central types shared by the entire Tutorial framework.
    /// This file must remain independent from Editor, scene and UI systems.
    /// </summary>

    #region Progress

    /// <summary>
    /// Persistent progression status of a tutorial for a player
    /// </summary>
    public enum ETutorialProgressStatus
    {
        /// <summary>
        /// The tutorial has never been started
        /// </summary>
        NotStarted = 0,

        /// <summary>
        /// The tutorial has been started but is not completed
        /// </summary>
        InProgress = 1,

        /// <summary>
        /// The tutorial has been completed
        /// </summary>
        Completed = 2
    }

    #endregion

    #region Replay

    /// <summary>
    /// Defines whether a completed tutorial can be played again
    /// </summary>
    public enum ETutorialReplayPolicy
    {
        /// <summary>
        /// A completed tutorial cannot be started again
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// A completed tutorial can be started again
        /// </summary>
        Allowed = 1
    }

    #endregion

    #region Runtime Instance

    /// <summary>
    /// Current lifecycle status of a tutorial runtime instance
    /// </summary>
    public enum ETutorialRuntimeInstanceStatus
    {
        Created = 0,
        Ready = 1,
        Running = 2,
        Completed = 3,
        Failed = 4,
        Disposed = 5
    }

    #endregion
}