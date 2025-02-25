using UnityEngine;
using UnityEngine.EventSystems;

public class ResizableRawImage : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public RectTransform rawImageRect;
    private Vector2 originalMousePosition;
    private Vector2 originalSizeDelta;
    private Vector2 originalAnchoredPosition;
    private float aspectRatio;

    void Start()
    {
        if (rawImageRect == null)
        {
            rawImageRect = GetComponent<RectTransform>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // ��¼��ʼ���λ�á�RawImage �Ĵ�С��λ�úͿ�߱�
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImageRect, eventData.position, eventData.pressEventCamera, out originalMousePosition);
        originalSizeDelta = rawImageRect.sizeDelta;
        originalAnchoredPosition = rawImageRect.anchoredPosition;
        aspectRatio = originalSizeDelta.x / originalSizeDelta.y;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // ��������ƶ���ƫ����
        Vector2 localMousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImageRect, eventData.position, eventData.pressEventCamera, out localMousePosition);
        Vector2 offset = localMousePosition - originalMousePosition;

        // ȷ�����ƫ���������ȱ�������
        float maxOffset = Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
        Vector2 newSizeDelta;
        if (Mathf.Abs(offset.x) == maxOffset)
            newSizeDelta = new Vector2(originalSizeDelta.x - offset.x, originalSizeDelta.y - offset.x / aspectRatio);
        else
            newSizeDelta = new Vector2(originalSizeDelta.x - offset.y, originalSizeDelta.y - offset.y / aspectRatio);


        // ȷ�� RawImage �Ŀ�߲�Ϊ��ֵ
        if (newSizeDelta.x < 0)
        {
            newSizeDelta.x = 0;
        }
        if (newSizeDelta.y < 0)
        {
            newSizeDelta.y = 0;
        }

        // �����µ�λ��
        Vector2 newAnchoredPosition = new Vector2(
            originalAnchoredPosition.x - (newSizeDelta.x - originalSizeDelta.x) / 2,
            originalAnchoredPosition.y + (newSizeDelta.y - originalSizeDelta.y) / 2
        );

        // Ӧ���µĴ�С��λ��
        rawImageRect.sizeDelta = newSizeDelta;
        rawImageRect.anchoredPosition = newAnchoredPosition;
    }
}
