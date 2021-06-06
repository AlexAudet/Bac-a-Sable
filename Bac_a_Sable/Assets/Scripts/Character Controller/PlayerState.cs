﻿using System.Collections;
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
    Vector3 lastPosition;
    Vector3 currentVelocity;
    float moveAmount;
    float turnAmount;
    float canJumpTimer;

    public override void Enter()
    {
        CameraController.Instance.updateMode = CameraController.UpdateMode.LateUpdate;
        lastPosition = player.transform.position;
        player.rigid.isKinematic = true;
        player.currentAirJumpAmount = 0;

        player.transform.rotation = player.TargetRotation(player.TargetDirection());

        canJumpTimer = 0;
        base.Enter();
    }

    public override void Update()
    {
        player.LookTarget();
        slopeData = player.CheckGroundSlope(player.transform.position);
  
        if (slopeData.slopeAngle < player.minSlopeAffectSpeed)
            moveAmount = player.MoveAmount(true);
        else
        {
            moveAmount = player.MoveAmountOnSlope(slopeData, true);

            if (slopeData.slopeAngle > player.maxSlopeWalkable && moveAmount <= 0.1f)
            {
                moveAmount = 0;

                nextState = new Sliding(player, anim);
                stage = EVENT.EXIT;
            }
        }
     
        player.transform.position += (anim.GetBool("IsRolling") == true ? 1.75f : 1) * anim.deltaPosition * (player.Sprint() ? player.sprintSpeed : player.speed);


        canJumpTimer += Time.deltaTime;

        anim.SetBool("IsGrounded", player.grounded);
        anim.SetBool("RightFootForward", player.RightFootForward());
        anim.SetFloat("Forward", moveAmount);
        anim.SetFloat("Turn", player.TurnAmount());

        if (anim.GetBool("IsRolling") == false)
        {
            if(!player.OnAiming())
                player.transform.rotation = player.TargetRotation(player.TargetDirection());
            else
                player.transform.rotation = player.TargetRotation(player.TargetDirection(
                    notRotTransform: !player.TouchMovingInput(), 
                    baseOnlyOnCam: !player.TouchMovingInput()), 
                    instantTurn: !player.TouchMovingInput());

            if (player.Roll() && canJumpTimer >= 0.2f)
            {
                if (!player.Sprint())
                {
                    if (moveAmount > 0.2f)
                        anim.CrossFade("Roll_Move", 0.1f);
                    else
                        anim.CrossFade("Roll_Idle", 0.1f);
                }
                else
                    anim.CrossFade("Dash_Grounded", 0.1f);
            }
        }

        if (player.Jump() && canJumpTimer >= 0.4f)
        {
            Vector3 jumpDirection = Vector3.up * player.jumpUpForce;
            jumpDirection += player.transform.forward * 4.5f * (moveAmount * 2) * (player.Sprint() ? player.sprintSpeed : player.speed);
            nextState = new Jump(player, anim, jumpDirection, false);
            stage = EVENT.EXIT;
        }

        currentVelocity = (player.transform.position - lastPosition) / Time.fixedDeltaTime;
        lastPosition = player.transform.position;
        player.CheckIfGrounded();
        if (player.grounded == false)
        {
            player.rigid.isKinematic = false;
            anim.SetBool("IsGrounded", false);
            anim.SetBool("IsRolling", false);
            CameraController.Instance.updateMode = CameraController.UpdateMode.FixedUpdate;

            currentVelocity = player.transform.forward * 3 * Mathf.Clamp(moveAmount, 0.5f, 2) * (player.Sprint() ? player.sprintSpeed : player.speed);
       
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
        if (player.TouchMovingInput())
            velocityForce = player.TargetDirection(notRotTransform: true) * player.airControlSpeed;
        else
            velocityForce = Vector3.Lerp(velocityForce, Vector3.zero, Time.deltaTime * player.airControlSpeed);

        velocityForce.y = player.gravityForce;


        if (anim.GetBool("IsRolling") == false)
        {
            moveAmount = player.MoveAmount();

            player.rigid.AddForce(velocityForce, ForceMode.Acceleration);
            player.transform.rotation = player.TargetRotation(player.TargetDirection(true));

            if (player.Jump() && player.currentAirJumpAmount < player.airJumpAmount)
            {
                player.rigid.velocity = Vector3.zero;
                Vector3 jumpDirection = Vector3.up * player.jumpUpForce;
                jumpDirection += player.transform.forward * player.jumpForwardForce * (Vector3.Magnitude(player.TargetDirection(notRotTransform: true) * 2));
                nextState = new Jump(player, anim, jumpDirection, true);
                stage = EVENT.EXIT;
            }

            if (player.Roll())
            {
                player.transform.rotation = player.TargetRotation(player.TargetDirection(notRotTransform: true), instantTurn: true);
                anim.Play("Dash_Air");
            }
        }
        else
        {
            player.transform.position += 2 * anim.deltaPosition;
            player.rigid.velocity = Vector3.zero;
        }


        fallTimer += Time.deltaTime;

        anim.SetFloat("YVelocity", player.rigid.velocity.y);
        anim.SetFloat("Forward", moveAmount);
        anim.SetFloat("FallTime", fallTimer);

  
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
    public Jump(PlayerController _player, Animator _anim, Vector3 _jumpDirection, bool _otherJump)
        : base(_player, _anim) { jumpDirection = _jumpDirection; otherJump = _otherJump; }

    bool otherJump;
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

        player.rigid.AddForce(jumpDirection, ForceMode.VelocityChange);

        timer = 0;

        base.Enter();
    }
    public override void Update()
    {
        if(player.TouchMovingInput())
            velocityForce = player.TargetDirection(notRotTransform: true) * player.airControlSpeed;
        else
            velocityForce = Vector3.zero;

        velocityForce.y = player.gravityForce;

        if (anim.GetBool("IsRolling") == false)
        {
           
            player.rigid.AddForce(velocityForce, ForceMode.Acceleration);
            player.transform.rotation = player.TargetRotation(player.TargetDirection(true));

            if (player.Jump() && timer >= 0.05f && player.currentAirJumpAmount < player.airJumpAmount)
            {
                player.rigid.velocity = Vector3.zero;
                Vector3 jumpDirection = Vector3.up * player.jumpUpForce;
                jumpDirection += player.transform.forward * player.jumpForwardForce * (Vector3.Magnitude(player.TargetDirection(notRotTransform:true) * 2));
                nextState = new Jump(player, anim, jumpDirection, !otherJump);
                stage = EVENT.EXIT;
            }

            if (player.Roll())
            {
                if(player.TouchMovingInput())
                    player.transform.rotation = player.TargetRotation(player.TargetDirection(notRotTransform : true), instantTurn : true);
                else
                    player.transform.rotation = player.TargetRotation(player.TargetDirection(notRotTransform: true, baseOnlyOnCam: true), instantTurn: true);
                anim.Play("Dash_Air");
            }
        }
        else
        {
            player.transform.position += 2 * anim.deltaPosition;
            player.rigid.velocity = Vector3.zero;
        }
            
          
        timer += Time.deltaTime;
        fallTimer += Time.deltaTime;

        anim.SetFloat("YVelocity", player.rigid.velocity.y);
        anim.SetFloat("FallTime", fallTimer);

        Debug.Log(1);
        Debug.Log(player.obstacleData.distance > player.distanceFromWallToHang);
        Debug.Log(player.obstacleData.canBeClimb);
        Debug.Log(timer > 0.2f);
        if (player.obstacleData.distance < player.distanceFromWallToHang && player.obstacleData.canBeClimb && timer > 0.2f)
        {
            Debug.Log(2);
            if (player.obstacleData.relativeHeight < player.hangDistanceFromTop)
            {
                Debug.Log(3);
                nextState = new Hang(player, anim);
                stage = EVENT.EXIT;
            
            }
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

public class Hang: PlayerState
{
    public Hang(PlayerController _player, Animator _anim)
        : base(_player, _anim) {}

    ObstacleForwardData data;
    float canJumpTimer;

    public override void Enter()
    {

        canJumpTimer = 0;
        data = player.obstacleData;

        player.rigid.isKinematic = true;       
        anim.CrossFade("Hang", 0.1f);
        anim.SetBool("Hang", true);

        base.Enter();
    }


    public override void Update()
    {
        player.transform.position = Vector3.Lerp(player.transform.position, data.playerHangPos, Time.deltaTime * 10);
        player.transform.rotation = Quaternion.Slerp(player.transform.rotation, player.TargetRotation(data.playerHangdAngle, instantTurn: true), Time.deltaTime * 10);

        player.leftHandTransform.position = Vector3.Lerp(player.leftHandTransform.position, data.leftHandPos, Time.deltaTime * 10);
        player.rightHandTransform.position = Vector3.Lerp(player.rightHandTransform.position, data.rightHandPos, Time.deltaTime * 10);
        player.IK.solver.leftHandEffector.target.position = player.leftHandTransform.position;
        player.IK.solver.leftHandEffector.positionWeight = 1; 
        player.IK.solver.rightHandEffector.target.position = player.rightHandTransform.position;
        player.IK.solver.rightHandEffector.positionWeight = 1;

        canJumpTimer += Time.deltaTime;

        if (Vector3.Dot(player.TargetDirection(notRotTransform: true), - player.transform.forward) > 0.5f)
        {
            anim.CrossFade("In_Air_Loop", 0.1f);
            nextState = new NotGrounded(player, anim);
            stage = EVENT.EXIT;
        }

        if (player.Jump() && canJumpTimer >= 0.4f)
        {
            Vector3 jumpDirection = Vector3.up * player.jumpUpForce;
            jumpDirection += player.TargetDirection(notRotTransform: true) * player.jumpForwardForce * (Vector3.Magnitude(player.TargetDirection(notRotTransform: true) * 2));
            nextState = new Jump(player, anim, jumpDirection, false);
            stage = EVENT.EXIT;
        }
    }

    public override void Exit()
    {
        player.IK.solver.leftHandEffector.positionWeight = 0;
        player.IK.solver.rightHandEffector.positionWeight = 0;

        base.Exit();
    }
}
