using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class Crate : MonoBehaviour
{
    public GameObject prefabFruit;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnDestroyCrate()
    {
        GameObject fruit = Instantiate(prefabFruit);
        fruit.transform.position = this.transform.position;
        Destroy(this.gameObject);
    }
    


}
