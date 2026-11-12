using PrimeTween;
using TMPro;
using UnityEngine;

public class DamageNumberEffect : MonoBehaviour
{
    //Create a DamageNumberEffect at the specified position with the given damage amount
    public static DamageNumberEffect Create(Vector3 position, int damageAmount)
    {
        GameObject prefab = Resources.Load<GameObject>("DamagePopUp");

        if (prefab == null)
        {
            Debug.LogError("DamagePopUp prefab not found in Resources");
            return null;
        }

        Transform effectTransform = Instantiate(prefab, position, Quaternion.identity).transform;
        DamageNumberEffect damageNumberEffect = effectTransform.GetComponent<DamageNumberEffect>();
        damageNumberEffect.Setup(damageAmount);
        return damageNumberEffect;
    }

    private TextMeshPro text;
    void Awake()
    {
        text = GetComponent<TextMeshPro>();
    }

    private void Start()
    {
        Animate();
    }

    void Animate()
    {
        transform.localScale = Vector3.zero;
        Tween.Scale(transform, Vector3.one, .5f, Ease.OutQuint);
        Tween.PositionY(transform, transform.position.y + .2f, 1f, Ease.OutQuint).OnComplete(() =>
        {
            Tween.Scale(transform, Vector3.zero, .5f, Ease.OutQuint);
            Tween.Alpha(text, 0f, .5f, Ease.OutQuint).OnComplete(() => { Destroy(gameObject); });

        });
    }

    public void Setup(int damageAmount)
    {
        text.SetText(damageAmount.ToString());
    }
}
