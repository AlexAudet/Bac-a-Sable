using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{

  
    [Space(50)]
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

    public float forwardObstacleCheckDistance = 0.5f;
    // A partir de quelle distance le player est considéré dans les airs
    public float groundCheckDistance = 0.5f;
    // A partir de quelle distance le player est considéré dans les airs
    public float airGroundCheckDistance = 0.1f;

    public float maxSlopeWalkable = 35f;
    public float minSlopeAffectSpeed = 15;

    [Header("Results")]
    public float slopDirection;
    public float groundSlopeAngle = 0f;            // Angle of the slope in degrees

    [Header("Settings")]
    public bool showDebug = false;                  // Show debug gizmos and lines
    public LayerMask castingMask;                  // Layer mask for casts. You'll want to ignore the player.
    public float startDistanceFromBottom = 0.2f;   // Should probably be higher than skin width
    public float sphereCastRadius = 0.25f;
    public float sphereCastDistance = 0.75f;       // How far spherecast moves down from origin point

    public float raycastLength = 0.75f;
    public Vector3 rayOriginOffset1 = new Vector3(-0.2f, 0f, 0.16f);
    public Vector3 rayOriginOffset2 = new Vector3(0.2f, 0f, -0.16f);


    public Transform lookTarget;
    private Transform camTransform;

    private Transform leftFoot;
    private Transform rightFoot;
    private Animator anim;

    private Quaternion targetRotation;

    [Space(100)]
    public Vector3 groundPosition;
    private Vector3 targetDir;
    private Vector3 rawTargetDir;
    private Vector3 lastPos;
    public  Vector3 slideDirection;

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

    private bool obstacleForward;
    private bool isSprinting;
    private bool isGrounded;
    public bool downSlope;
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
        //CheckIfObstaceForward();

        if (isGrounded)
        {
            CheckGround(transform.position);
        }
    }


    void OnDrawGizmos()
    {
        if (showDebug)
        {
            // Visualize SphereCast with two spheres and a line
            Vector3 startPoint = new Vector3(transform.position.x, transform.position.y - (2 / 2) + startDistanceFromBottom, transform.position.z);
            Vector3 endPoint = new Vector3(transform.position.x, transform.position.y - (2 / 2) + startDistanceFromBottom - sphereCastDistance, transform.position.z);

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(startPoint, sphereCastRadius);

            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(endPoint, sphereCastRadius);

            Gizmos.DrawLine(startPoint, endPoint);
        }
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
  

        Debug.DrawRay(origin, dir * dis, Color.blue);
        Debug.DrawRay(leftOrigin, dir * leftDis, Color.blue);
        Debug.DrawRay(rightOrigin, dir * rightDis, Color.blue);

        RaycastHit hit;
        RaycastHit leftHit;
        RaycastHit rightHit;

        if (Physics.Raycast(origin, dir, out hit, dis, LayerMask.GetMask("Default")))
        {         
            if(hit.transform.gameObject != gameObject)
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
            if (hit.transform.gameObject != gameObject)
            {
                isGrounded = true;
            }
        }
  


        if (Physics.Raycast(rightOrigin, dir, out rightHit, rightDis))
        {
            if (hit.transform.gameObject != gameObject)
            {
                isGrounded = true;
            }
        }

    }

    public void CheckGround(Vector3 origin)
    {
        RaycastHit hit;
        if (Physics.SphereCast(origin, sphereCastRadius, Vector3.down, out hit, sphereCastDistance, castingMask))
        {
            groundSlopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            Vector3 temp = Vector3.Cross(hit.normal, Vector3.down);
           
        }
        RaycastHit slopeHit1;
        RaycastHit slopeHit2;

        if (Physics.Raycast(origin + rayOriginOffset1, Vector3.down, out slopeHit1, raycastLength))
        {      
            if (showDebug) { Debug.DrawLine(origin + rayOriginOffset1, slopeHit1.point, Color.red); }
   
            float angleOne = Vector3.Angle(slopeHit1.normal, Vector3.up);

            if (Physics.Raycast(origin + rayOriginOffset2, Vector3.down, out slopeHit2, raycastLength))
            {
         
                if (showDebug) { Debug.DrawLine(origin + rayOriginOffset2, slopeHit2.point, Color.red); }
               
                float angleTwo = Vector3.Angle(slopeHit2.normal, Vector3.up);
           
                float[] tempArray = new float[] { groundSlopeAngle, angleOne, angleTwo };
                System.Array.Sort(tempArray);
                groundSlopeAngle = tempArray[1];
            }
            else
            {        
                float average = (groundSlopeAngle + angleOne) / 2;
                groundSlopeAngle = average;
            }
        }



        Vector3 firstRay = transform.position + transform.forward * 0.2f;
        firstRay.y += 1;      
        Debug.DrawRay(firstRay, -Vector3.up * 3, Color.green);

        Vector3 upOrDownRay = transform.position + transform.forward * 0.6f;
        upOrDownRay.y += 1;
        Debug.DrawRay(upOrDownRay, -Vector3.up * 3, Color.green);


        RaycastHit firstHit;
        RaycastHit slopeDirHit;
        RaycastHit upOrDownHit;

        Vector3 firstHitPosition = Vector3.zero;
        Vector3 slopeDirPosition = Vector3.zero;
        Vector3 upOrDownPosition = Vector3.zero;

        if (Physics.Raycast(firstRay, -Vector3.up, out firstHit, 3, LayerMask.GetMask("Default")))
        {
            firstHitPosition = firstHit.point;
        }
        if (Physics.Raycast(upOrDownRay, -Vector3.up, out upOrDownHit, 3, LayerMask.GetMask("Default")))
        {
            upOrDownPosition = upOrDownHit.point;
        }


        Vector3 secondRay = firstHit.normal + transform.position;
        Debug.DrawRay(secondRay, -Vector3.up * 3, Color.green);

        if (Physics.Raycast(secondRay, -Vector3.up, out slopeDirHit, 3, LayerMask.GetMask("Default")))
            slopeDirPosition = slopeDirHit.point;

        if(firstHitPosition.y > upOrDownPosition.y)
            downSlope = true;
        else
            downSlope = false;

        slideDirection = slopeDirPosition - firstHitPosition;

        Debug.DrawRay(firstHitPosition, slideDirection * 2, Color.green);
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
        if (targetMoveAmount > 0 && (maxSlopeWalkable > groundSlopeAngle || downSlope == true))
        {
            if (isSprinting == false)
                moveAmount = Mathf.Lerp(moveAmount, targetMoveAmount, Time.unscaledDeltaTime * acceleration);
            else
                moveAmount = Mathf.Lerp(moveAmount, 2, Time.unscaledDeltaTime * acceleration);
        }
        else
            moveAmount = Mathf.Lerp(moveAmount, 0, Time.unscaledDeltaTime * (isGrounded || maxSlopeWalkable > groundSlopeAngle ? 5 : 200));

        if(groundSlopeAngle > 0)
        {
            float slopeMoveAmount = Remap(groundSlopeAngle, minSlopeAffectSpeed, maxSlopeWalkable, 0, 1);

            slopeMoveAmount = Mathf.Clamp(slopeMoveAmount, 0, 1) * targetMoveAmount;

            Debug.Log(slopeMoveAmount);

            moveAmount = Mathf.Lerp(moveAmount, slopeMoveAmount, Time.deltaTime * 10);
        }


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

    void CheckIfObstaceForward()
    {
        Vector3 dir = transform.forward;
        Vector3 origin = transform.position;
        origin.y += 1;
        float dis = forwardObstacleCheckDistance;

        Debug.DrawRay(origin, dir * dis, Color.red);
        RaycastHit hit;


        if (Physics.Raycast(origin, dir, out hit, dis))
        {
            if (hit.transform.gameObject.layer != LayerMask.GetMask("Player"))
            {
                obstacleForward = true;
            }
        }
        else
        {
            obstacleForward = false;
        }
      
       

        
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

    float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}


[System.Serializable]
public class PlayerAttribute
{
    
}