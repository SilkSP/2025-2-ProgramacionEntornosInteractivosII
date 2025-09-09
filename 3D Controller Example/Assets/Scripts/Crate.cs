using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class Crate : MonoBehaviour
{
    public GameObject prefabFruit;

    public int initialResistance = 1;
    private int actualResistance;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        actualResistance = initialResistance;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPlayerBounce(PlayerCharacterController player)
    {
        actualResistance--;
        player.pm.ExecuteBounceJump();//rebotar

        if (actualResistance <= 0 )
        {
            OnDestroyCrate(player);
        }
        else
        {
            SpawnFruit();
        }

    }

    public void OnPlayerHeadBounce(PlayerCharacterController player)
    {
        actualResistance--;
        //player.pm.ExecuteBounceJump();//rebotar

        player.pm.ForceStopVertical();
        player.pm.CancelJump();
        //Cancelar el salto
        if (actualResistance <= 0)
        {
            OnDestroyCrate(player);
        }
        else
        {
            SpawnFruit();
        }

    }

    public void SpawnFruit()
    {
        GameObject fruit = Instantiate(prefabFruit);
        fruit.transform.position = this.transform.position;
    }

    public void OnDestroyCrate(PlayerCharacterController player)
    {
        SpawnFruit();

        if (player.playerStats != null)
        {
            player.playerStats.cajasDestruidas = player.playerStats.cajasDestruidas + 1;
        }

        Destroy(this.gameObject);
    }
    


}
