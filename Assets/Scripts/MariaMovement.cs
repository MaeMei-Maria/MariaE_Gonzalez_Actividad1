using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MariaMovement : MonoBehaviour
{
    private CharacterController ch_Controller;
    private Animator animator;

    [SerializeField] private float normalSpeed = 1.5f;
    [SerializeField] private float runSpeed = 3.5f;
    [SerializeField] private float slideSlope = 4f;
    [SerializeField] private float slideVelocity = 3f;
    [SerializeField] private float maxSlideVelocity = 6f;

    private Vector3 playerVelocity;
    private float gravity = 9.8f;
    private float slidenTime = 0f;
    private bool sliding = false;

    [Header("Animator")]
    private static readonly int ZSpeed = Animator.StringToHash("zSpeed");
    private static readonly int XSpeed = Animator.StringToHash("xSpeed");

    void Start()
    {
        ch_Controller = GetComponent<CharacterController>();       
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        UpdatePlayerVelocity();
        UpdateSlideVelocity();

        ApplyVelocity();
    }

    void ApplyVelocity()
    {
        Vector3 totalVelocity = playerVelocity;

        ch_Controller.SimpleMove(totalVelocity);
    }

    void UpdatePlayerVelocity()
    {
        //Setear input.
        float xInput = Input.GetAxis("Horizontal");
        float zInput = Input.GetAxis("Vertical");

        //Se combinan los inputs y se normaliza el vector creado.
        Vector3 input = new Vector3(xInput, 0, zInput);
        if (input.sqrMagnitude < 1)
        {
            input.Normalize();
        }

        //Cálculo de la velocidad.
        Vector3 localPlayerVelocity = new Vector3(input.x * normalSpeed, 0, input.z * normalSpeed);

        playerVelocity = transform.TransformVector(localPlayerVelocity);

        //Llamar a las animaciones pasándole la velocidad de movimiento en cada eje.
        animator.SetFloat(ZSpeed, localPlayerVelocity.z);
        animator.SetFloat(XSpeed, localPlayerVelocity.x);
    }

    void UpdateSlideVelocity()
    {
        Vector3 maxSlideVelocity = Vector3.zero;

        RaycastHit hitInfo;

        if (ch_Controller.isGrounded && Physics.SphereCast(transform.position + ch_Controller.center, ch_Controller.radius, Vector3.down, out hitInfo))
        {
            float angle = Vector3.Angle(hitInfo.normal, Vector3.up); //Ángulo entre la normal del HitInfo y el Vector.up.

            if(angle > slideSlope)
            {
                sliding = true;

                Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, hitInfo.normal).normalized; //Dirección del slide proyectado en un plano.
                maxSlideVelocity = slideDirection * slideVelocity;
            }
            else
            {
                sliding = false;
                slidenTime = 0;
            }

            if (sliding)
            {
                slidenTime += Time.deltaTime;
            }
        }
    }
}