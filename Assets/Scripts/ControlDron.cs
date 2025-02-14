using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlDron : MonoBehaviour
{
    private CharacterController controller;

    [SerializeField] private float pushPower = 2f;

    [Header("Movement")]
    private Vector3 dronVelocity;
    private Vector3 currentVelocity;
    [SerializeField] private float acceleration = 2f;
    [SerializeField] private float dronForwardVelocity = 3f;
    [SerializeField] private float dronVerticalVelocity = 3f;
    [SerializeField] private float dronRotationVelocity = 10f;
    [SerializeField] private float maxDronVelocity = 7f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        UpdateDronVelocity();
        UpdateDronRotation();
        ApplyVelocity();
    }

    void ApplyVelocity()
    {
        Vector3 totalVelocity = dronVelocity;

        controller.Move(totalVelocity * Time.deltaTime);
    }

    void UpdateDronVelocity()
    {
        //Se accede al input WS/Flechas para el movimiento hacia delante y atrás.
        float zInput = Input.GetAxis("Vertical");

        //Se accede al input Control/Espacio para el movimiento hacia arriba y abajo.
        float yInput = 0f;

        if(Input.GetKey(KeyCode.Space))
        {
            yInput = maxDronVelocity;
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            yInput = -maxDronVelocity;
        }

        //Se combinan los inputs y se normaliza
        Vector3 desiredVelocity = new Vector3(0, yInput, zInput);
        
        if(desiredVelocity.magnitude > 1)
        {
            desiredVelocity.Normalize();
        }

        desiredVelocity *= maxDronVelocity;

        //Se calcula la velocidad
        currentVelocity = Vector3.MoveTowards(currentVelocity, desiredVelocity, acceleration * Time.deltaTime);

        dronVelocity = transform.TransformVector(currentVelocity);
    }

    void UpdateDronRotation()
    {
        //Rotación con el AD/Flechas derecha/Izquierda.
        float rotationInput = Input.GetAxis("Horizontal") * dronRotationVelocity * Time.deltaTime;

        transform.Rotate(0, rotationInput, 0);
    }

    //Empujar objetos dinámicos.
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;

        //No tiene Rigidbody o es Kinemático
        if(body == null || body.isKinematic)
        {
            return;
        }

        //Para evitar empujar un objeto que está por debajo
        if(hit.moveDirection.y < -0.3)
        {
            return;
        }

        //Calcular la dirección de empuje hacia los lados.
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        //Aplicar el empuje.
        body.AddForce(pushDir * pushPower, ForceMode.Impulse);
    }
}
