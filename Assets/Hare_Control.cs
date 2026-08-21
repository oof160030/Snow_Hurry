using UnityEngine;
using System.Collections;
using TMPro;

public class Hare_Control : MonoBehaviour
{
    //Access to components
    private Rigidbody2D RB2;
    public LayerMask LM;

    //Ground Movement - Physics Variables
    [Header("Standard Physics Variables")]
    public float dirtWalkSpeed;
    public float dirtDashSpeed, dirtJumpForce;
    public float sharpGravRate, hangGravRate, gravTransitionHangBound, gravTransitionSharpBound, maxFallSpeed;

    //Ice Movement - Physics Variables
    [Header("Ice Physics Variables")]
    public float skateChargeRate;
    public float skateDecayRate, brakeChargeRate;
    public float skateSpeedLow, skateSpeedMedium, skateSpeedHigh, brakeSpeed;
    public float skateJumpForce, skateAirAccel;
    private int skateGear;
    private float skateCharge, currentSkateSpeed;

    [Header("Universal Movement Variables")]
    public bool grounded;
    public bool lookingRight;
    public float maxDashDuration;
    public float dashSpeedMultiplier;
    public int variableJumpMultiplier;
    public float maxCoyoteTime;
    private float coyoteTime;
    private bool canAirDash;
    private bool justGrounded, justAirborne;
    private bool onIce;

    //Input variables
    private int xIn, yIn;
    private KeyCode Jump, Dash;
    private float jumpBuffer, dashBuffer;
    private bool jumpRelease;

