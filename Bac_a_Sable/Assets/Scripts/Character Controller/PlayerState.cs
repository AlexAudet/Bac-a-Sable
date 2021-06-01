using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState
{

    public enum EVENT
    {
        ENTER, UPDATE, EXIT
    };

    protected EVENT stage;
    protected Animator anim;
    protected PlayerController player;
    protected PlayerState nextState;


    public PlayerState(PlayerController _player, Animator _anim)
    {
        player = _player;
        anim = _anim;
        stage = EVENT.ENTER;

    }

    public virtual void Enter() { stage = EVENT.UPDATE; }
    public virtual void Update() { stage = EVENT.UPDATE; }
    public virtual void Exit() { stage = EVENT.EXIT; }

    public PlayerState Process()
    {
        if (stage == EVENT.ENTER) Enter();
        if (stage == EVENT.UPDATE) Update();
        if (stage == EVENT.EXIT)
        {
            Exit();
            return nextState;
        }

        return this;
    }

    public float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}

public class NormalMovement : PlayerState
{
    public NormalMovement(PlayerController _player, Animator _anim)
        : base(_player, _anim){ }

    OnSlopeData slopeData = new OnSlopeData();
    Vector3 targetDir;
    Vector3 lastPosition;
    Vector3 currentVelocity;
    float targetMoveAmount;
    float moveAmount;
    float turnAmount;
    bool sprinting;

    public override void Enter()
    {
        CameraController.Instance.updateMode = CameraController.UpdateMode.LateUpdate;
        lastPosition = player.transform.position;
        player.rigid.isKinematic = true;
        base.Enter();
    }

    public override void Update()
    {
        player.LookTarget();
        targetDir = player.TargetDirection();
        sprinting = player.Sprint();



        if (player.TouchMovingInput())
        {
            if (!sprinting)
                targetMoveAmount = Vector3.Magnitude(targetDir);
            else
                targetMoveAmount = 2;
        }
        else
        {
            targetMoveAmount = 0;
        }


        if (slopeData.slopeAngle < player.minSlopeAffectSpeed)
        {
           
            moveAmount = Mathf.Lerp(moveAmount, targetMoveAmount, Time.deltaTime * player.acceleration);
            
        }
        else
        {
            float slopDotDirection = Remap(Mathf.Clamp(slopeData.slopeDotDirection, -1, 0), -1, 0, 0, 1);
            float slopeMoveAmount = Mathf.Clamp(Remap(slopeData.slopeAngle, player.minSlopeAffectSpeed, player.maxSlopeWalkable, 1, 0), 0, 1);        
            slopeMoveAmount = Mathf.Clamp(slopeMoveAmount + slopDotDirection, 0, 1);
            slopeMoveAmount *= targetMoveAmount;

            if (slopeData.slopeAngle < player.maxSlopeWalkable)
            {
                if (slopeData.slopeDotDirection < 1)
                    moveAmount = Mathf.Lerp(moveAmount, slopeMoveAmount, Time.deltaTime * player.overSlopeDecceleration);
                else
                    moveAmount = Mathf.Lerp(moveAmount, targetMoveAmount * Remap(slopeData.slopeAngle, player.minSlopeAffectSpeed, player.maxSlopeWalkable, 1, 2), Time.unscaledDeltaTime * player.rotationSpeed);
            }
            else
            {
                moveAmount = Mathf.Lerp(moveAmount, 0, Time.unscaledDeltaTime * player.overSlopeDecceleration);

                if (moveAmount < 0.1f)
                {
                    moveAmount = 0;

                    nextState = new Sliding(player, anim);
                    stage = EVENT.EXIT;
                }
            }
        }

        if ((player.TargetRotation(targetDir).eulerAngles - player.transform.eulerAngles).sqrMagnitude > 100)
        {
            float targetTurnAmount = Vector3.Dot(player.transform.right, targetDir);

            turnAmount = Mathf.Lerp(turnAmount, targetTurnAmount, Time.unscaledDeltaTime * 10);
        }
        else
        {
            turnAmount = Mathf.Lerp(turnAmount, 0, Time.unscaledDeltaTime * 10);
        }
           

        player.transform.position += anim.deltaPosition * (sprinting ? player.sprintSpeed : player.speed);
        player.transform.rotation = player.TargetRotation(targetDir);

  

        slopeData = player.CheckGroundSlope(player.transform.position);
        anim.SetBool("IsGrounded", player.grounded);
        anim.SetBool("RightFootForward", player.RightFootForward());
        anim.SetFloat("Forward", moveAmount);
        anim.SetFloat("Turn", turnAmount);


        currentVelocity = (player.transform.position - lastPosition) / Time.fixedDeltaTime;
        lastPosition = player.transform.position;

        if (player.Jump())
        {
            nextState = new Jump(player, anim, moveAmount, sprinting);
            stage = EVENT.EXIT;
        }


        player.CheckIfGrounded();
        if (player.grounded == false)
        {
            player.rigid.isKinematic = false;
            anim.SetBool("IsGrounded", false);
            CameraController.Instance.updateMode = CameraController.UpdateMode.FixedUpdate;

            currentVelocity = player.transform.forward * 3 * Mathf.Clamp(moveAmount, 0.5f, 2) * (sprinting ? player.sprintSpeed : player.speed);
       
            currentVelocity.y += 3;
            player.rigid.AddForce(currentVelocity, ForceMode.VelocityChange);
            nextState = new NotGrounded(player, anim);
            stage = EVENT.EXIT;
        }

    }

    public override void Exit()
    {
        base.Exit();
    }
}

