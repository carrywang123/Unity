using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShuicaoCollider : MonoBehaviour
{
    public GameObject step;
    public Vector3 position;
    public float rospeed;
    public float waterspeed;
    public marks marks;
    private wait currentwait;
    private ChangeTextColor changeTextColor;
    private int working;
    private float waternum;
    bool flag = false;
    // Start is called before the first frame update
    void Start()
    {
        changeTextColor = step.GetComponent<ChangeTextColor>();
    }

    // Update is called once per frame
    void Update()
    {
        if(working == 1)
        {
            Quaternion currentRotation = currentwait.gameObject.transform.rotation;
            Quaternion addedRotation = Quaternion.Euler(0, rospeed * Time.deltaTime, 0);
            currentwait.gameObject.transform.rotation = currentRotation * addedRotation;
            waternum -= waterspeed * Time.deltaTime;
            currentwait.settian(waternum);
            if (currentwait.gameObject.transform.rotation.y > 0.3f && !flag)
            {
                waterspeed *= 2;
                flag = true;
            }
            if (currentwait.gameObject.transform.rotation.y > 0.4f)
            {
                working++;
            }    
        }
        else if(working == 2)
        {
            if (waternum > 0.1f)
            {
                waternum -= waterspeed * Time.deltaTime;
                currentwait.settian(waternum);
                if (this.gameObject.tag == "烧杯")
                {
                    //水位非常缓慢地上升
                }
            }
            else
                working++;
        }
        else if(working == 3)
        {
            currentwait.tomyposition();
            currentwait.gameObject.transform.eulerAngles = new Vector3(337.55f, 343.408f, 127.96f);
            currentwait.Something = 6;
            working = 4;
            flag = false;
            waterspeed /= 2;
            changeTextColor.Changetext();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("比色皿"))
        {
            working = 1;
            currentwait = other.gameObject.GetComponent<wait>();
            currentwait.gameObject.transform.position = position;
            waternum = currentwait.gettian();
            currentwait.Something = 5;
            if (this.gameObject.tag != "烧杯")
                marks.ChangeAllOperationDefaltsFlag(1);
        }

    }
}
