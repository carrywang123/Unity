using UnityEngine;

public class Xiping : MonoBehaviour
{
    public GameObject step;
    public GameObject water;
    public GameObject bisemin;
    public GameObject gaizi;
    public float rospeed;
    public float waterspeed;
    public float waterspeedbi;
    public Vector3[] myposition;
    private Material watermaterial;
    private wait currentwait;
    private ChangeTextColor changeTextColor;
    Color __1;
    Color __2;
    float waternum;
    float waternumbi;
    int working;
    private void Start()
    {
        try
        {
            watermaterial = water.gameObject.GetComponent<Renderer>().material;
            changeTextColor = step.gameObject.GetComponent<ChangeTextColor>();
            waternum = gettian();
        }
        catch (System.Exception e)
        {
            Debug.Log(e.Message);
        }
        working = 0;
    }

    private void Update()
    {
        if (working == 1)
        {
            Quaternion currentRotation = this.gameObject.transform.rotation;
            if(tag=="Ï´Æ¿")
            {
                if (currentRotation.x > -0.67f)
                {
                    Quaternion addedRotation = Quaternion.Euler(0, rospeed * Time.deltaTime, 0);
                    this.gameObject.transform.rotation = currentRotation * addedRotation;
                }
                else
                {
                    working++;
                }
            }
            else
            {
                if (currentRotation.x > -0.36f)
                {
                    Quaternion addedRotation = Quaternion.Euler(-1 * rospeed * Time.deltaTime, 0, 0);
                    this.gameObject.transform.rotation = currentRotation * addedRotation;
                }
                else
                {
                    working++;
                }
            }
            
        }
        else if(working == 2)
        {
            Debug.Log(waternumbi);
            
            waternum -= waterspeed * Time.deltaTime;
            watermaterial.SetFloat("_tian", waternum);
            waternumbi += waterspeedbi * Time.deltaTime;
            currentwait.settian(waternumbi);
            if(waternumbi > 0.4f)
            {
                working = 3;
                if(tag == "Ï´Æ¿")
                    transform.eulerAngles = new Vector3(-90, 0, 90);
                else
                {
                    transform.eulerAngles = new Vector3(0, 0, 0);
                }
            }
        }
        if(working == 3)
        {
            if (
            this.gameObject.tag == "ÈÝÁ¿Æ¿1" ||
            this.gameObject.tag == "ÈÝÁ¿Æ¿2" ||
            this.gameObject.tag == "ÈÝÁ¿Æ¿3" ||
            this.gameObject.tag == "ÈÝÁ¿Æ¿4")
            {
                gaizi.gameObject.SetActive(true);
            }
            currentwait.tomyposition();
            transform.position = myposition[0];
            working = 4;
            currentwait.Something = 2;
            changeTextColor.Changetext();
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Åöµ½ÁË");
        if(other.CompareTag("±ÈÉ«Ãó")&&this.gameObject.tag == "Ï´Æ¿")
        {
            transform.position = myposition[1];
            bisemin = other.gameObject;
            currentwait = bisemin.GetComponent<wait>();
            currentwait.toposition2();
            working = 1;
            waternumbi = currentwait.gettian();
            waternum = gettian();
            currentwait.Something = 1;
        }
        if (other.CompareTag("±ÈÉ«Ãó") && (
            this.gameObject.tag == "ÈÝÁ¿Æ¿1"||
            this.gameObject.tag == "ÈÝÁ¿Æ¿2"||
            this.gameObject.tag == "ÈÝÁ¿Æ¿3"||
            this.gameObject.tag == "ÈÝÁ¿Æ¿4"))
        {
            transform.position = myposition[1];
            bisemin = other.gameObject;
            currentwait = bisemin.GetComponent<wait>();
            currentwait.toposition3();
            working = 1;
            waternumbi = currentwait.gettian();
            waternum = gettian();
            currentwait.Something = 1;
            gaizi.gameObject.SetActive(false);
            __1 = get__1();
            __2 = get__2();
            currentwait.set__1(__1);
            currentwait.set__2(__2);
        }
    }

    public float gettian()
    {
        return watermaterial.GetFloat("_tian");
    }

    public Color get__1()
    {
        return watermaterial.GetColor("__1");
    }
    public Color get__2()
    {
        return watermaterial.GetColor("__2");
    }
    public void settian(float num)
    {
        watermaterial.SetFloat("_tian", num);
    }
}
