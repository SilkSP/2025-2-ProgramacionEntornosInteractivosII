using UnityEngine;

public class GameplayManager : MonoBehaviour
{
    public static GameplayManager instance;

    private int cantidadSaltos = 0;
    [SerializeField] private GameObject playerGameObject;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
    }


    public void AddJumps()
    {
        cantidadSaltos++;
    }
    public int GetJumps()
    {
        return cantidadSaltos;
    }
    
    public void SetPlayerGameObject(GameObject player)
    {
        playerGameObject = player;
    }
    public GameObject GetPlayerGameObject()
    {
        return playerGameObject;
    }
}