    [Header("Debug Variables")]
    public float velY;
    [Range(-1, 1)]
    public float displaySkateCharge;
    public TextMeshProUGUI debugtext;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RB2 = GetComponent<Rigidbody2D>();
        grounded = false;
        Jump = KeyCode.Space;
        Dash = KeyCode.LeftShift;
        jumpBuffer = 0;
        dashBuffer = 0;
        skateCharge = 0;
        canAirDash = true;
        jumpRelease = false;
        coyoteTime = 0;
    }

    // Update is called once per frame
    void Update()
    {
        //Get keyboard inputs
        xIn = (Input.GetKey(KeyCode.LeftArrow) ? -1 : 0) + (Input.GetKey(KeyCode.RightArrow) ? 1 : 0);
        yIn = (Input.GetKey(KeyCode.DownArrow) ? -1 : 0) + (Input.GetKey(KeyCode.UpArrow) ? 1 : 0);

        if (xIn != 0)
        {
            //If not on ice (or on ice and not moving), automatically look in direction of input
            if(!onIce || (onIce && grounded && skateGear == 0) )
                lookingRight = xIn > 0;

            //if on ice & grounded, our looking direction changes when we dash in a new direction
            else if(onIce && grounded)
            {
                //handled elsewhere
            }

            //if on ice and airborne, our looking direction changes when our velocity changes
            else if(onIce && !grounded && RB2.linearVelocityX != 0)
            {
                lookingRight = RB2.linearVelocityX > 0;
            }
        }
            

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
            jumpRelease = false;

            //Then start couroutine
            StartCoroutine("JumpBuffer");
        }

        if (Input.GetKeyUp(Jump) && !jumpRelease)
            jumpRelease = true;

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
            justGrounded = true;
            canAirDash = true;
            coyoteTime = maxCoyoteTime;
        }
        else if(grounded && !hit)
        {
            //We were grounded, but should become airborne; Change state!
            grounded = false;
            justAirborne = true;
        }

        //If we are grounded, we also want to determine which type of surface we are on (dirt or ice)
        if(hit)
        {
            bool prevIce = onIce;
            onIce = hit.transform.gameObject.CompareTag("ice");

            //if moving onto ice for first time, reset basic ice mechanics
            if(onIce && !prevIce)
            {
                skateGear = 1;
                skateCharge = 0;
                currentSkateSpeed = skateSpeedLow;
            }
        }

        if(!grounded && coyoteTime > 0)
        {
            coyoteTime = Mathf.Clamp(coyoteTime - Time.fixedDeltaTime, 0, maxCoyoteTime);
        }
        //Remaining methods change based on dirt or ice!

        //SECOND (DIRT VER.) - we set our desired velocity
        if (!onIce)
        {
            //HORIZONTAL: Instant control whether on ground or air
            //But can control direction only when not dashing (dash buffer is 0 or greater)
            if(dashBuffer >= 0)
                RB2.linearVelocityX = xIn * dirtWalkSpeed;

            //Check if we got a dash input
            if(dashBuffer > 0)
            {
                //Always permit dash on the ground
                if(grounded)
                {
                    RB2.linearVelocityX = dirtDashSpeed * (lookingRight ? 1 : -1);
                    //Then reset dash input buffer
                    ClearDashBuffer();
                }
                //In the air, only allow once per airtime
                else if (canAirDash)
                {
                    canAirDash = false;
                    RB2.linearVelocityY = 0;
                    RB2.linearVelocityX = dirtDashSpeed * (lookingRight ? 1 : -1);
                    //Then reset dash input buffer
                    ClearDashBuffer();
                }
            }

            //VERTICAL: Jump input is a short hop (may split jumping into a separate method)
            if((grounded || coyoteTime > 0) && jumpBuffer > 0)
            {
                RB2.linearVelocityY = dirtJumpForce; //Change to weak ground jump force
                grounded = false;
                coyoteTime = -1;

                //End coroutine
                StopCoroutine("JumpBuffer");
                jumpBuffer = 0;
            }
        }

        //SECOND (ICE VER.) - we set our desired velocity
        //Set desired horizontal velocity based on x input (depending on if we are grounded
        else
        {
            //If we JUST landed, and we are holding a direction, set our looking direction based on the direction
            if (justGrounded && xIn != 0)
                lookingRight = xIn > 0;

            //Easy way to exit gear 0: hold left or right while either at full charge or as you land!
            if(skateGear == 0 & xIn != 0 && (skateCharge == 1 || justGrounded))
            {
                skateGear = 1;
                currentSkateSpeed = skateSpeedLow;
                skateCharge = 0;
            }

            //HORIZONTAL: if grounded, horizontal movement is automatic based on facing
            if(grounded && dashBuffer >= 0)
                RB2.linearVelocityX = currentSkateSpeed * (lookingRight ? 1 : -1);

            //May later allow even higher momentum preservation in gear 3?
            //Otherwise, air control is acceleration based
            else if(dashBuffer >= 0)
            {
                float hSpeed = RB2.linearVelocityX + (xIn * skateAirAccel * Time.fixedDeltaTime);
                //Clamp value depends on our gear
                float clampVal = skateGear == 0 ? skateSpeedLow : currentSkateSpeed;
                //But if we are going faster than the clamp, allow it (but still can't accelerate)
                if (Mathf.Abs(RB2.linearVelocityX) > clampVal)
                    clampVal = Mathf.Abs(RB2.linearVelocityX);
                hSpeed = Mathf.Clamp(hSpeed, -clampVal, clampVal);
                RB2.linearVelocityX = hSpeed;
            }

            //Ground Dash (Gear Up, Direction Change): A dash input (if valid) can do two things::
            if (dashBuffer > 0 && grounded)
            {
                //Gear Up: If charge is full (or if not moving), increase gear
                if(skateGear == 0)
                {
                    skateGear = 1;
                    currentSkateSpeed = skateSpeedLow;
                    RB2.linearVelocityX = currentSkateSpeed * (lookingRight ? 1 : -1) * dashSpeedMultiplier;
                    skateCharge = 0;
                    ClearDashBuffer();
                }
                else if(skateGear != 3 && skateCharge == 1)
                {
                    skateGear++;
                    currentSkateSpeed = skateGear == 2 ? skateSpeedMedium : skateSpeedHigh;
                    RB2.linearVelocityX = currentSkateSpeed * (lookingRight ? 1 : -1) * dashSpeedMultiplier;
                    skateCharge = 0;
                    ClearDashBuffer();
                }
                //Direction Change: If holding back when dashing, change direction (& temporary speed boost)
                else if(xIn != 0 && (xIn > 0 != lookingRight))
                {
                    lookingRight = xIn > 0;
                    skateCharge = 0;
                    RB2.linearVelocityX = currentSkateSpeed * (lookingRight ? 1 : -1) * dashSpeedMultiplier;
                    ClearDashBuffer();
                }
                //Max Velocity: If holding forwards, but charge wasn't full, fill charge (& temporary speed boost)
                else
                {
                    skateCharge = Mathf.Clamp(skateCharge + 0.1f, -1, 1);
                    RB2.linearVelocityX = currentSkateSpeed * (lookingRight ? 1 : -1) * dashSpeedMultiplier;
                    ClearDashBuffer();
                }
            }

            //Air Dash: A dash input in the air will just change our direction and lock in speed
            //Also check if air dash is allowed (only one per airtime!)
            else if(dashBuffer > 0 && !grounded && canAirDash)
            {
                //If our current gear is 0, immediately gear up to 1. Then...
                if (skateGear == 0)
                {
                    skateGear = 1;
                    currentSkateSpeed = skateSpeedLow;
                }

                // ...if holding a direction, dash in that direction (with speed boost)
                if (xIn != 0)
                {
                    RB2.linearVelocityX = currentSkateSpeed * xIn * dashSpeedMultiplier;
                    //And update facing if needed
                    lookingRight = xIn > 0;
                }
                // or if not holding a direction, dash in the direction we are facing
                else
                {
                    RB2.linearVelocityX = currentSkateSpeed * (lookingRight ? 1 : -1) * dashSpeedMultiplier;
                }

                skateCharge = 0;
                ClearDashBuffer();
                RB2.linearVelocityY = 0;
                canAirDash = false;
            }

            //Charge and Brake Functions
            if(grounded)
            {
                //Charge: if we are holding forwards and on the ground, we can build charge
                if (xIn != 0 && (xIn > 0 == lookingRight))
                {
                    //Gain skate charge
                    skateCharge = Mathf.Clamp(skateCharge + skateChargeRate * Time.fixedDeltaTime, -1, 1);
                }
                //Brake: If we are holding backwards and on the ground, reduce & lose charge
                else if (xIn != 0 && (xIn > 0 != lookingRight))
                {
                    //Override speed with brake speed
                    RB2.linearVelocityX = brakeSpeed * (lookingRight ? 1 : -1);

                    //Reduce charge
                    skateCharge = Mathf.Clamp(skateCharge - brakeChargeRate * Time.fixedDeltaTime, -1, 1);

                    //And if brake charge hits -1, reduce gear
                    if(skateCharge == -1)
                    {
                        skateGear--;
                        currentSkateSpeed = GetSkateSpeed(skateGear);
                        skateCharge = 0;
                    }
                }
                //Charge Decay (if no input held, charge will eventually die)
                else if (xIn == 0 && skateCharge != 0)
                {
                    if (skateCharge > 0)
                        skateCharge = Mathf.Clamp(skateCharge - skateDecayRate * Time.fixedDeltaTime, 0, 1);
                    else
                        skateCharge = Mathf.Clamp(skateCharge + skateDecayRate * Time.fixedDeltaTime, -1, 0);
                }
            }

            //VERTICAL: Jump input is a short hop (may split jumping into a separate method)
            if ((grounded || coyoteTime > 0) && jumpBuffer > 0)
            {
                RB2.linearVelocityY = skateJumpForce;
                grounded = false;
                coyoteTime = -1;

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

            
            //Variable gravity rate: if not yet in hang gravity (range > 0), and jump button released, double gravity
            float varJumpFactor = (RB2.linearVelocityY > 0 && range > 0 && jumpRelease ? variableJumpMultiplier : 1);

            //Calculate scaled gravity based on vertical velocity and whether jump input is still being held
            float gravScale = hangGravRate + range * ((sharpGravRate * varJumpFactor) - hangGravRate);

            //Apply gravity when airborne
            if (RB2.linearVelocityY > -maxFallSpeed)
            {
                RB2.linearVelocityY = Mathf.Clamp(RB2.linearVelocityY - (gravScale * Time.fixedDeltaTime), -maxFallSpeed, 1000);
            }
            //RB2.linearVelocityY -= sharpGravRate * Time.fixedDeltaTime;
        }

        if(justGrounded || justAirborne)
        {
            justGrounded = false;
            justAirborne = false;
        }

        velY = RB2.linearVelocityY;
        displaySkateCharge = skateCharge;
        debugtext.text = "Debug Stats:\nSktGr: " + skateGear + "\nCharge : " + skateCharge.ToString("0.00");
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

    private void ClearDashBuffer()
    {
        //Stop coroutine (just in case)
        StopCoroutine("DashBuffer");

        //Set buffer value
        dashBuffer = -maxDashDuration;

        //Then start couroutine
        StartCoroutine("DashBuffer");
    }

    private float GetSkateSpeed(int gear)
    {
        switch(gear)
        {
            case 0:
                return 0;
            case 1:
                return skateSpeedLow;
            case 2:
                return skateSpeedMedium;
            case 3:
                return skateSpeedHigh;
            default:
                return 0;
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
