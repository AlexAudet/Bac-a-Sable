using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{


    // l'acceleleration du joueur
    public float speed = 1;
    // l'acceleleration du joueur
    public float acceleration = 0.75f;
    // la vitesse de rotation du joueur
    public float rotationSpeed = 0.75f;
    // A partir de quelle distance le player est considéré dans les airs
    public float airDis = 0.75f;



    public Transform lookTarget;
    private Transform camTransform;

    private Transform leftFoot;
    private Transform rightFoot;
    private Animator anim;

    private Quaternion targetRotation;

    [Space(50)]
    private Vector3 groundPosition;
    public Vector3 targetDir;
    public Vector3 rawTargetDir;

    [Space(10)]
    public float moveAmount;
    public float turnAmount;

    [Space(10)]
    public float dotNewDirection;
    public float turnDirection;

    [Space(10)]
    public float horizontal;
    public float vertical;
    public float rawHorizontal;
    public float rawVertical;

    private bool isGrounded;
    private bool rightFootForward;

    private void Start()
    {
        camTransform = Camera.main.transform;
        anim = GetComponentInChildren<Animator>();

        leftFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);
    }

    private void Update()
    {
        GetMovementVariable();
    }

    void CheckIfGrounded()
    {
        Vector3 origin = transform.position;
        origin.y += 1f;

        Vector3 dir = -Vector3.up;
        float dis = airDis;


        RaycastHit hit;
        Debug.DrawRay(origin, dir * dis);

        if (Physics.Raycast(origin, dir, out hit, dis, LayerMask.GetMask("Default")))
        {
            groundPosition = hit.point;

            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }

    void GetMovementVariable()
    {   
        rawHorizontal = Input.GetAxisRaw("Horizontal");
        rawVertical = Input.GetAxisRaw("Vertical");
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");


        rawTargetDir = camTransform.forward * rawVertical;
        rawTargetDir += camTransform.right * rawHorizontal;
        rawTargetDir.y = 0;

        dotNewDirection = Vector3.Dot(rawTargetDir, transform.forward);
        turnDirection = Vector3.Dot(transform.right, rawTargetDir);



        targetDir = camTransform.forward * vertical;
        targetDir += camTransform.right * horizontal;
        targetDir.y = 0;

    


        float targetMoveAmount = Vector3.Magnitude(targetDir);
        if (targetMoveAmount > 0)
            moveAmount = Mathf.Lerp(moveAmount, targetMoveAmount, Time.unscaledDeltaTime * acceleration);
        else
            moveAmount = Mathf.Lerp(moveAmount, 0, Time.unscaledDeltaTime * 5);


        Quaternion tr = Quaternion.LookRotation(targetDir);
        targetRotation = Quaternion.Slerp(
            transform.rotation, tr,
            Time.deltaTime * rotationSpeed);

        if (rawHorizontal == 0 && rawVertical == 0)
        {
            targetRotation = transform.rotation;
        }

        transform.rotation = targetRotation;


        if ((tr.eulerAngles - transform.eulerAngles).sqrMagnitude > 100)
        {
            float targetTurnAmount = Vector3.Dot(transform.right, targetDir);

            turnAmount = Mathf.Lerp(turnAmount, targetTurnAmount, Time.unscaledDeltaTime * 10);
        }
        else
            turnAmount = 0;



        Vector3 lf_relativPos = transform.InverseTransformPoint(leftFoot.position);
        Vector3 rf_relativPos = transform.InverseTransformPoint(rightFoot.position);

        rightFootForward = false;
        if (rf_relativPos.z > lf_relativPos.z)
            rightFootForward = true;


        anim.SetFloat("Forward", moveAmount);
        anim.SetFloat("Turn", turnAmount);
        anim.SetFloat("DotDirection", dotNewDirection);
        anim.SetBool("RightFootForward", rightFootForward);



        Vector3 lookTargetPosition = Vector3.Lerp(lookTarget.position, (anim.GetBoneTransform(HumanBodyBones.Head).position + camTransform.transform.forward * 10), Time.deltaTime * 8);
        lookTarget.position = lookTargetPosition;
    }

    private void LateUpdate()
    {
        transform.position = anim.deltaPosition * speed;
        anim.transform.localPosition = Vector3.zero;
        anim.transform.localEulerAngles = Vector3.zero;
    }
}


[System.Serializable]
public class PlayerAttribute
{
    
}