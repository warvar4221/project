using UnityEngine;

public class Pellet : MonoBehaviour
{
    public int points = 10;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player picked up pellet!");

           

            // ”ничтожаем сам объект пеллета
            Destroy(gameObject);
        }
    }
}
