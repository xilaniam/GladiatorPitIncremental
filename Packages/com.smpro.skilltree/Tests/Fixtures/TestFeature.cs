namespace SMPro.SkillTree.Tests
{
    public class TestFeature : SkillFeature
    {
        public int unlockCount;
        public int lockCount;

        protected override void OnUnlocked() => unlockCount++;

        protected override void OnLocked() => lockCount++;
    }
}
