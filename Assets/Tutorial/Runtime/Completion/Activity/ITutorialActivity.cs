namespace Tutorial.Runtime.Activity
{
    public interface ITutorialActivity
    {
        void Trigger() {}
        void Skipped() {}
        void Raised() {}
    }
}
