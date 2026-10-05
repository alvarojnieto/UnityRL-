using UnityEngine;
using UnityEngine.InputSystem;

public class CocheController : MonoBehaviour
{
    [SerializeField] private float fuerzaAvance = 15f;
    [SerializeField] private float velocidadGiro = 90f; 
    private Rigidbody rb;
    private float acelerador; // 
    private float direccion;  // 

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        var teclado = Keyboard.current;
        if (teclado == null) return;

        acelerador = 0f;
        direccion = 0f;

        if (teclado.wKey.isPressed) acelerador += 1f;
        if (teclado.sKey.isPressed) acelerador -= 1f;
        if (teclado.dKey.isPressed) direccion += 1f;
        if (teclado.aKey.isPressed) direccion -= 1f;
    }

    void FixedUpdate()
    {
        
        rb.AddForce(transform.forward * acelerador * fuerzaAvance);

        float angulo = direccion * velocidadGiro * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, angulo, 0f));
    }
}