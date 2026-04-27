using UnityEngine;

namespace game_1
{
    public class ClosePanel : MonoBehaviour
    {
        public GameObject panelToClose;

        public void Close()
        {
            panelToClose.SetActive(false);
        }
    }
}
