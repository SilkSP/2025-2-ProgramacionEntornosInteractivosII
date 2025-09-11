using UnityEngine;

public class GemSpawner : MonoBehaviour
{
    public PlayerStats stats;
    public GameObject prefabGem;
    public int cajasNecesitadas = 1;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if(stats!=null && stats.cajasDestruidas >= cajasNecesitadas)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                GameObject gem = Instantiate(prefabGem);
                gem.transform.position = this.transform.position;
                Destroy(this.gameObject);
            }
        }
        
    }
}
