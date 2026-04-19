using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace game_2
{
public class ChangePosition : MonoBehaviour
{
    public GameObject step;
    public Vector3 firstposition;
    public Vector3 position1;
    public Vector3 position2;
    public Vector3 position3;
    public Vector3 position4;

    public Vector3 firstrotation;
    public Vector3 rotation1;
    public Vector3 rotation2;
    public Vector3 rotation3;
    public Vector3 rotation4;

    public GameObject qidon;

    // Start is called before the first frame update

    private ChangeTextColor changetextcolor;

    private void Start()
    {
        changetextcolor = step.GetComponent<ChangeTextColor>();
    }
    public void Tofirstposition()
    {
        transform.position = firstposition;
        transform.eulerAngles = firstrotation;
        CloseAll();
    }

    public void Toposition1()
    {
        transform.position = position1;
        transform.eulerAngles = rotation1;
        CloseAll();
    }
    public void Toposition2()
    {
        transform.position = position2;
        transform.eulerAngles = rotation2;
        CloseAll();
    }
    public void Toposition3()
    {
        showqidon();
        transform.position = position3;
        transform.eulerAngles = rotation3;
        if (changetextcolor.currenttext == 0)
            changetextcolor.Changetext();
    }
    public void Toposition4()
    {
        transform.position = position4;
        transform.eulerAngles = rotation4;
        CloseAll();
    }

    private void showqidon()
    {
        qidon.SetActive(true);
    }
    private void CloseAll()
    {
        qidon.SetActive(false);
    }

    public void Quitgame()
    {
        Application.Quit();
    }

    /// <summary>返回主场景（不上传成绩；上传请在 marks 的 exit1/exit2 完成）。</summary>
    public void ReStart()
    {
        SceneManager.LoadScene("Main");
    }
}
}
