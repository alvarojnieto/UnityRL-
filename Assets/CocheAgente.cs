using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

public class CocheAgente : Agent
{
    [Header("Referencias")]
    [SerializeField] private SplineContainer lineaGuia;
    [SerializeField] private float alturaInicial = 0.2f;

    [Header("Etiquetas")]
    [SerializeField] private string etiquetaMuro = "Muro";
    [SerializeField] private string etiquetaMeta = "Meta";

    [Header("Recompensas")]
    [SerializeField] private float recompensaVelocidadPista = 0.002f;
    [SerializeField] private float costeTiempo = 0.001f;
    [SerializeField] private float bonusMeta = 5f;
    [SerializeField] private float castigoChoque = -2f;

    [Header("Configuración")]
    [SerializeField] private float velocidadMaxima = 20f;
    [SerializeField] private float distanciaNormalizacion = 10f;

    [Header("Piloto automático (solo para pruebas, mantén ESPACIO en Heuristic)")]
    [SerializeField] private float pilotoAcelerador = 1f;
    [SerializeField] private float pilotoGanancia = 2f;

    [Header("Debug")]
    [SerializeField] private bool mostrarLogs = false;

    private Rigidbody rb;
    private CocheController motor;

    private bool terminado;


    // =========================================================
    // INITIALIZE
    // =========================================================

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        motor = GetComponent<CocheController>();

