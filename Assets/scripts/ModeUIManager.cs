using UnityEngine;

public class ModeUIManager : MonoBehaviour
{
    public GameObject sharedUI;
    public GameObject trainingUI;
    public GameObject scoringUI;
    public GameObject gameObject;
    public GameObject gameObject1;

    public void ShowTrainingUI()
    {
        trainingUI.SetActive(true);
        scoringUI.SetActive(false);
        gameObject.SetActive(true);
        gameObject1.SetActive(false);
    }

    public void ShowScoringUI()
    {
        trainingUI.SetActive(false);
        scoringUI.SetActive(true);
        gameObject.SetActive(true);
        gameObject1.SetActive(false);
    }
}
