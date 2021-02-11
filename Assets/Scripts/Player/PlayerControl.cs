using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControl : MonoBehaviour
{

    // Inputs
    public Controls controls;
    Vector2 inputs;
    [HideInInspector]
    public Vector2 inputNormalized;
    [HideInInspector]
    public float rotation;
    bool run = true, jump;
    [HideInInspector]
    public bool steer, autoRun;

    // Velocity
    Vector3 velocity;
    float gravity = -18, velocityY, terminalVelocity = -25;
    float fallMult;

    // Running
    float currentSpeed;
    public float baseSpeed = 1f, runSpeed = 5.6f, rotateSpeed = 0.7f;

    // Ground
    Vector3 forwardDirection, collisionPoint;
    float slopeAngle, directionAngle, forwardAngle, strafeAngle;
    float forwardMult, strafeMult;
    Ray groundRay;
    RaycastHit groundHit;

    // Jumping
    bool jumping;
    float jumpSpeed, jumpHeight = 3f;
    Vector3 jumpDirection;

    // Debug
    public bool showGroundRay, showMoveDirection, showForwardDirection, showStrafeDirection, showFallNormal;


    // References
    CharacterController controller;
    public Transform groundDirection, moveDirection, fallDirection;
    [HideInInspector]
    public CameraController mainCam;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        GetInputs();
        Locomotion();
    }

    void Locomotion()
    {
        GroundDirection();

        // Running and Walking
        if (controller.isGrounded && slopeAngle <= controller.slopeLimit)
        {
            inputNormalized = inputs.normalized;

            currentSpeed = baseSpeed;

            if (run)
            {
                currentSpeed *= runSpeed;

                if (inputNormalized.y < 0)
                {
                    currentSpeed = currentSpeed / 2;
                }

            }
        }
        else if (!controller.isGrounded || slopeAngle > controller.slopeLimit)
        {
            inputNormalized = Vector2.Lerp(inputNormalized, Vector2.zero, 0.025f);
            currentSpeed = Mathf.Lerp(currentSpeed, 0, 0.025f);
        }
        



        // Rotating
        Vector3 characterRotation = transform.eulerAngles + new Vector3(0, rotation * rotateSpeed, 0);
        transform.eulerAngles = characterRotation;

        // Jump set to Spacebar
        if (jump && controller.isGrounded && slopeAngle <= controller.slopeLimit)
            Jump();

        // Apply gravity if not grounded
        if (!controller.isGrounded && velocityY > terminalVelocity)
            velocityY += gravity * Time.deltaTime;
        else if (controller.isGrounded && slopeAngle > controller.slopeLimit)
            velocityY = Mathf.Lerp(velocityY, terminalVelocity, 0.25f);

        // Applying inputs
        if (!jumping)
        {
            velocity = groundDirection.forward * inputNormalized.y * forwardMult + groundDirection.right * inputNormalized.x * strafeMult;  // Applying movement direction inputs
            velocity *= currentSpeed; // Applying current move speed.
            velocity += fallDirection.up * (velocityY * fallMult);  // Gravity
        }

        else
            velocity = jumpDirection * jumpSpeed + Vector3.up * velocityY;

        // Moving character
        controller.Move(velocity * Time.deltaTime);

        if (controller.isGrounded)
            // Stop jumping if grounded
        {   if (jumping)
                jumping = false;
            // Stop gravity if grounded
            velocityY = 0;
        }
    }

    void GroundDirection()
    {
        // Setting forwardDirection to controller position
        forwardDirection = transform.position;

        // Setting forwardDirection based on control input
        if (inputNormalized.magnitude > 0)
            forwardDirection += transform.forward * inputNormalized.y + transform.right * inputNormalized.x;
        else
            forwardDirection += transform.forward;

        // Setting groundDirection to look forward
        moveDirection.LookAt(forwardDirection);
        fallDirection.rotation = transform.rotation;
        groundDirection.rotation = transform.rotation;



        // Setting ground ray
        groundRay.origin = transform.position + collisionPoint + Vector3.up * 0.05f;
        groundRay.direction = Vector3.down;

        if (showGroundRay)
            Debug.DrawLine(groundRay.origin, groundRay.origin + Vector3.down * 0.3f, Color.red);

        forwardMult = 1;
        fallMult = 1;
        strafeMult = 1;

        if (Physics.Raycast(groundRay, out groundHit, 0.3f))
        {
            // Getting angles
            slopeAngle = Vector3.Angle(transform.up, groundHit.normal);
            directionAngle = Vector3.Angle(moveDirection.forward, groundHit.normal) - 90;

            if (directionAngle < 0 && slopeAngle <= controller.slopeLimit)
            {
                forwardAngle = Vector3.Angle(transform.forward, groundHit.normal) - 90; // Checking forwardAngle to the slope
                forwardMult = 1 / Mathf.Cos(forwardAngle * Mathf.Deg2Rad); // Applying the forward movement multiplier based on the forwardAngle
                groundDirection.eulerAngles += new Vector3(-forwardAngle, 0, 0); // rotating groundDirection X

                strafeAngle = Vector3.Angle(groundDirection.right, groundHit.normal) - 90; // Checking strafeAngle against the slope
                strafeMult = 1 / Mathf.Cos(strafeAngle * Mathf.Deg2Rad); // Applying the strafe movement multiplier based on the stafeAngle
                groundDirection.eulerAngles += new Vector3(0, 0, strafeAngle);
            }
            else if (slopeAngle > controller.slopeLimit)
            {
                float groundDistance = Vector3.Distance(groundRay.origin, groundHit.point);

                if (groundDistance <= 0.1f)
                {
                    fallMult = 1 / Mathf.Cos((90 - slopeAngle) * Mathf.Deg2Rad);

                    Vector3 groundCross = Vector3.Cross(groundHit.normal, Vector3.up);
                    fallDirection.rotation = Quaternion.FromToRotation(transform.up, Vector3.Cross(groundCross, groundHit.normal));
                }
            }
        }

        DebugGroundNormals();
    }

    void Jump()
    {
        // Set jumping to true
        if (!jumping)
            jumping = true;

        // Set jump direction and speed
        jumpDirection = (transform.forward * inputs.y + transform.right * inputs.x).normalized;
        jumpSpeed = currentSpeed;

        // Set velocity Y
        velocityY = Mathf.Sqrt(-gravity * jumpHeight);
    }

    void GetInputs()
    {
        if (controls.autoRun.GetControlBindingDown())
            autoRun = !autoRun;

        // FORWARD and BACKWARD controls
        inputs.y = Axis(controls.forwards.GetControlBinding(), controls.backwards.GetControlBinding());

        if (inputs.y != 0 && !mainCam.autoRunReset)
        {
            autoRun = false;
        }

        if (autoRun)
        {
            inputs.y += 1;

            inputs.y = Mathf.Clamp(inputs.y, -1, 1);
        }

        // STRAFE LEFT AND RIGHT
        inputs.x = Axis(controls.strafeRight.GetControlBinding(), controls.strafeLeft.GetControlBinding());

        if (steer)
        {
            inputs.x += Axis(controls.rotateRight.GetControlBinding(), controls.rotateLeft.GetControlBinding());

            inputs.x = Mathf.Clamp(inputs.x, -1, 1);
        }

        // ROTATE LEFT AND RIGHT
        if (steer)
            rotation = Input.GetAxis("Mouse X") * mainCam.CameraSpeed;
        else
            rotation = Axis(controls.rotateRight.GetControlBinding(), controls.rotateLeft.GetControlBinding());

        // Toggle walking mode  USE PGDOWN to toggle it
        if (controls.walkRun.GetControlBindingDown())
            run = !run;

        // Jumping
        jump = controls.jump.GetControlBinding();

        inputNormalized = inputs.normalized;
    }

    public float Axis(bool pos, bool neg)
    {
        float axis = 0;

        if (pos)
            axis += 1;

        if (neg)
            axis -= 1;

        return axis;
    }

    void DebugGroundNormals()
    {
        Vector3 lineStart = transform.position + Vector3.up * 0.05f;

        if (showMoveDirection)
            Debug.DrawLine(lineStart, lineStart + moveDirection.forward * 0.5f, Color.cyan);

        if (showForwardDirection)
            Debug.DrawLine(lineStart - groundDirection.forward * 0.5f, lineStart + groundDirection.forward * 0.5f, Color.blue);

        if (showStrafeDirection)
            Debug.DrawLine(lineStart - groundDirection.right * 0.5f, lineStart + groundDirection.right * 0.5f, Color.red);

        if (showFallNormal)
            Debug.DrawLine(lineStart, lineStart + fallDirection.up * 0.5f, Color.green);
    }   
    
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.point.y <= transform.position.y + 0.25f)
        {
            collisionPoint = hit.point;
            collisionPoint = collisionPoint - transform.position;
        }
    }
}
