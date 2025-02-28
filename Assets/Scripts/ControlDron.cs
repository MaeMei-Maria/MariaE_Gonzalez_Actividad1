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
    [SerializeField] private float dronRotationVelocity = 10f;
    [SerializeField] private float maxDronVelocity = 7f;

    [Header("Fall")]
    private float gravity = 9.8f;
    private bool isFalling = false; //Indica si se chocó con algo por arriba.
    private float fallTimer = 0f;
    private float maxFallTime = 1f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Verifica si el dron sigue en el aire (no tocando el suelo)
        if(isFalling)
        {
            DronIsFalling();
        }
        else
        {
            UpdateDronVelocity();
            UpdateDronRotation();
            ApplyVelocity();
        }
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
        
        if(desiredVelocity.sqrMagnitude > 1)
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
        ApplyForce(hit);

        // Detectar colisión con un techo (comprobando si el punto de impacto está arriba del dron)
        if (controller.collisionFlags == CollisionFlags.Above)
        {
            StartFalling();
        }

        if (controller.isGrounded && isFalling)
        {
            StopFalling();
        }
    }

    void StartFalling()
    {
        isFalling = true;
        fallTimer = 0;
        dronVelocity = Vector3.zero;
    }

    void DronIsFalling()
    {
        fallTimer += Time.deltaTime;

        // Aplica gravedad solo si el dron está en el aire
        if (!controller.isGrounded)
        {
            dronVelocity.y -= gravity * Time.deltaTime; // Aplica gravedad
        }

        controller.Move(dronVelocity * Time.deltaTime);

        if (controller.isGrounded || fallTimer >= maxFallTime)
        {
            StopFalling();
        }
    }

    // Detiene la caída y el movimiento del dron
    private void StopFalling()
    {
        dronVelocity = Vector3.zero;
        currentVelocity = Vector3.zero;

        isFalling = false;  // Detiene la caída
        fallTimer = 0f;      // Reinicia el tiempo de caída
    }

    private void ApplyForce(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;

        //No tiene Rigidbody o es Kinemático
        if (body == null || body.isKinematic)
        {
            return;
        }

        //Para evitar empujar un objeto que está por debajo
        if (hit.moveDirection.y < -0.3)
        {
            return;
        }

        //Calcular la dirección de empuje hacia los lados.
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        //Aplicar el empuje.
        body.AddForce(pushDir * pushPower, ForceMode.Impulse);
    }
}
