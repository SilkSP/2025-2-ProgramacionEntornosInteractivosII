using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    public PlayerStats stats;

    public TextMeshProUGUI vidasValue;
    public TextMeshProUGUI frutasValue;
    public TextMeshProUGUI cajasValue;
    public TextMeshProUGUI gemasValue;
    public TextMeshProUGUI saltosValue;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        ActualizarHUD();
    }


    public void ActualizarHUD()
    {
        vidasValue.text = stats.vidas.ToString();
        frutasValue.text = stats.frutas.ToString();
        cajasValue.text = stats.cajasDestruidas.ToString();
        gemasValue.text = stats.gemas.ToString();
        saltosValue.text = GameplayManager.instance.GetJumps().ToString();

    }
}