public class LockOnMovement : PlayerState
{
    public LockOnMovement(PlayerController _player, Animator _anim)
        : base(_player, _anim){ }


    public override void Enter()
    {
        anim.SetBool("LockOn", true);
        base.Enter();
    }

    Vector3 targetDir;
    float vertical;
    float horizontal;
    float targetMoveAmount;
    float moveAmount;
    float turnAmount;

    public override void Update()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        targetDir = player.camTransform.forward * vertical;
        targetDir += player.camTransform.right * horizontal;
        targetDir.y = 0;

        Vector3 loockRot = player.camTransform.forward;
        loockRot.y = player.transform.position.y;
        player.transform.rotation = player.TargetRotation(loockRot);

        moveAmount = Mathf.Lerp(moveAmount, vertical, Time.unscaledDeltaTime * player.acceleration);
        turnAmount = Mathf.Lerp(turnAmount, horizontal, Time.unscaledDeltaTime * player.acceleration);

        player.lookTarget.position = Vector3.Lerp(player.lookTarget.position, (player.camTransform.position + player.camTransform.forward * 10), Time.deltaTime * 8);
    }

    public override void Exit()
    {
        anim.SetBool("LockOn", false);
        base.Exit();
    }
}

public class Sliding : PlayerState
{
    public Sliding(PlayerController _player, Animator _anim)
        : base(_player, _anim){}

    OnSlopeData slopeData;
    float slideSpeed;
    float targetSlideSpeed;

    public override void Enter()
    {
        
        base.Enter();
    }


    public override void Update()
    {
        slopeData = player.CheckGroundSlope(player.transform.position);

        targetSlideSpeed = Remap(slopeData.slopeAngle, player.maxSlopeWalkable, 90, player.slidingSpeedRange.x, player.slidingSpeedRange.y);


        slideSpeed = Mathf.Lerp(slideSpeed, targetSlideSpeed, Time.deltaTime * player.slideingAcceleration);

        player.transform.position += slopeData.slopeDirection.normalized * Time.deltaTime * slideSpeed;

        Vector3 lookDirection = slopeData.slopeDirection;
        lookDirection.y = 0;

        player.transform.rotation = player.TargetRotation(lookDirection, true);
    }

    public override void Exit()
    {
        base.Exit();
    }
}

public class NotGrounded : PlayerState
{
    public NotGrounded(PlayerController _player, Animator _anim)
        : base(_player, _anim){}

    Vector3 targetDir;

    float targetMoveAmount;
    float moveAmount;
    float timer;

    public override void Enter()
    {
        CameraController.Instance.updateMode = CameraController.UpdateMode.FixedUpdate;
        player.rigid.isKinematic = false;
        anim.SetBool("IsGrounded", false);
        base.Enter();
    }
    public override void Update()
    {
        targetDir = player.TargetDirection();
        if (player.TouchMovingInput())
            targetMoveAmount = Vector3.Magnitude(targetDir);
        else
            targetMoveAmount = 0;

        moveAmount = Mathf.Lerp(moveAmount, targetMoveAmount, Time.deltaTime * player.acceleration);

        player.rigid.AddForce(new Vector3(0, player.gravityForce, 0), ForceMode.Acceleration);

        timer += Time.deltaTime;

        anim.SetFloat("TimeFall", timer);
        anim.SetFloat("Forward", moveAmount);

        player.CheckIfGrounded();
        if (player.grounded)
        {
            nextState = new NormalMovement(player, anim);
            stage = EVENT.EXIT;
        }
    }

    public override void Exit()
    {
        anim.SetBool("IsGrounded", true);
        base.Exit();
    }
}

public class Jump : PlayerState
{
    public Jump(PlayerController _player, Animator _anim, float _moveAmount, bool _sprinting)
        : base(_player, _anim) { moveAmount = _moveAmount; sprinting = _sprinting; }

    bool sprinting;

    float targetMoveAmount;
    float moveAmount;
    float timer;

    Vector3 targetDir;
    Vector3 jumpDirection;

    public override void Enter()
    {   
        CameraController.Instance.updateMode = CameraController.UpdateMode.FixedUpdate;
        player.rigid.isKinematic = false;

        anim.SetBool("Jump", true);
        anim.SetBool("IsGrounded", false);

        jumpDirection = Vector3.up * player.jumpForce;
        jumpDirection += player.transform.forward * 3 * (moveAmount * 2) * (sprinting ? player.sprintSpeed : player.speed);

        player.rigid.AddForce(jumpDirection, ForceMode.VelocityChange);

        base.Enter();
    }
    public override void Update()
    {
        targetDir = player.TargetDirection();
        if (player.TouchMovingInput())
            targetMoveAmount = Vector3.Magnitude(targetDir);
        else
            targetMoveAmount = 0;

        moveAmount = Mathf.Lerp(moveAmount, targetMoveAmount, Time.deltaTime * player.acceleration);

        player.rigid.AddForce(new Vector3(0, player.gravityForce, 0), ForceMode.Acceleration);

        anim.SetFloat("YVelocity", player.rigid.velocity.y);
        anim.SetFloat("Forward", moveAmount);

        timer += Time.deltaTime;

        if (player.rigid.velocity.y < -0.2f || timer >= 0.2f)
        {
            player.CheckIfGrounded();
            if (player.grounded)
            {
                Debug.Log(anim.GetFloat("Forward"));
                anim.SetBool("Jump", false);
                nextState = new NormalMovement(player, anim);
                stage = EVENT.EXIT;
            }
        }
       
               
    }

    public override void Exit()
    {
        anim.SetBool("Jump", false);

        base.Exit();
    }
}