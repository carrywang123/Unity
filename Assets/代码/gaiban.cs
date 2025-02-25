using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class gaiban : MonoBehaviour
{
    public GameObject step;
    public GameObject bochang;
    public Vector3[] position;
    public GameObject parent;
    public float speed;
    public float rospeed;
    public marks marks;
    public Button button1;
    private ChangeTextColor changeTextColor;

    public float timeInrerval = 0.2f;
    float lastTime;
    private wait currentwait;
    private ChangeTemperature changeTemperature;
    int working;
    //选择磨砂面
    public GameObject selectionPlace;
    public Vector3[] selectPosition;
    public Vector3[] selectRotation;
    int myselect;
    private int isSelecting; // 0:无 1:开  2:关
    void Start()
    {
        changeTextColor = step.GetComponent<ChangeTextColor>();
        this.gameObject.transform.parent = parent.transform;
        changeTemperature = bochang.GetComponent<ChangeTemperature>();
        isSelecting = 0;
        working = 0;
        lastTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if(parent != null)
        {
            if(working == 1)
            {
                parent.transform.eulerAngles = new Vector3(300, 87, 0);
                if (isSelecting == 0)
                {
                    working = -1;
                    button1.onClick.Invoke();
                }
                if(isSelecting == 2)
                {
                    currentwait.gameObject.transform.position = position[myselect];
                    working = 2;
                }
                
            }
            else if(working == 2)
            {
                if (currentwait.gameObject.transform.position.y > 1.35f)
                {
                    currentwait.gameObject.transform.position -= new Vector3(0, speed * Time.deltaTime, 0);
                }
                else
                {
                    working = 3;
                }
            }
            else if(working == 3)
            {
                parent.transform.eulerAngles = new Vector3(360, 87, 0);
                working = 4;
            }
            else if(working == 4)
            {
                //显示
                StartCoroutine(shownum());
            }
            else if(working == 5)
            {
                parent.transform.eulerAngles = new Vector3(300, 87, 0);
                working = 6;
            }
            else if(working == 6)
            {
                if (currentwait.gameObject.transform.position.y < 1.58f)
                {
                    currentwait.gameObject.transform.position += new Vector3(0, speed * Time.deltaTime, 0);
                }
                else
                {
                    working = 7;
                }
            }
            else if (working == 7)
            {
                parent.transform.eulerAngles = new Vector3(360, 87, 0);
                working = 8;
            }
            else if(working == 8)
            {
                currentwait.tomyposition();
                currentwait.Something = 4;
                working = 0;
                changeTextColor.Changetext();
            }
            if (isSelecting == 1)
            {
                currentwait.gameObject.transform.position = position[myselect];
                working = 2;
                isSelecting = 2;
            }
        }
    }

    private IEnumerator shownum()
    {
        yield return new WaitForSeconds(1f);
        if(Time.time - lastTime > timeInrerval)
        {
            if (changeTextColor.currenttext == 3)
                changeTemperature.changeresult(Random.Range(0.200f, 0.300f).ToString("F3"));
            else if (changeTextColor.currenttext == 7)
                changeTemperature.changeresult(Random.Range(marks.input1_guang_min, marks.input1_guang_max).ToString("F3"));
            else if (changeTextColor.currenttext == 11)
                changeTemperature.changeresult(Random.Range(marks.input2_guang_min, marks.input2_guang_max).ToString("F3"));
            else if (changeTextColor.currenttext == 15)
                changeTemperature.changeresult(Random.Range(marks.input3_guang_min, marks.input3_guang_max).ToString("F3"));
            else if (changeTextColor.currenttext == 19)
                changeTemperature.changeresult(Random.Range(marks.input4_guang_min, marks.input4_guang_max).ToString("F3"));
            working = 5;
            lastTime = Time.time;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("比色皿")&& working==0)
        {
            working = 1;
            currentwait = other.GetComponent<wait>();
            currentwait.Something = 3;
            if(isSelecting == 0)
            selectionPlace.SetActive(true);
            if (isSelecting == 2)
                currentwait.transform.eulerAngles = selectRotation[myselect];
        }
    }

    public void ChangeSelection(int val)
    {
        myselect = val;
        currentwait.transform.eulerAngles = selectRotation[myselect];
        currentwait.transform.position = selectPosition[val];
    }

    public void Confirm()
    {
        isSelecting = 1;
        selectionPlace.SetActive(false);
        if(myselect == 0)
        marks.ChangeAllOperationDefaltsFlag(0);
    }
}
