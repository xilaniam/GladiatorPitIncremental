using System.Collections;
using SMPro.SkillTree;
using TMPro;
using UnityEngine;

namespace SMPro.SkillTree.Samples
{
    public class BerserkFeature : SkillFeature
    {
        [SerializeField] StatRef damage;
        [SerializeField] TMP_Text statusLabel;
        [SerializeField] float interval = 6f;
        [SerializeField] float duration = 2f;
        [SerializeField] float damageFactor = 1.5f;

        Coroutine loop;

        protected override void OnUnlocked()
        {
            loop = StartCoroutine(RageLoop());
            SetStatus("Berserk: ready");
        }

        protected override void OnLocked()
        {
            if (loop != null) StopCoroutine(loop);
            loop = null;
            damage.RemoveModifiers(this);
            SetStatus("");
        }

        IEnumerator RageLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(interval);
                damage.AddTemporary(ModifierKind.Multiply, damageFactor, duration, this);
                SetStatus($"Berserk: x{damageFactor} damage!");
                yield return new WaitForSeconds(duration);
                SetStatus("Berserk: ready");
            }
        }

        void SetStatus(string text)
        {
            if (statusLabel != null) statusLabel.text = text;
        }
    }
}
