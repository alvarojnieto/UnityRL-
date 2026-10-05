using UnityEngine;

public class CamaraSeguimiento : MonoBehaviour
{
    [SerializeField] private Transform coche;

    void LateUpdate()
    {
        if (coche == null)
        {
            Debug.LogError("NO HAY COCHE ASIGNADO");
            return;
        }

        transform.position = coche.position + new Vector3(0f, 15f, 0f);
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}