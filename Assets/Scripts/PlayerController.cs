using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 direction;
    private Animator anim;
    private CinemachineVirtualCamera cm;
    private Vector2 moveDirection;

    [Header("Player Statistics")]
    public float speedMovement = 10;
    public float jumpStrength = 5;
    public float dashSpeed = 20;

    [Header("Player Collisions")]
    public LayerMask layerFloor;
    public Vector2 downPosition;
    public float collisionRadius;

    [Header("Player booleans")]
    public bool canMove = true;
    public bool onGround = true;
    public bool canDash;
    public bool dashing = false;
    public bool hitingGround;
    public bool shaking = false;
    public bool isAttacking = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        cm = GameObject.FindGameObjectWithTag("Virtual Camera").GetComponent<CinemachineVirtualCamera>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Move();
        FloorChecker();
    }

    private void FloorChecker()
    {
        onGround = Physics2D.OverlapCircle((Vector2)transform.position + downPosition, collisionRadius, layerFloor);
    }

    /***********************
     *                     *
     *   Camera settings   *
     *                     *
     ***********************/
    private IEnumerator ShakeCamera()
    {
        shaking = true;
        CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin = cm.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = 5;

        yield return new WaitForSeconds(0.3f);

        cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = 0;
        shaking = false;
    }

    private IEnumerator ShakeCamera(float time)
    {
        shaking = true;
        CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin = cm.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = 5;

        yield return new WaitForSeconds(time);

        cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = 0;
        shaking = false;
    }
    // END camera settings


    /************
     *          *
     *   Dash   *
     *          *
     ************/
    private void Dash(float x, float y)
    {
        anim.SetBool("dash", true);

        Vector3 playerPosition = Camera.main.WorldToViewportPoint(transform.position);
        Camera.main.GetComponent<RippleEffect>().Emit(playerPosition);

        StartCoroutine(ShakeCamera());

        canDash = true;

        rb.velocity = Vector2.zero;
        rb.velocity += new Vector2(x, y).normalized * dashSpeed;

        StartCoroutine(PrepareDash());
    }

    private IEnumerator PrepareDash()
    {
        StartCoroutine(DashGround());

        float gravityScale = rb.gravityScale;

        rb.gravityScale = 0;

        dashing = true;

        yield return new WaitForSeconds(0.3f);

        rb.gravityScale = gravityScale;

        dashing = false;

        EndDash();
    }

    private IEnumerator DashGround()
    {
        yield return new WaitForSeconds(0.15f);

        if (onGround)
        {
            canDash = false;
        }    
    }

    public void EndDash()
    {
        anim.SetBool("dash", false);
    }
    // END: Dash


    /************************
     *                      * 
     *   Player movements   *
     *                      *
     ************************/
    private void Move()
    {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");

        float xRaw = Input.GetAxisRaw("Horizontal");
        float yRaw = Input.GetAxisRaw("Vertical");

        direction = new Vector2(x, y);
        Vector2 directionRaw = new Vector2(xRaw, yRaw);

        Walk();

        Attack(AttackDirection(moveDirection, directionRaw));

        JumpUpgraded();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (onGround)
            {
                anim.SetBool("jump", true);
                Jump();
            }
        }

        if (Input.GetKeyDown(KeyCode.X) && !dashing)
        {
            if(xRaw != 0 || yRaw != 0)
            {
                Dash(xRaw, yRaw);
            }
        }

        if (onGround && !hitingGround)
        {
            HitGround();
            hitingGround = true;
        }

        if (!onGround && hitingGround)
        {
            hitingGround = false;
        }

        float vSpeed;

        if (rb.velocity.y > 0)
        {
            vSpeed = 1;
        }
        else
        {
            vSpeed = -1;
        }

        if (!onGround)
        {
            anim.SetFloat("verticalSpeed", vSpeed);
        }
        else
        {
            if (vSpeed == -1)
            {
                JumpEnded();
            }
        }
    }

    private void Walk()
    {
        if (canMove && !dashing)
        {
            rb.velocity = new Vector2(direction.x * speedMovement, rb.velocity.y);

            if (direction != Vector2.zero)
            {
                if (!onGround)
                {
                    anim.SetBool("jump", true);
                }
                else
                {
                    anim.SetBool("walk", true);
                }

                if (direction.x < 0 && transform.localScale.x > 0)
                {
                    moveDirection = AttackDirection(Vector2.left, direction);
                    transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
                }
                else if (direction.x > 0 && transform.localScale.x < 0)
                {
                    moveDirection = AttackDirection(Vector2.right, direction);
                    transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
                }
            }
            else
            {
                if (direction.y > 0 && direction.x == 0)
                {
                    moveDirection = AttackDirection(direction, Vector2.up);
                }
                anim.SetBool("walk", false);
            }
        }

    }
    // END: Player movements

    /************
     *          *
     *   Jump   *
     *          *
     ************/
    public void JumpEnded()
    {
        anim.SetBool("jump", false);
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.velocity += Vector2.up * jumpStrength;
    }

    private void JumpUpgraded()
    {
        if (rb.velocity.y < 0) {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (2.5f - 1) * Time.deltaTime;
        } else if (rb.velocity.y > 0 && !Input.GetKey(KeyCode.Space)) {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (2.0f - 1) * Time.deltaTime;
        }
    }

    private void HitGround()
    {
        canDash = false;
        dashing = false;
        anim.SetBool("jump", false);
    }
    // END: Jump

    /**************
     *            *
     *   Attack   *
     *            *
     **************/
    private void Attack(Vector2 direction)
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (!isAttacking && !dashing)
            {
                isAttacking = true;
                anim.SetFloat("attackX", direction.x);
                anim.SetFloat("attackY", direction.y);

                anim.SetBool("attack", true);
            }
        }
    }

    public void AttackEnd()
    {
        anim.SetBool("attack", false);
        isAttacking = false;
    }

    private Vector2 AttackDirection(Vector2 moveDirection, Vector2 direction)
    {
        if (rb.velocity.x == 0 && direction.y != 0)
        {
            return new Vector2(0, direction.y);
        }

        return new Vector2(moveDirection.x, direction.y);
    }
    // END: Attack
}
