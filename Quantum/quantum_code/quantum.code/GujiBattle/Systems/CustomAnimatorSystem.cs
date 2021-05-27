namespace Quantum
{
    public unsafe partial class CustomAnimatorSystem : SystemMainThread, ISignalOnComponentAdded<CustomAnimator>, ISignalOnComponentRemoved<CustomAnimator>

    {
        public override void Update(Frame f)
        {
            f.CustomAnimatorUpdater.Update(f);
        }

        public void OnAdded(Frame f, EntityRef entity, CustomAnimator* component)
        {
            if (component->animatorGraph.Id != default)
            {
                var animatorGraphAsset = f.FindAsset<CustomAnimatorGraph>(component->animatorGraph.Id);
                CustomAnimator.SetCustomAnimatorGraph(f, component, animatorGraphAsset);
            }
        }

        public void OnRemoved(Frame f, EntityRef entity, CustomAnimator* component)
        {
            if (component->AnimatorVariables.Ptr != default)
            {
                f.FreeList(component->AnimatorVariables);
            }

            if (component->TriggeredStateBehaviours.Ptr != default)
            {
                f.FreeList(component->TriggeredStateBehaviours);
            }
        }


    }
}