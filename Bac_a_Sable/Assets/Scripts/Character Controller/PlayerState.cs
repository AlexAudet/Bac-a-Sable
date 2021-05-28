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
}

public class NormalMovement : PlayerState
{
    public NormalMovement(PlayerController _player, Animator _anim)
        : base(_player, _anim){ }

    public override void Enter()
    {
        base.Enter();
    }


    public override void Update()
    {
        player.transform.position += anim.deltaPosition * player.speed;
        player.transform.rotation = player.targetRotation;

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


    public override void Update()
    {
        Vector3 loockRot = player.camTransform.forward;
        loockRot.y = player.transform.position.y;
        Quaternion tr = Quaternion.LookRotation(loockRot);
        tr.z = 0;
        player.targetRotationLockOn = Quaternion.Slerp(
            player.transform.rotation, tr,
            Time.deltaTime * player.rotationSpeed);


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
        base.Enter();
    }


    public override void Update()
    {
        player.transform.position += player.slideDirection.normalized * Time.deltaTime * player.slideSpeed;
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
        player.AdjustPlayerHeightFromGround();
    }

    public override void Exit()
    {
        base.Exit();
    }
}