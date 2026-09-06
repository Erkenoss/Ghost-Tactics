namespace Tutorial
{
    /// <summary>
    /// Contains the central types shared by the entire Tutorial framework.
    /// This file must remain independent from Editor, scene and UI systems.
    /// </summary>

    #region Step

    public enum EStepType
    {
        None = 0,
        Gameplay = 1,
        UI = 2,
    }

    public enum ESequenceType
    {
        None = 0,
        Linear = 1,
        Random = 2,
    }

    #endregion

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

    #region Step Runner

    /// <summary>
    /// Current lifecycle status of a tutorial Step runner
    /// </summary>
    public enum ETutorialStepRunnerStatus
    {
        Created = 0,
        WaitingForTrigger = 1,
        Running = 2,
        Completed = 3,
        Skipped = 4,
        Disposed = 5
    }

    #endregion

    #region Sequence Runner

    /// <summary>
    /// Current lifecycle status of a tutorial sequence runner
    /// </summary>
    public enum ETutorialSequenceRunnerStatus
    {
        Created = 0,
        WaitingForStep = 1,
        Running = 2,
        Completed = 3,
        Skipped = 4,
        Failed = 5,
        Disposed = 6
    }

    #endregion

    #region Tutorial Runner

    /// <summary>
    /// Current lifecycle status of the tutorial runtime runner
    /// </summary>
    public enum ETutorialRunnerStatus
    {
        Created = 0,
        Running = 1,
        WaitingForDependencies = 2,
        Completed = 3,
        Failed = 4,
        Disposed = 5
    }

    #endregion
}