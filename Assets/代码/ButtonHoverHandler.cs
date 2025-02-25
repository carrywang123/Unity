
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    Sprite sprite;
    Sprite originalSprite;
    Color color;
    bool flag;
    private void Start()
    {
        if (this.gameObject.tag == "���ӽ�")
            flag = true;
        else
            flag = false;
        sprite = Resources.Load<Sprite>("2");
    }

    public bool ChangeFlag(bool flag)
    {
        this.flag = flag;
        if (this.flag == flag)
            return true;
        else
            return false;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if(!flag)
        {
            originalSprite = GetComponent<Button>().image.sprite;
            // �ı䰴ť����ʽ
            GetComponent<Button>().image.sprite = sprite;
            Transform childTransform = transform.Find("Text (TMP)");
            if (childTransform != null)
            {
                color = childTransform.gameObject.GetComponent<TMP_Text>().color;
                childTransform.gameObject.GetComponent<TMP_Text>().color = Color.white;
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(!flag)
        {
            // �ָ���ť����ʽ
            GetComponent<Button>().image.sprite = originalSprite;
            Transform childTransform = transform.Find("Text (TMP)");
            if (childTransform != null)
            {
                childTransform.gameObject.GetComponent<TMP_Text>().color = color;
            }
        }
        
    }
}
