using System;
using System.Collections.Generic;

namespace Quantum
{
  unsafe partial class Frame
  {

        public Custom.Animator.AnimatorUpdater CustomAnimatorUpdater;

        partial void InitUser()
        {
            CustomAnimatorUpdater = new Custom.Animator.AnimatorUpdater();
        }
    }
}
