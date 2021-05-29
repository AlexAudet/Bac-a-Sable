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
    public virtual void FixedUpdate() { stage = EVENT.UPDATE; }
    public virtual void Exit() { stage = EVENT.EXIT; }

    public PlayerState Process()
    {
        if (stage == EVENT.ENTER) Enter();
        if (stage == EVENT.UPDATE) Update(); FixedUpdate();
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

    public override void Enter()
    {
        base.Enter();
    }

    Vector3 targetDir;
    float vertical;
    float horizontal;
    float targetMoveAmount;
    float moveAmount;
    float turnAmount;
    bool sprinting;

    OnSlopeData slopeData = new OnSlopeData();
    public override void Update()
    {

        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        sprinting = Input.GetButton(player.SprintInput);

        targetDir = player.camTransform.forward * vertical;
        targetDir += player.camTransform.right * horizontal;
        targetDir.y = 0;

        if (!sprinting)
            targetMoveAmount = Vector3.Magnitude(targetDir);
        else
            targetMoveAmount = 2;

        if (slopeData.slopeAngle < player.minSlopeAffectSpeed)
        {           
            moveAmount = Mathf.Lerp(moveAmount, targetMoveAmount, Time.unscaledDeltaTime * player.acceleration);          
        }
        else
        {
            float slopeMoveAmount = Remap(slopeData.slopeAngle, player.minSlopeAffectSpeed, player.maxSlopeWalkable, 1, 0);

            slopeMoveAmount = Mathf.Clamp(slopeMoveAmount, 0, 1) * targetMoveAmount;


            if (slopeData.slopeAngle < player.maxSlopeWalkable)
            {
                if (downSlope == false)
                    moveAmount = Mathf.Lerp(moveAmount, slopeMoveAmount, Time.deltaTime * player.overSlopeDecceleration);
                else
                    moveAmount = Mathf.Lerp(moveAmount, targetMoveAmount * Remap(slopeData.slopeAngle, player.minSlopeAffectSpeed, player.maxSlopeWalkable, 1, 2), Time.unscaledDeltaTime * player.rotationSpeed);
            }
            else
            {
                moveAmount = Mathf.Lerp(moveAmount, 0, Time.unscaledDeltaTime * player.overSlopeDecceleration);
            }
        }

        if ((player.TargetRotation(targetDir).eulerAngles - player.transform.eulerAngles).sqrMagnitude > 100)
        {
            float targetTurnAmount = Vector3.Dot(player.transform.right, targetDir);

            turnAmount = Mathf.Lerp(turnAmount, targetTurnAmount, Time.unscaledDeltaTime * 10);
        }
        else
            turnAmount = Mathf.Lerp(turnAmount, 0, Time.unscaledDeltaTime * 10);

        if (turnAmount > 1)
            turnAmount = 1;

        if (turnAmount < -1)
            turnAmount = -1;


        player.transform.position += anim.deltaPosition * player.speed;
        player.transform.rotation = player.TargetRotation(player.targetDir);

        anim.SetBool("RightFootForward", player.RightFootForward());
        anim.SetFloat("Forward", moveAmount);
        anim.SetFloat("Turn", turnAmount);

    }
    public override void FixedUpdate()
    {
        slopeData = player.CheckGroundSlope(player.transform.position);
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
        Quaternion rot = player.TargetRotation(player.camTransform.forward);
  
        Quaternion tr = Quaternion.LookRotation(loockRot);
        tr.z = 0;
        player.targetRotationLockOn = Quaternion.Slerp(
            player.transform.rotation, tr,
            Time.deltaTime * player.rotationSpeed);
 

        turnAmount = Mathf.Lerp(turnAmount, horizontal, Time.unscaledDeltaTime * player.rotationSpeed);

        player.transform.rotation = player.targetRotationLockOn;

        player.lookTarget.position = Vector3.Lerp(player.lookTarget.position, (player.camTransform.position + player.camTransform.forward * 10), Time.deltaTime * 8);
    }

    public override void Exit()
    {
        base.Exit();
    }
}

public class Sliding : PlayerState
{
    public Sliding(PlayerController _player, Animator _anim)
        : base(_player, _anim){}


    public override void Enter()
    {
        float targetSlideSpeed = Remap(groundSlopeAngle, maxSlopeWalkable, 90, slidingSpeedRange.x, slidingSpeedRange.y);

        base.Enter();
    }


    public override void Update()
    {

  

        slideSpeed = Mathf.Lerp(slideSpeed, targetSlideSpeed, Time.deltaTime * slideingAcceleration);

        player.transform.position += slideDirection.normalized * Time.deltaTime * slideSpeed;

        Vector3 lookDirection = slideDirection;
        lookDirection.y = 0;

        Quaternion tr = Quaternion.LookRotation(lookDirection);
        targetRotation = Quaternion.Slerp(
            player.transform.rotation, tr,
            Time.deltaTime * player.rotationSpeed);
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


    public override void Enter()
    {
        base.Enter();
    }


    public override void Update()
    {
        YVelocity = (transform.position - lastPos).magnitude;
        lastPos = transform.position;
        if (YVelocity > 1)
            YVelocity = 1;
        if (YVelocity < -1)
            YVelocity = -1;

        anim.SetFloat("YVelocity", YVelocity);

    }

    public override void Exit()
    {
        base.Exit();
    }
}