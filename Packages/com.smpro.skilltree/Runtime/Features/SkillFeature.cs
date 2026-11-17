using UnityEngine;

namespace SMPro.SkillTree
{
    public abstract class SkillFeature : MonoBehaviour
    {
        [SerializeField] string featureId;

        public string FeatureId => featureId;
        public bool IsUnlocked { get; private set; }

        protected virtual void OnEnable() => SkillFeatures.Register(this);

        protected virtual void OnDisable() => SkillFeatures.Unregister(this);

        protected virtual void OnUnlocked() { }

        protected virtual void OnLocked() { }

        internal void SetUnlocked(bool unlocked)
        {
            if (IsUnlocked == unlocked) return;
            IsUnlocked = unlocked;
            if (unlocked) OnUnlocked();
            else OnLocked();
        }
    }
}
