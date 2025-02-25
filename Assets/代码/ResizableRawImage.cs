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
        // 记录初始鼠标位置、RawImage 的大小、位置和宽高比
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImageRect, eventData.position, eventData.pressEventCamera, out originalMousePosition);
        originalSizeDelta = rawImageRect.sizeDelta;
        originalAnchoredPosition = rawImageRect.anchoredPosition;
        aspectRatio = originalSizeDelta.x / originalSizeDelta.y;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 计算鼠标移动的偏移量
        Vector2 localMousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImageRect, eventData.position, eventData.pressEventCamera, out localMousePosition);
        Vector2 offset = localMousePosition - originalMousePosition;

        // 确保最大偏移量，并等比例调整
        float maxOffset = Mathf.Max(Mathf.Abs(offset.x), Mathf.Abs(offset.y));
        Vector2 newSizeDelta;
        if (Mathf.Abs(offset.x) == maxOffset)
            newSizeDelta = new Vector2(originalSizeDelta.x - offset.x, originalSizeDelta.y - offset.x / aspectRatio);
        else
            newSizeDelta = new Vector2(originalSizeDelta.x - offset.y, originalSizeDelta.y - offset.y / aspectRatio);


        // 确保 RawImage 的宽高不为负值
        if (newSizeDelta.x < 0)
        {
            newSizeDelta.x = 0;
        }
        if (newSizeDelta.y < 0)
        {
            newSizeDelta.y = 0;
        }

        // 计算新的位置
        Vector2 newAnchoredPosition = new Vector2(
            originalAnchoredPosition.x - (newSizeDelta.x - originalSizeDelta.x) / 2,
            originalAnchoredPosition.y + (newSizeDelta.y - originalSizeDelta.y) / 2
        );

        // 应用新的大小和位置
        rawImageRect.sizeDelta = newSizeDelta;
        rawImageRect.anchoredPosition = newAnchoredPosition;
    }
}
