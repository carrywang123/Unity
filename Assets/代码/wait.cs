using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class wait : MonoBehaviour
{
    public GameObject water;
    public Vector3[] myposition;
    public Vector3 myposition1;
    public Vector3 myposition2;
    public Vector3 myposition3;
    private Quaternion myRotation;
    public UnityEngine.UI.Button[] Cpositionall;
    private Collider collider;
    private Material watermaterial;
    private float lastCallTime;
    private float callInterval = 1f;
    private int Cposition;
    private int something;

    private float waternum;
    
    public int Something { get => something; set => something = value; }

    // Start is called before the first frame update
    void Start()
    {
        myposition1 = myposition[1];
        watermaterial = water.GetComponent<Renderer>().material;
        collider = GetComponent<Collider>();
        Cposition = 1;
        something = 0;
        myRotation = this.gameObject.transform.rotation;
    }

    private void OnMouseDrag()
    {
        collider.enabled = false;
        // �����������ľ������������겢�������弴�������ƶ�������λ��
        Vector3 newPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.WorldToScreenPoint(transform.position).z));
        transform.position = newPosition;
        if (Input.mousePosition.x < Screen.width / 20)
        {
            if (Time.time - lastCallTime >= callInterval && Cposition > 0)
            {
                lastCallTime = Time.time; // �����ϴε���ʱ��
                Cposition--;
                ChangeCameraPosition(Cposition); // ����Ŀ�꺯��
                Changemyposition(Cposition);
            }
        }
        else if (Input.mousePosition.x > Screen.width * 19 / 20)
        {
            if (Time.time - lastCallTime >= callInterval && Cposition < 2)
            {
                lastCallTime = Time.time; // �����ϴε���ʱ��
                Cposition++;
                ChangeCameraPosition(Cposition); // ����Ŀ�꺯��
                Changemyposition(Cposition);
            }
        }
    }
    public void ChangeCopsition(int value)
    {
        Cposition = value;
    }
    private void Changemyposition(int cposition)
    {
        myposition1 = myposition[cposition];
    }

    private void ChangeCameraPosition(int i)
    {
        Cpositionall[i].onClick.Invoke();
    }

    private void OnMouseUp()
    {
        StartCoroutine(ResetPositionCoroutine());
    }
    private IEnumerator ResetPositionCoroutine()
    {
        collider.enabled = true;
        yield return new WaitForSeconds(0.1f); // �ȴ�0.1��
        if(something % 2 == 0)
        transform.position = myposition[Cposition];
    }

    public void toposition2()
    {
        transform.position = myposition2;
    }
    public void toposition3()
    {
        transform.position = myposition3;
    }
    public void tomyposition()
    {
        transform.position = myposition1;
        transform.rotation = myRotation;
    }
    public float gettian()
    {
        return watermaterial.GetFloat("_tian");
    }
    public void settian(float num)
    {
        watermaterial.SetFloat("_tian", num);
    }
    public Color get__1()
    {
        return watermaterial.GetColor("__1");
    }
    public Color get__2()
    {
        return watermaterial.GetColor("__2");
    }
    public void set__1(Color __1)
    {
        watermaterial.SetColor("__1",__1);
    }
    public void set__2(Color __2)
    {
        watermaterial.SetColor("__2", __2);
    }

}
