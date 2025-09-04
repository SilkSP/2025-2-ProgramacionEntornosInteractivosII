using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    public string sceneName = "";

    public bool mustResetStats;

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
        if(other.CompareTag("Player"))
        {
            if(mustResetStats)
            {
                other.GetComponent<PlayerCharacterController>().stats.ResetStats();
            }
            SceneManager.LoadScene(sceneName);
        }
        
    }
}
