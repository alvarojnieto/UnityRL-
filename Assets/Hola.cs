using UnityEngine;
using UnityEngine.InputSystem;

public class Hola : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Hola desde Start()");
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("Pulsaste espacio");
        }
    }
}