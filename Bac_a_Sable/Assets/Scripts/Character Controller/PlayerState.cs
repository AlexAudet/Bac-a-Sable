using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState : MonoBehaviour
{
    public enum STATE
    {
        IDLE, MOVEMENT
    };

    public enum EVENT
    {
        ENTER, UPDATE, EXIT
    };

    public STATE name;
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

public class Movement : PlayerState
{
    public Movement(PlayerController _player, Animator _anim)
        : base(_player, _anim)
    {
        name = STATE.IDLE;
    }


    public override void Enter()
    {
        base.Enter();
    }


    public override void Update()
    {
        anim.SetFloat("Forward", player.moveAmount);

        anim.SetFloat("Turn", player.turnAmount);

       // player.transform.rotation = player.targetRotation;
    }

    public override void Exit()
    {
        base.Exit();
    }
}
