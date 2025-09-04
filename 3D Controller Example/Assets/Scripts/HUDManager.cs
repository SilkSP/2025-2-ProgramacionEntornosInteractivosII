using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    public PlayerStats stats;

    public TextMeshProUGUI vidasTexto;
    public TextMeshProUGUI frutasTexto;
    public TextMeshProUGUI cajasTexto;
    public TextMeshProUGUI gemasTexto;



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
        vidasTexto.text = stats.vidas.ToString();
        frutasTexto.text = stats.frutas.ToString();
        cajasTexto.text = stats.cajasDestruidas.ToString();
        gemasTexto.text = stats.gemas.ToString();

    }
}
