using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    public PlayerStats stats;

    public TextMeshProUGUI saludValue;
    public TextMeshProUGUI geoValue;
    public TextMeshProUGUI cajasValue;
    public TextMeshProUGUI gemasValue;



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
        saludValue.text = stats.salud.ToString();
        geoValue.text = stats.geo.ToString();
        //cajasValue.text = stats.cajasDestruidas.ToString();
        //gemasValue.text = stats.gemas.ToString();

    }
}
