using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{


    // l'acceleleration du joueur
    public float speed = 5;
    // l'acceleleration du joueur
    public float acceleration = 0.75f;
    // la vitesse de rotation du joueur
    public float rotationSpeed = 0.75f;
    // A partir de quelle distance le player est considéré dans les airs
    public float airDis = 0.75f;



    private Transform lookTarget;
    private Transform camTransform;
    private Animator anim;

    private Quaternion targetRotation;

    [Space(10)]
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
    public float rawHorizontal;
    public float vertical;
    public float rawVertical;

    private bool isGrounded;

    private void Start()
    {
        camTransform = Camera.main.transform;

        anim = GetComponentInChildren<Animator>();
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
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        rawHorizontal = Input.GetAxisRaw("Horizontal");
        rawVertical = Input.GetAxisRaw("Vertical");


        targetDir = camTransform.forward * rawVertical;
        targetDir += camTransform.right * rawHorizontal;
        targetDir.y = 0;

        rawTargetDir = camTransform.forward * rawVertical;
        rawTargetDir += camTransform.right * rawHorizontal;
        rawTargetDir.y = 0;



        dotNewDirection = Vector3.Dot(rawTargetDir, transform.forward);
        turnDirection = Vector3.Dot(transform.right, rawTargetDir);

        float targetMoveAmount = Vector3.Magnitude(targetDir);
        if (targetMoveAmount > 0)
            moveAmount = Mathf.Lerp(moveAmount, targetMoveAmount, Time.unscaledDeltaTime * acceleration);
        else
            moveAmount = 0;

        Quaternion tr = Quaternion.LookRotation(targetDir);
        targetRotation = Quaternion.Slerp(
            transform.rotation, tr,
            Time.deltaTime * rotationSpeed);

        if (rawHorizontal == 0 && rawVertical == 0)
        {
            targetRotation = transform.rotation;
        }

        anim.SetFloat("Vertical", moveAmount);

        anim.SetFloat("Horizontal", turnAmount);

        anim.transform.localPosition = Vector3.zero;
        transform.position = anim.rootPosition;
        transform.rotation = targetRotation;


    
        if ((tr.eulerAngles - transform.eulerAngles).sqrMagnitude > 0.1f)
        {
            float targetTurnAmount = Vector3.Dot(transform.right, targetDir);

            turnAmount = Mathf.Lerp(turnAmount, targetTurnAmount, Time.unscaledDeltaTime * 3);
        }
        else
            turnAmount = Mathf.Lerp(turnAmount, 0, Time.unscaledDeltaTime * 3);



      //  Vector3 lookTargetPosition = Vector3.Lerp(lookTarget.position, (anim.GetBoneTransform(HumanBodyBones.Head).position + cam.transform.forward * 10), Time.deltaTime * 8);
      //  lookTargetPosition.y = anim.GetBoneTransform(HumanBodyBones.Head).position.y;
      //  lookTarget.position = lookTargetPosition;

     //   if (Vector3.Dot(rawTargetDir, transform.forward) < 0f)
     //   {
     //       if (Vector3.Dot(transform.right, rawTargetDir) < 0)
     //       {
     //           left_180 = true;
     //           right_180 = false;
     //       }
     //       else
     //       {
     //           left_180 = false;
     //           right_180 = true;
     //       }
     //   }
     //   else
     //   {
     //       left_180 = false;
     //       right_180 = false;
     //   }
    }
}


[System.Serializable]
public class PlayerAttribute
{
    
}