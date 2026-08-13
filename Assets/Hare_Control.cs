using UnityEngine;

public class Hare_Control : MonoBehaviour
{
    //Access to components
    private Rigidbody2D RB2;
    public LayerMask LM;
    
    //Standard Movement - Physics Variables
    public float walkSpeed, dashSpeed, airSpeed, jumpForce;
    public float sharpGravRate, hangGravRate, switchPoint;
    public bool grounded, lookingRight;

    //Skating Movement - Physics Variables

    //Input variables
    private int xIn, yIn;
    private KeyCode Jump, Dash;
    private bool jumpBuffer;
    public int overX;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RB2 = GetComponent<Rigidbody2D>();
        grounded = false;
        Jump = KeyCode.Space;
        jumpBuffer = false;
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

        if (Input.GetKeyDown(Jump))
            jumpBuffer = true;

        /*
        //Update velocity / apply forces
        if(grounded)
        {
            //Set horiz movement
            float hSpeed = xIn * walkSpeed;
            float vSpeed = RB2.linearVelocityY;
            if (Input.GetKeyDown(Jump))
            {
                vSpeed = jumpForce;
                grounded = false;
            }

            RB2.linearVelocity = new Vector2(hSpeed, vSpeed - sharpGravRate * Time.deltaTime);
        }
        else
        {
            //Apply lateral acceleration and gravity
            float hSpeed = RB2.linearVelocityX + (xIn * airSpeed * Time.deltaTime);
            hSpeed = Mathf.Clamp(hSpeed, -walkSpeed, walkSpeed);
            RB2.linearVelocity = new Vector2(hSpeed, RB2.linearVelocityY - sharpGravRate * Time.deltaTime);
        }
        */
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

        //SECOND - we set our desired velocity
        //Set desired horizontal velocity based on x input (depending on if we are grounded
        if (grounded)
        {
            RB2.linearVelocityX = xIn * walkSpeed;
        }
        else
        {
            float hSpeed = RB2.linearVelocityX + (xIn * airSpeed * Time.fixedDeltaTime);
            hSpeed = Mathf.Clamp(hSpeed, -walkSpeed, walkSpeed);
            RB2.linearVelocityX = hSpeed;
        }

        //If we received a jump input, set desired vertical speed
        if(grounded && jumpBuffer == true)
        {
            RB2.linearVelocityY = jumpForce;
            grounded = false;
            jumpBuffer = false;
        }

        //THIRD - We apply forces to our velocity (as needed)
        if(!grounded)
        {
            //Apply gravity when airborne
            RB2.linearVelocityY -= sharpGravRate * Time.fixedDeltaTime;
        }

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
