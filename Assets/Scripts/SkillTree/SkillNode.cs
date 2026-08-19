using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SkillNode : MonoBehaviour
{
    [SerializeField] RectTransform rectTransform;
    [SerializeField] List<SkillNode> connectedNodes = new List<SkillNode>();
    [SerializeField] GameObject connectorLinePrefab;
    [SerializeField] int maxLevel = 3;
    [SerializeField] int requiredLevelToExpand = 1;
    public Vector2Int PositionRelativeToParent;

    int currentLevel = 0;
    Button button;
    float offset = 256;
    void Start()
    {
        button = GetComponent<Button>();

        button.onClick.AddListener(ApplyUpgrade);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(ApplyUpgrade);
    }

    void ApplyUpgrade()
    {
        currentLevel++;
        if(currentLevel == requiredLevelToExpand) ExpandSkillNodes();
        //Here do what this skill does, like increasing damage, health, etc.
    }

    void ExpandSkillNodes()
    {
        if(connectedNodes.Count == 0) return;
        foreach (SkillNode node in connectedNodes)
        {
            float xPos = rectTransform.anchoredPosition.x + node.PositionRelativeToParent.x * offset;
            float yPos = rectTransform.anchoredPosition.y + node.PositionRelativeToParent.y * offset;
            node.SetNodePosition(xPos, yPos);
            CreateConnectionTo(node);
        }
    }

    void CreateConnectionTo(SkillNode node)
    {
        float x = 0;
        float y = 0;
        float rotation = 0;
        if(node.PositionRelativeToParent.x != 0)
        {
            x = (rectTransform.anchoredPosition.x + node.rectTransform.anchoredPosition.x) / 2;
        }
        if(node.PositionRelativeToParent.y != 0)
        {
            y = (rectTransform.anchoredPosition.y + node.rectTransform.anchoredPosition.y) / 2;
            rotation = 90;
        }

        GameObject line = Instantiate(connectorLinePrefab, transform.parent);
        line.transform.SetAsFirstSibling();
        line.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
        line.GetComponent<RectTransform>().rotation = Quaternion.Euler(0, 0, rotation);
    }

    public void SetNodePosition(float x , float y)
    {
        rectTransform.anchoredPosition = new Vector2(x, y);
        gameObject.SetActive(true);
    }
}
