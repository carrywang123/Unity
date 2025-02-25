using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraRaycastHandler : MonoBehaviour, IPointerClickHandler
{
    public Camera mycamera;
    public RectTransform rawImageRect;

    public void OnPointerClick(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImageRect, eventData.position, eventData.pressEventCamera, out localPoint);

        // ����������ת��Ϊ��������
        float x = (localPoint.x - rawImageRect.rect.x) * (mycamera.pixelWidth / rawImageRect.rect.width);
        float y = (localPoint.y - rawImageRect.rect.y) * (mycamera.pixelHeight / rawImageRect.rect.height);

        Ray ray = mycamera.ScreenPointToRay(new Vector3(x, y, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            // �������¼�
            GameObject clickedObject = hit.collider.gameObject;
            IClickable clickable = clickedObject.GetComponent<IClickable>();
            if (clickable != null){
                clickable.OnClick();
            }
            Debug.Log("Clicked on: " + hit.collider.gameObject.name);
        }
    }
}