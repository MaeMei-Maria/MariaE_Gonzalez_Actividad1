using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MariaMovement : MonoBehaviour
{
    private CharacterController ch_Controller;
    private Animator animator;

    [Header ("Movement")]
    [SerializeField] private float normalSpeed = 1.5f;
    [SerializeField] private float runSpeed = 3.5f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float jumpRayDistance = 3f;

    [Header("Slide")]
    [SerializeField] private AnimationCurve slideSlowCurve;
    [SerializeField] private float slideSlope = 4f;
    [SerializeField] private float slideSpeed = 3f;
    [SerializeField] private float maxSlideVelocity = 6f;
    [SerializeField] private float slideDownTime = 3f;

    [Header("Crouched")]
    [SerializeField] private float crouchSpeed = 1f;
    [SerializeField] private float standHeight = 2f; // Altura de la cápsula cuando está de pie.
    [SerializeField] private float crouchHeight = 0.8f; // Altura de la cápsula cuando está agachado.
    [SerializeField] private float crouchCenter = 0.4f; // Centro de la cápsula cuando está agachado.
    [SerializeField] private float standCenter = 0.5f; // Centro de la cápsula cuando está de pie.

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 8f;
    [SerializeField] private float dashDuration = 2f;
    [SerializeField] private float dashEndSpeed = 1f;

    private Vector3 playerVelocity;
    private Vector3 slideVelocity;
    private float gravity = -9.8f;
    private float verticalSpeed;
    private float currentSpeed; // Variable para suavizar la transición de velocidad
    private float slidenTime = 0f;
    private float slideVelocityFactor = 1f;
    private bool sliding = false;
    private bool isCrouched = false;
    private bool tryingToStand = false;
    private Vector3 dashDirection;
    private bool isDashing = false;
    private float dashTime = 0f;

    //Variables de números enteros
    private static readonly int ZSpeed = Animator.StringToHash("zSpeed");
    private static readonly int XSpeed = Animator.StringToHash("xSpeed");
    private static readonly int Crouched = Animator.StringToHash("crouched");

    void Start()
    {
        ch_Controller = GetComponent<CharacterController>();       
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isDashing)
        {
            HandleDash();
        }
        else
        {
            UpdatePlayerVelocity();
            UpdateVerticalSpeed();
            UpdateSlideVelocity();
            HandleCrouch();

            if (tryingToStand)
            {
                TryStandUp();
            }

            ApplyVelocity();
        }


        StartDash();
    }

    void ApplyVelocity()
    {
        Vector3 totalVelocity = (playerVelocity + slideVelocity) * slideVelocityFactor + Vector3.up * verticalSpeed;
        ch_Controller.Move(totalVelocity * Time.deltaTime);
    }

    void HandleCrouch()
    {
        // Si se mantiene presionada la tecla LeftControl, se agacha
        if (Input.GetKey(KeyCode.LeftControl) && !isCrouched)
        {
            StartCrouch();
        }
        // Levantarse si se suelta la tecla
        else if (Input.GetKeyUp(KeyCode.LeftControl) && isCrouched)
        {
            tryingToStand = true;
        }
    }

    void UpdatePlayerVelocity()
    {
        //Setear input.
        float xInput = Input.GetAxis("Horizontal");
        float zInput = Input.GetAxis("Vertical");

        //Se combinan los inputs y se normaliza el vector creado.
        Vector3 input = new Vector3(xInput, 0, zInput);
        if (input.magnitude < 1)
        {
            input.Normalize();
        }

        if(isCrouched)
        {
            currentSpeed = crouchSpeed;
        }
        else
        {
            //Interpolar para caminar y correr.
            float targetSpeed = Input.GetKey(KeyCode.LeftShift) && !isCrouched ? runSpeed : normalSpeed; //Si presiona Shift y no está agachado se usa la velocidad para correr sino usa la velociad normal.
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5); //Suaviza el cambio de velocidad gradualmente.
        }

        //Cálculo de la velocidad.
        Vector3 localPlayerVelocity = new Vector3(input.x * currentSpeed, 0, input.z * currentSpeed);

        playerVelocity = transform.TransformVector(localPlayerVelocity);

        //Llamar a las animaciones pasándole la velocidad de movimiento en cada eje.
        animator.SetFloat(ZSpeed, localPlayerVelocity.z);
        animator.SetFloat(XSpeed, localPlayerVelocity.x);
    }

    void UpdateVerticalSpeed()
    {
        if (ch_Controller.isGrounded)
        {
            verticalSpeed = -1f; // Mantener un valor bajo en el suelo para evitar microflotaciones
        }
        else
        {
            verticalSpeed += gravity * Time.deltaTime;
        }
    }

    void UpdateSlideVelocity()
    {
        Vector3 maxSlideVelocity = Vector3.zero;

        RaycastHit hitInfo;

        if (ch_Controller.isGrounded && Physics.Raycast(transform.position, Vector3.down, out hitInfo, ch_Controller.height))
        {
            float angle = Vector3.Angle(hitInfo.normal, Vector3.up); //Ángulo entre la normal del HitInfo y el Vector.up.

            if (angle > slideSlope)
            {
                sliding = true;

                Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, hitInfo.normal).normalized; //Dirección del slide proyectado en un plano.
                
                //REVISAR YA QUE NO SE CAE POR LA PENDIENTE
                maxSlideVelocity = slideDirection * slideSpeed;

                Debug.DrawRay(hitInfo.point, hitInfo.normal, Color.red, 3);
                Debug.DrawRay(hitInfo.point, slideDirection, Color.blue, 3);
            }
            else
            {
                sliding = false;
                slidenTime = 0;
            }
        }

        if (sliding)
        {
            slidenTime += Time.deltaTime;
        }

        /// <summary>
        /// Calcula la velocidad del deslizamiento dependiendo de si el personaje está deslizándose o no.
        /// (?) Si está deslizándose, interpolar la velocidad hacia la velocidad máxima con un factor de suavizado.
        /// (:) Si no está deslizándose, reducir gradualmente la velocidad hasta detenerse (Vector3.zero).
        /// </summary>

        slideVelocity = sliding
            ? Vector3.Lerp(slideVelocity, maxSlideVelocity, Time.deltaTime * 3)
            : Vector3.Lerp(slideVelocity, Vector3.zero, Time.deltaTime * 5);

        /// <summary>
        /// Ralentizar el movimiento tras alcanzar la velocidad máxima.
        /// (?) Si está deslizándose, usar una curva de desaceleración para ajustar la velocidad en función del tiempo de deslizamiento.
        /// (:) Si no está deslizándose, suavizar el factor de velocidad de vuelta a 1 con interpolación lineal.
        /// </summary>
        slideVelocityFactor = sliding
            ? slideSlowCurve.Evaluate(Mathf.Clamp01(slidenTime / slideDownTime))
            : Mathf.Lerp(slideVelocityFactor, 1, 10 * Time.deltaTime);
    }

    void StartCrouch()
    {
        isCrouched = true;
        ch_Controller.height = crouchHeight;
        ch_Controller.center = new Vector3(0, crouchCenter, 0);
        animator.SetBool(Crouched, true);
        tryingToStand = false;
    }

    void TryStandUp()
    {
        if (CanStandUp())
        {
            StandUp();
            tryingToStand = false;
        }
    }

    private bool CanStandUp()
    {
        RaycastHit hitInfo;
        
        return !Physics.SphereCast(transform.position + ch_Controller.center, ch_Controller.radius, Vector3.up, out hitInfo, 2f);
    }

    void StandUp()
    {
        ch_Controller.height = standHeight;
        ch_Controller.center = new Vector3(0, standCenter, 0);
        animator.SetBool(Crouched, false);
        isCrouched = false;
    }

    void StartDash()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isDashing = true; // Activamos el estado de dash
            dashTime = 0; // Reiniciamos el temporizador del dash

            // Si el jugador se estaba moviendo, usamos su dirección normalizada.
            // Si no se estaba moviendo, usamos transform.forward como dirección por defecto.
            dashDirection = playerVelocity.sqrMagnitude > 0 ? playerVelocity.normalized : transform.forward;
        }
    }

    void HandleDash()
    {
        dashTime += Time.deltaTime; // Aumentamos el tiempo transcurrido en el dash
        ch_Controller.Move(dashDirection * dashSpeed * Time.deltaTime); // Movemos al jugador en la dirección del dash
        if (dashTime >= dashDuration) isDashing = false; // Terminamos el dash cuando se cumple el tiempo
    }
}