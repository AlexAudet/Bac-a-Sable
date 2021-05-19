using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{
    public string SprintInput = "Sprint";
    [Space(50)]
    // l'acceleleration du joueur
    public float speed = 1;
    // l'acceleleration du joueur
    public float acceleration = 4;
    // la vitesse de rotation du joueur
    public float rotationSpeed = 5;
    //La vitesse a que la hauteur du joueur s'adapte au sol
    public float heightFromGroundAdaptation = 20;
    //la force de la gravité si le joueur ne touche pas le sol
    public float gravityForce = 9;
    // A partir de quelle distance le player est considéré dans les airs
    public float groundCheckDistance = 0.5f;
    // A partir de quelle distance le player est considéré dans les airs
    public float airGroundCheckDistance = 0.1f;


    public Transform lookTarget;
    private Transform camTransform;

    private Transform leftFoot;
    private Transform rightFoot;
    private Animator anim;

    private Quaternion targetRotation;

    [Space(100)]
    private Vector3 groundPosition;
    private Vector3 targetDir;
    private Vector3 rawTargetDir;
    private Vector3 lastPos;

    [Space(10)]
    private float moveAmount;
    private float turnAmount;
    private float YVelocity;

    [Space(10)]
    private float dotNewDirection;
    private float turnDirection;

    [Space(10)]
    private float horizontal;
    private float vertical;
    private float rawHorizontal;
    private float rawVertical;

    private bool isSprinting;
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
        ProcessAnimationVariable();
        WhichFootForward();
        LookTarget();
        Sprint();
    }
    private void LateUpdate()
    {
        AdjustPlayerHeightFromGround();
        SetAnimationVariable();
        SetPlayerTransform();
      
    }
    private void FixedUpdate()
    {
        CheckIfGrounded();
    }

    //Check si le player touche au sol
    void CheckIfGrounded()
    {
        Vector3 dir = -Vector3.up;

        Vector3 origin = transform.position;
        origin.y += 2;

        Vector3 leftOrigin = leftFoot.position;
        leftOrigin.y += 0.5f;

        Vector3 rightOrigin = rightFoot.position;
        rightOrigin.y += 0.5f;

        float dis;
        float leftDis;
        float rightDis;

        if (isGrounded)
        {
            dis = groundCheckDistance + 2;
            leftDis = groundCheckDistance + 1;
            rightDis = groundCheckDistance +  1;
        }
        else
        {
            dis = airGroundCheckDistance + 2;
            leftDis = airGroundCheckDistance + 0.5f;
            rightDis = airGroundCheckDistance + 0.5f;
        }
  

        Debug.DrawRay(origin, dir * dis);
        Debug.DrawRay(leftOrigin, dir * leftDis);
        Debug.DrawRay(rightOrigin, dir * rightDis);

        RaycastHit hit;
        RaycastHit leftHit;
        RaycastHit rightHit;

        if (Physics.Raycast(origin, dir, out hit, dis))
        {         
            if(hit.transform.gameObject.layer != LayerMask.GetMask("Player"))
            {
                groundPosition = hit.point;

                isGrounded = true;
            }
        }
        else
        {
            isGrounded = false;
        }


        if (Physics.Raycast(leftOrigin, dir, out leftHit, leftDis))
        {
            if (hit.transform.gameObject.layer != LayerMask.GetMask("Player"))
            {
                isGrounded = true;
            }
        }
        else
        {
            isGrounded = false;
        }


        if (Physics.Raycast(rightOrigin, dir, out rightHit, rightDis))
        {
            if (hit.transform.gameObject.layer != LayerMask.GetMask("Player"))
            {
                isGrounded = true;
            }
        }
        else
        {
            isGrounded = false;
        }
    }

    //Ajuste la hauteur du player par rapport au sol
    void AdjustPlayerHeightFromGround()
    {
        Vector3 heightPos;

        if (isGrounded == true)
        {
            heightPos = transform.position;
            heightPos.y = groundPosition.y;
            transform.position = Vector3.Lerp(transform.position, heightPos, Time.deltaTime * heightFromGroundAdaptation);
        }
        else
        {
            heightPos = transform.position;
            heightPos.y -= 1;
            transform.position = Vector3.Lerp(transform.position, heightPos, Time.deltaTime * gravityForce);
        }

    }

    //Get les variable des géré par les input;
    void GetMovementVariable()
    {   
        rawHorizontal = Input.GetAxisRaw("Horizontal");
        rawVertical = Input.GetAxisRaw("Vertical");
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        rawTargetDir = camTransform.forward * rawVertical;
        rawTargetDir += camTransform.right * rawHorizontal;
        rawTargetDir.y = 0;

        targetDir = camTransform.forward * vertical;
        targetDir += camTransform.right * horizontal;
        targetDir.y = 0;     
    }

    //calcul les variable comme le moveAmount ou le turnAmount pour pouvoir les envoyer a l'animator
    void ProcessAnimationVariable()
    {
        dotNewDirection = Vector3.Dot(rawTargetDir, transform.forward);
        turnDirection = Vector3.Dot(transform.right, rawTargetDir);


        YVelocity = (transform.position - lastPos).magnitude;
        lastPos = transform.position;
        if (YVelocity > 1)
            YVelocity = 1;
        if (YVelocity < -1)
            YVelocity = -1;


        float targetMoveAmount = Vector3.Magnitude(targetDir);
        if (targetMoveAmount > 0)
        {
            if (isSprinting == false)
                moveAmount = Mathf.Lerp(moveAmount, targetMoveAmount, Time.unscaledDeltaTime * acceleration);
            else
                moveAmount = Mathf.Lerp(moveAmount, 2, Time.unscaledDeltaTime * acceleration);
        }
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

        if ((tr.eulerAngles - transform.eulerAngles).sqrMagnitude > 100)
        {
            float targetTurnAmount = Vector3.Dot(transform.right, targetDir);

            turnAmount = Mathf.Lerp(turnAmount, targetTurnAmount, Time.unscaledDeltaTime * 10);
        }
        else
            turnAmount = Mathf.Lerp(turnAmount, 0, Time.unscaledDeltaTime * 10);

        if (turnAmount > 1)
            turnAmount = 1;

        if (turnAmount < -1)
            turnAmount = -1;
    }

    //Set les transform du jouer comme la rotation et la position et garde le model au centre du parent
    void SetPlayerTransform()
    {
        transform.position += anim.deltaPosition * speed;
        transform.rotation = targetRotation;

        anim.transform.localPosition = Vector3.zero;
        anim.transform.localEulerAngles = Vector3.zero;
    }

    //Envoie les variable calculé a l'Animator pour joué avec les blendTrees
    void SetAnimationVariable()
    {
        anim.SetFloat("Forward", moveAmount);
        anim.SetFloat("Turn", turnAmount);
        anim.SetFloat("DotDirection", dotNewDirection);
        anim.SetFloat("YVelocity", YVelocity);
        anim.SetBool("RightFootForward", rightFootForward);
        anim.SetBool("IsGrounded", isGrounded);
    }

    //Check quelle pied est devant pour pouvoir lancé les animation en conséquence du pied qui est en avant
    void WhichFootForward()
    {

        Vector3 lf_relativPos = transform.InverseTransformPoint(leftFoot.position);
        Vector3 rf_relativPos = transform.InverseTransformPoint(rightFoot.position);

        rightFootForward = false;
        if (rf_relativPos.z > lf_relativPos.z)
            rightFootForward = true;
    }

    //Set la position du LookTarget pour que la tete du player tourne dans la direction avec le component LookAtIk
    void LookTarget()
    {
        Vector3 lookTargetPosition = Vector3.Lerp(lookTarget.position, (anim.GetBoneTransform(HumanBodyBones.Head).position + camTransform.transform.forward * 10), Time.deltaTime * 8);
        lookTargetPosition.y = anim.GetBoneTransform(HumanBodyBones.Head).position.y;
        lookTarget.position = lookTargetPosition;
    }

    //Check si le player cours ou non
    void Sprint()
    {
        if (Input.GetButton(SprintInput))
        {
            isSprinting = true;
        }
        else
            isSprinting = false;
    }

 
}


[System.Serializable]
public class PlayerAttribute
{
    
}