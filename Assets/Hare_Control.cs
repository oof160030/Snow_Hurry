using UnityEngine;
using System.Collections;

public class Hare_Control : MonoBehaviour
{
    //Access to components
    private Rigidbody2D RB2;
    public LayerMask LM;
    
    //Ground Movement - Physics Variables
    public float dirtWalkSpeed, dirtDashSpeed, iceAirAccel, dirtJumpForce;
    public float sharpGravRate, hangGravRate, gravTransitionHangBound, gravTransitionSharpBound, maxFallSpeed;

    //Ice Movement - Physics Variables

    public bool grounded, lookingRight;

    //Skating Movement - Physics Variables

    //State management
    private bool onIce;

    //Input variables
    private int xIn, yIn;
    private KeyCode Jump, Dash;
    private float jumpBuffer, dashBuffer;
    public float maxDashDuration;


    public int overX;  //temporary; used to override movement input
    public float velY;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RB2 = GetComponent<Rigidbody2D>();
        grounded = false;
        Jump = KeyCode.Space;
        Dash = KeyCode.LeftShift;
        jumpBuffer = 0;
        dashBuffer = 0;
    }

    // Update is called once per frame
    void Update()
    {
        //Get keyboard inputs
        xIn = (Input.GetKey(KeyCode.LeftArrow) ? -1 : 0) + (Input.GetKey(KeyCode.RightArrow) ? 1 : 0);
        if (overX == 1 || overX == -1)
            xIn = overX;
        if(xIn != 0)
            lookingRight = xIn > 0;

        if (Input.GetKeyDown(Dash) && dashBuffer == 0) //May change so dashBuffer just has to be <= 0 (so you can spam dashes)
        {
            //Stop coroutine (just in case)
            StopCoroutine("DashBuffer");

            //Set buffer value
            dashBuffer = 0.15f;

            //Then start couroutine
            StartCoroutine("DashBuffer");
        }

        if (Input.GetKeyDown(Jump))
        {
            //Stop coroutine (just in case)
            StopCoroutine("JumpBuffer");

            //Set buffer value
            jumpBuffer = 0.15f;

            //Then start couroutine
            StartCoroutine("JumpBuffer");
        }         

        //Update states?
    }

    private void FixedUpdate()
    {
        //Updates to inputs are handled in "Update" method

        //FIRST - we check whether we are grounded
        RaycastHit2D hit = Physics2D.Raycast(transform.position + (Vector3.up * 0.15f), Vector2.down, 0.3f, LM);
        if(!grounded && hit && RB2.linearVelocityY <= 0)
        {
            //We are airbone (and falling), but should become grounded; Change state!
            grounded = true;
        }
        else if(grounded && !hit)
        {
            //We were grounded, but should become airborne; Change state!
            grounded = false;
        }

        //If we are grounded, we also want to determine which type of surface we are on (dirt or ice)
        if(hit)
            onIce = hit.transform.gameObject.CompareTag("ice");

        //Remaining methods change based on dirt or ice!

        //SECOND (DIRT VER.) - we set our desired velocity
        if (!onIce)
        {
            //HORIZONTAL: Instant control whether on ground or air
            //But can only control direction when not dashing (dash buffer is 0 or greater)
            if(dashBuffer >= 0)
                RB2.linearVelocityX = xIn * dirtWalkSpeed;

            //Check if we got a dash input
            if(dashBuffer > 0)
            {
                // if holding a direction, dash in that direction
                if (xIn != 0)
                    RB2.linearVelocityX = dirtDashSpeed * xIn;
                //If not holding a direction, dash in the direction we are facing
                else
                    RB2.linearVelocityX = dirtDashSpeed * (lookingRight ? 1 : -1);
                //And if in the air, cancel our vertical velocity
                if (!grounded)
                    RB2.linearVelocityY = 0;

                //Then reset our dash state (use negative buffer to determine duration)
                //Stop coroutine (just in case)
                StopCoroutine("DashBuffer");

                //Set buffer value
                dashBuffer = -maxDashDuration;

                //Then start couroutine
                StartCoroutine("DashBuffer");
            }

            //VERTICAL: Jump input is a short hop (may split jumping into a separate method)
            if(grounded && jumpBuffer > 0)
            {
                RB2.linearVelocityY = dirtJumpForce; //Change to weak ground jump force
                grounded = false;

                //End coroutine
                StopCoroutine("JumpBuffer");
                jumpBuffer = 0;
            }
        }

        //SECOND (ICE VER.) - we set our desired velocity
        //Set desired horizontal velocity based on x input (depending on if we are grounded
        else
        {
            //HORIZONTAL: As a test, just keep slippery physics on ice
            float hSpeed = RB2.linearVelocityX + (xIn * iceAirAccel * Time.fixedDeltaTime);
            hSpeed = Mathf.Clamp(hSpeed, -dirtWalkSpeed, dirtWalkSpeed);
            RB2.linearVelocityX = hSpeed;

            //VERTICAL: Jump input is a short hop (may split jumping into a separate method)
            if (grounded && jumpBuffer > 0)
            {
                RB2.linearVelocityY = dirtJumpForce; //Change to weak ground jump force
                grounded = false;

                //End coroutine
                StopCoroutine("JumpBuffer");
                jumpBuffer = 0;
            }
        }

        //THIRD - We apply forces to our velocity (as needed)
        if (!grounded && dashBuffer > (-maxDashDuration) / 2)
        {
            //Check absolute Y velocity against our scale
            float range = Mathf.Clamp((Mathf.Abs(RB2.linearVelocityY) - gravTransitionHangBound) / (gravTransitionSharpBound - gravTransitionHangBound), 0, 1);

            //Calculate scaled gravity
            float gravScale = hangGravRate + range * (sharpGravRate - hangGravRate);

            //Apply gravity when airborne
            if(RB2.linearVelocityY > -maxFallSpeed)
            {
                RB2.linearVelocityY = Mathf.Clamp(RB2.linearVelocityY - (gravScale * Time.fixedDeltaTime), -maxFallSpeed, 1000);
            }
            //RB2.linearVelocityY -= sharpGravRate * Time.fixedDeltaTime;
        }

        velY = RB2.linearVelocityY;
    }

    private void LateUpdate()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position + (Vector3.up * 0.15f), Vector2.down, 0.3f, LM);
        //LAST - we adjust our position (as needed)
        if (grounded && hit)
        {
            //If we are grounded and detect ground, move into "contact"
            transform.position += Vector3.down * (hit.distance - 0.15f);
            RB2.linearVelocityY = 0;
            Debug.DrawLine(transform.position, transform.position + Vector3.down * (hit.distance), Color.red);
        }
        else
        {
            Debug.DrawLine(transform.position, transform.position + Vector3.down * (0.2f), Color.yellow);
        }
    }

    IEnumerator JumpBuffer()
    {
        while (jumpBuffer > 0)
        {
            //Reduce value of jump buffer
            jumpBuffer = Mathf.Clamp(jumpBuffer - Time.deltaTime, 0, 0.20f);

            //Then wait
            yield return null;
        }
        //Ends once jump buffer equals 0
    }

    IEnumerator DashBuffer()
    {
        while (dashBuffer != 0)
        {
            //Reduce value of jump buffer
            if(dashBuffer > 0)
                dashBuffer = Mathf.Clamp(dashBuffer - Time.deltaTime, 0, 0.20f);
            else
                dashBuffer = Mathf.Clamp(dashBuffer + Time.deltaTime, -5, 0);

            //Then wait
            yield return null;
        }
        //Ends once jump buffer equals 0
    }

    /*
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("ground"))
            grounded = true;
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("ground"))
            grounded = false;
    }
    */
}
