using UnityEngine;

public class Collectable : MonoBehaviour
{
    public enum CollectableTypeEnum { Fruit, Gem}


    public CollectableTypeEnum CollectableType;

    public float speed = 18f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //transform.Rotate(0, speed * Time.deltaTime, 0);
        
        switch (CollectableType)
        {
            case CollectableTypeEnum.Fruit:
                transform.Rotate(speed * Time.deltaTime, 0, 0);
                break;
            case CollectableTypeEnum.Gem:
                transform.Rotate(0, speed / 4f * Time.deltaTime, speed / 4f * Time.deltaTime);
                break;
        }
        
    }


    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            Destroy(gameObject);

            switch (CollectableType)
            {
                case CollectableTypeEnum.Fruit:
                    other.GetComponent<SideScrollerPlayerCharacterController>().GrabFruitEvent();
                    break;
                case CollectableTypeEnum.Gem:
                    other.GetComponent<SideScrollerPlayerCharacterController>().GrabGemEvent();
                    break;
            }

            
        }
    }
}
