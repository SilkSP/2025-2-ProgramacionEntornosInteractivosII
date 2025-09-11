using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStats", menuName = "Player/PlayerStats")]
public class PlayerStats : ScriptableObject
{
    public int salud = 3;
    public int geo = 0;
    public int cajasDestruidas = 0;
    public int gemas = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ResetStats()
    {
        salud = 3;
        geo = 0;
        cajasDestruidas = 0;
        gemas = 0;
    }   
}
