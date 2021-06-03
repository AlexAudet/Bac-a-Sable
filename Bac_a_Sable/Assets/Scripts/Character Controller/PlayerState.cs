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
    float moveAmount;
    float turnAmount;
    float canJumpTimer;
    bool sprinting;

    public override void Enter()
    {
        CameraController.Instance.updateMode = CameraController.UpdateMode.LateUpdate;
        lastPosition = player.transform.position;
        player.rigid.isKinematic = true;
        player.currentAirJumpAmount = 0;

        player.transform.rotation = player.TargetRotation(player.TargetDirection(), instantTurn: true);

        canJumpTimer = 0;
        base.Enter();
    }

    public override void Update()
    {
        player.LookTarget();
        targetDir = player.TargetDirection();
        sprinting = player.Sprint();
        turnAmount = player.TurnAmount();
        slopeData = player.CheckGroundSlope(player.transform.position);

        if (slopeData.slopeAngle < player.minSlopeAffectSpeed)
            moveAmount = player.MoveAmount(true);
        else
        {
            moveAmount = player.MoveAmountOnSlope(slopeData, true);

            if (slopeData.slopeAngle > player.maxSlopeWalkable)
            {
                moveAmount = 0;

                nextState = new Sliding(player, anim);
                stage = EVENT.EXIT;
            }
        }

              
        player.transform.position += (anim.GetBool("IsRolling") == true ? 1.75f : 1) * anim.deltaPosition * (sprinting ? player.sprintSpeed : player.speed);

        if(anim.GetBool("IsRolling") == false)
            player.transform.rotation = player.TargetRotation(targetDir);


        canJumpTimer += Time.deltaTime;

        anim.SetBool("IsGrounded", player.grounded);
        anim.SetBool("RightFootForward", player.RightFootForward());
        anim.SetFloat("Forward", moveAmount);
        anim.SetFloat("Turn", turnAmount);

        if(player.Roll() && canJumpTimer >= 0.2f)
        {   
           if (!sprinting)
           {
               if (moveAmount > 0.2f)
                   anim.CrossFade("Roll_Move", 0.1f);
               else
                   anim.CrossFade("Roll_Idle", 0.1f);
           }
           else
               anim.CrossFade("Dash_Grounded", 0.1f);
        }


        if (player.Jump() && canJumpTimer >= 0.4f)
        {
            nextState = new Jump(player, anim, moveAmount, sprinting, false);
            stage = EVENT.EXIT;
        }

        currentVelocity = (player.transform.position - lastPosition) / Time.fixedDeltaTime;
        lastPosition = player.transform.position;
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

    float moveAmount;
    float fallTimer;

    Vector3 velocityForce;

    public override void Enter()
    {
        CameraController.Instance.updateMode = CameraController.UpdateMode.FixedUpdate;
        player.rigid.isKinematic = false;
        anim.SetBool("IsGrounded", false);
        base.Enter();
    }
    public override void Update()
    {
        moveAmount = player.MoveAmount();
        velocityForce = player.TargetDirection(notRotTransform: true) * player.airControlSpeed;
        velocityForce.y = player.gravityForce;

        player.rigid.AddForce(velocityForce, ForceMode.Acceleration);

        player.transform.rotation = player.TargetRotation(player.TargetDirection(true));


        fallTimer += Time.deltaTime;

        anim.SetFloat("YVelocity", player.rigid.velocity.y);
        anim.SetFloat("Forward", moveAmount);
        anim.SetFloat("FallTime", fallTimer);

        if (player.Jump() && player.currentAirJumpAmount < player.airJumpAmount)
        {
            player.rigid.velocity = Vector3.zero;
            nextState = new Jump(player, anim, moveAmount, player.Sprint(), true);
            stage = EVENT.EXIT;
        }

        if (player.Roll())
        {
            anim.CrossFade("Dash_Air", 0.1f);
        }
  
        player.CheckIfGrounded();
        if (player.grounded)
        {
            anim.SetBool("IsGrounded", true);
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
    public Jump(PlayerController _player, Animator _anim, float _moveAmount, bool _sprinting, bool _otherJump)
        : base(_player, _anim) { moveAmount = _moveAmount; sprinting = _sprinting; otherJump = _otherJump; }

    bool sprinting;
    bool otherJump;
    float moveAmount;
    float timer;
    float fallTimer;

    Vector3 jumpDirection;
    Vector3 velocityForce;

    public override void Enter()
    {   
        CameraController.Instance.updateMode = CameraController.UpdateMode.FixedUpdate;
        player.rigid.isKinematic = false;
        player.rigid.velocity = Vector3.zero;
        player.currentAirJumpAmount++;

        anim.SetBool("Jump", !otherJump);
        anim.SetBool("OtherJump", otherJump);
        anim.SetBool("IsGrounded", false);

        jumpDirection = Vector3.up * player.jumpForce;
        jumpDirection += player.transform.forward * 4.5f * (moveAmount * 2) * (sprinting ? player.sprintSpeed : player.speed);

        player.rigid.AddForce(jumpDirection, ForceMode.VelocityChange);

        timer = 0;

        base.Enter();
    }
    public override void Update()
    {
        moveAmount = player.MoveAmount();
        velocityForce = player.TargetDirection(notRotTransform: true) * player.airControlSpeed;
        velocityForce.y = player.gravityForce;


        if (anim.GetBool("IsRolling") == false)
        {
            player.rigid.AddForce(velocityForce, ForceMode.Acceleration);

            if (player.Roll())
            {
                player.transform.rotation = player.TargetRotation(player.TargetDirection(baseOnlyOnCam : true), instantTurn : true);
                anim.Play("Dash_Air");
            }
        }
        else
        {
            player.transform.position += 2 * anim.deltaPosition;
            player.rigid.velocity = Vector3.zero;
        }
            
        if (anim.GetBool("IsRolling") == false)
            player.transform.rotation = player.TargetRotation(player.TargetDirection(true));

        timer += Time.deltaTime;
        fallTimer += Time.deltaTime;

        anim.SetFloat("YVelocity", player.rigid.velocity.y);
        anim.SetFloat("Forward", moveAmount);
        anim.SetFloat("FallTime", fallTimer);

        if (player.Jump() && timer >= 0.05f && player.currentAirJumpAmount < player.airJumpAmount)
        {
            player.rigid.velocity = Vector3.zero;
            nextState = new Jump(player, anim, moveAmount, sprinting, !otherJump);
            stage = EVENT.EXIT;
        }

       
          
        if (player.rigid.velocity.y < -0.2f || timer >= 0.2f)
        {
            anim.SetBool("Jump", false);
            anim.SetBool("OtherJump", false);

            player.CheckIfGrounded();
            if (player.grounded)
            {
                anim.SetBool("IsGrounded", true);
                nextState = new NormalMovement(player, anim);
                stage = EVENT.EXIT;
            }
        }                     
    }

    public override void Exit()
    {

        base.Exit();
    }
}