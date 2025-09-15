using UnityEngine;
using System.Collections;

public class HeadSensor : MonoBehaviour
{
    public HeadMovement snakeHead;


    private void Awake()
    {
        if (!snakeHead) snakeHead = GetComponentInParent<HeadMovement>();
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        int layer = other.gameObject.layer;

        
        if (layer == LayerMask.NameToLayer("Food"))
        {
            if (snakeHead != null)
            {
                //TODO
                //w glowie weza jest lista pozostalych segmentow ciala, przy zjedzeniu trzeba dodac nowy segment bezposrednio za glowa
                //mozliwe ze trzeba bedzie tez ustawiac odpowiednia klatke animacji
            }

            Destroy(other.gameObject); 
        }
    }
}
