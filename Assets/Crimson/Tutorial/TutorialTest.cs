using UnityEngine;
using Tutorial.Runtime.Activity;

namespace Crimson.Tutorial
{
    public sealed class TutorialDebugActivity : TutorialActivity
    {
        public override void Trigger()
        {
            Debug.Log("ACTIVITY TRIGGER", this);
        }

        public override void Raised()
        {
            Debug.Log("ACTIVITY RAISED", this);
        }

        public override void Skipped()
        {
            Debug.Log("ACTIVITY SKIPPED", this);
        }
    }
}