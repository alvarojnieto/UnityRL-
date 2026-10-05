using UnityEngine;
using UnityEngine.InputSystem;

public class CocheController : MonoBehaviour
{
    [SerializeField] private float fuerzaAvance = 15f;
    [SerializeField] private float velocidadGiro = 90f;

   
    public float Acelerador { get; set; }
    public float Direccion { get; set; }
    public bool ControlExterno { get; set; }

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (ControlExterno) return;

        var teclado = Keyboard.current;
        if (teclado == null) return;

        Acelerador = 0f;
        Direccion = 0f;
        if (teclado.wKey.isPressed) Acelerador += 1f;
        if (teclado.sKey.isPressed) Acelerador -= 1f;
        if (teclado.dKey.isPressed) Direccion += 1f;
        if (teclado.aKey.isPressed) Direccion -= 1f;
    }

    void FixedUpdate()
    {
        rb.AddForce(transform.forward * Acelerador * fuerzaAvance);

        float angulo = Direccion * velocidadGiro * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, angulo, 0f));
    }
}