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
    [SerializeField] private float recompensaAvance = 1f;   
    [SerializeField] private float bonusMeta = 1f;
    [SerializeField] private float castigoChoque = -1f;

    [SerializeField] private float velocidadMaxima = 20f;   
    private Rigidbody rb;
    private CocheController motor;
    private float ultimoAvance;
    private bool terminado;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        motor = GetComponent<CocheController>();
        motor.ControlExterno = true;
    }

    public override void OnEpisodeBegin()
    {
        terminado = false;

        var spline = lineaGuia.Spline;
        Vector3 pos = lineaGuia.transform.TransformPoint((Vector3)SplineUtility.EvaluatePosition(spline, 0f));
        Vector3 dir = lineaGuia.transform.TransformDirection((Vector3)SplineUtility.EvaluateTangent(spline, 0f));
        dir.y = 0f;

        transform.SetPositionAndRotation(pos + Vector3.up * alturaInicial,
                                         Quaternion.LookRotation(dir.normalized));
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        motor.Acelerador = 0f;
        motor.Direccion = 0f;

        ultimoAvance = CalcularAvance();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 v = transform.InverseTransformDirection(rb.linearVelocity);
        sensor.AddObservation(v.x / velocidadMaxima);          
        sensor.AddObservation(v.z / velocidadMaxima);          
        sensor.AddObservation(rb.angularVelocity.y / 5f);      
    }

    public override void OnActionReceived(ActionBuffers acciones)
    {
        if (terminado) return;

        motor.Acelerador = Mathf.Clamp(acciones.ContinuousActions[0], -1f, 1f);
        motor.Direccion = Mathf.Clamp(acciones.ContinuousActions[1], -1f, 1f);

        // Recompensa por avanzar a lo largo de la línea guía
        float avance = CalcularAvance();
        float delta = avance - ultimoAvance;
        ultimoAvance = avance;
        if (Mathf.Abs(delta) < 0.2f)               
            AddReward(delta * recompensaAvance);

      
        if (MaxStep > 0) AddReward(-1f / MaxStep);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var acc = actionsOut.ContinuousActions;
        var teclado = Keyboard.current;
        float a = 0f, d = 0f;
        if (teclado != null)
        {
            if (teclado.wKey.isPressed) a += 1f;
            if (teclado.sKey.isPressed) a -= 1f;
            if (teclado.dKey.isPressed) d += 1f;
            if (teclado.aKey.isPressed) d -= 1f;
        }
        acc[0] = a;
        acc[1] = d;
    }

    private float CalcularAvance()
    {
        Vector3 local = lineaGuia.transform.InverseTransformPoint(transform.position);
        SplineUtility.GetNearestPoint(lineaGuia.Spline, new float3(local.x, local.y, local.z),
                                      out float3 _, out float t, 16, 4);
        return t;  
    }

    void OnCollisionEnter(Collision c)
    {
        if (c.collider.CompareTag(etiquetaMuro)) Terminar(castigoChoque, "Choque");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(etiquetaMeta)) Terminar(bonusMeta, "Meta");
    }

    private void Terminar(float recompensa, string motivo)
    {
        if (terminado) return;         
        terminado = true;
        AddReward(recompensa);
        Debug.Log($"{motivo}: recompensa total del episodio = {GetCumulativeReward():F2}");
        EndEpisode();
    }
}