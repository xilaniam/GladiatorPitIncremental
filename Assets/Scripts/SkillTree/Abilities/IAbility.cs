using UnityEngine;

public interface IAbility 
{
    void Enable(SkillContext context);
    void Disable();
}