        motor.ControlExterno = true;
    }


    // =========================================================
    // INICIO DEL EPISODIO
    // =========================================================

    public override void OnEpisodeBegin()
    {
        terminado = false;

        var spline = lineaGuia.Spline;

        // Posición inicial
        Vector3 pos =
            lineaGuia.transform.TransformPoint(
                (Vector3)SplineUtility.EvaluatePosition(
                    spline,
                    0f
                )
            );

        // Dirección inicial
        Vector3 dir =
            lineaGuia.transform.TransformDirection(
                (Vector3)SplineUtility.EvaluateTangent(
                    spline,
                    0f
                )
            );

        dir.y = 0f;

        // Colocar coche
        transform.SetPositionAndRotation(
            pos + Vector3.up * alturaInicial,
            Quaternion.LookRotation(dir.normalized)
        );

        // Reset físico
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Reset controles
        motor.Acelerador = 0f;
        motor.Direccion = 0f;
    }


    // =========================================================
    // OBSERVACIONES (6 en total: Space Size = 6)
    // =========================================================

    public override void CollectObservations(VectorSensor sensor)
    {
        // 1. Velocidad local del coche (2 valores)
        Vector3 velocidadLocal =
            transform.InverseTransformDirection(
                rb.linearVelocity
            );

        sensor.AddObservation(
            Mathf.Clamp(velocidadLocal.x / velocidadMaxima, -1f, 1f)
        );

        sensor.AddObservation(
            Mathf.Clamp(velocidadLocal.z / velocidadMaxima, -1f, 1f)
        );

        // 2. Velocidad angular (1 valor)
        sensor.AddObservation(
            Mathf.Clamp(rb.angularVelocity.y / 5f, -1f, 1f)
        );

        // 3. Información de la spline
        ObtenerInformacionSpline(
            out Vector3 tangenteMundo,
            out float distanciaLateral
        );

        // 4. Dirección de la pista relativa al coche (2 valores)
        Vector3 tangenteCoche =
            transform.InverseTransformDirection(
                tangenteMundo
            );

        sensor.AddObservation(tangenteCoche.x);
        sensor.AddObservation(tangenteCoche.z);

        // 5. Distancia lateral a la pista (1 valor)
        sensor.AddObservation(
            Mathf.Clamp(
                distanciaLateral / distanciaNormalizacion,
                -1f,
                1f
            )
        );
    }


    // =========================================================
    // ACCIONES
    // =========================================================

    public override void OnActionReceived(
        ActionBuffers acciones
    )
    {
        if (terminado)
            return;

        float acelerador =
            Mathf.Clamp01(acciones.ContinuousActions[0]);

        float direccion =
            Mathf.Clamp(acciones.ContinuousActions[1], -1f, 1f);

        motor.Acelerador = acelerador;
        motor.Direccion = direccion;

        // Dirección de la pista
        ObtenerInformacionSpline(
            out Vector3 tangenteMundo,
            out float distanciaLateral
        );

        // Velocidad en la dirección de la pista
        float velocidadEnPista =
            Vector3.Dot(
                rb.linearVelocity,
                tangenteMundo
            );

        // Recompensa por avanzar (y castigo simétrico si va hacia atrás)
        AddReward(velocidadEnPista * recompensaVelocidadPista);

        // Coste por tiempo
        AddReward(-costeTiempo);
    }


    // =========================================================
    // HEURISTIC
    // =========================================================

    public override void Heuristic(
        in ActionBuffers actionsOut
    )
    {
        var acc =
            actionsOut.ContinuousActions;

        var teclado =
            Keyboard.current;

        float acelerador = 0f;
        float direccion = 0f;

        if (teclado != null)
        {
            if (teclado.wKey.isPressed)
                acelerador += 1f;

            if (teclado.sKey.isPressed)
                acelerador -= 1f;

            if (teclado.dKey.isPressed)
                direccion += 1f;

            if (teclado.aKey.isPressed)
                direccion -= 1f;
        }

        // Piloto automático de prueba: mantén ESPACIO pulsado.
        // Gira hacia donde apunta la pista y hacia el centro de la línea guía.
        if (teclado != null && teclado.spaceKey.isPressed)
        {
            ObtenerInformacionSpline(
                out Vector3 tangenteMundo,
                out float lateral
            );

            Vector3 tangenteCoche =
                transform.InverseTransformDirection(tangenteMundo);

            direccion = Mathf.Clamp(
                (tangenteCoche.x + lateral / distanciaNormalizacion)
                    * pilotoGanancia,
                -1f,
                1f
            );

            acelerador = pilotoAcelerador;
        }

        acc[0] = acelerador;
        acc[1] = direccion;
    }


    // =========================================================
    // INFORMACIÓN DE LA SPLINE
    // =========================================================

    private void ObtenerInformacionSpline(
        out Vector3 tangenteMundo,
        out float distanciaLateral
    )
    {
        Vector3 localPos =
            lineaGuia.transform.InverseTransformPoint(
                transform.position
            );

        // Punto más cercano de la línea guía
        SplineUtility.GetNearestPoint(
            lineaGuia.Spline,
            new float3(localPos.x, localPos.y, localPos.z),
            out float3 punto,
            out float t,
            16,
            4
        );

        // Tangente de la spline en ese punto
        Vector3 tangenteLocal =
            (Vector3)SplineUtility.EvaluateTangent(
                lineaGuia.Spline,
                t
            );

        tangenteMundo =
            lineaGuia.transform.TransformDirection(
                tangenteLocal
            );

        tangenteMundo.y = 0f;

        if (tangenteMundo.sqrMagnitude > 0.001f)
            tangenteMundo.Normalize();
        else
            tangenteMundo = transform.forward;

        // Distancia lateral (positiva si la línea guía queda a la derecha del coche)
        Vector3 puntoMundo =
            lineaGuia.transform.TransformPoint(
                (Vector3)punto
            );

        Vector3 diferencia =
            puntoMundo - transform.position;

        diferencia.y = 0f;

        distanciaLateral =
            Vector3.Dot(
                diferencia,
                transform.right
            );
    }


    // =========================================================
    // COLISIÓN CON MURO
    // =========================================================

    private void OnCollisionEnter(Collision c)
    {
        if (c.collider.CompareTag(etiquetaMuro))
        {
            Terminar(castigoChoque, "Choque");
        }
    }


    // =========================================================
    // META
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(etiquetaMeta))
        {
            Terminar(bonusMeta, "Meta");
        }
    }


    // =========================================================
    // TERMINAR EPISODIO
    // =========================================================

    private void Terminar(
        float recompensa,
        string motivo
    )
    {
        if (terminado)
            return;

        terminado = true;

        AddReward(recompensa);

        if (mostrarLogs)
        {
            Debug.Log(
                $"{motivo}: recompensa total = {GetCumulativeReward():F3}"
            );
        }

        EndEpisode();
    }
}