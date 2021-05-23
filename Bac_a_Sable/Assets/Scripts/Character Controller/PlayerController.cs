using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

public class PlayerController : MonoBehaviour
{

  
    [Space(50)]
    public string SprintInput = "Sprint";
    [Space(50)]
    //si on affiche les Ray debug ou non
    public bool showDebug = false;                 
    //le point que le player regarde
    public Transform lookTarget;
    // speed multiply du joueur
    public float speed = 1;
    // sprint speed multiply du joueur
    public float sprintSpeed = 1;
    // l'acceleleration du joueur
    public float acceleration = 4;
    // la vitesse de rotation du joueur
    public float rotationSpeed = 5;
    //La vitesse a que la hauteur du joueur s'adapte au sol
    public float heightFromGroundAdaptation = 20;
    //la force de la gravité si le joueur ne touche pas le sol
    public float gravityForce = 9;

    // la distance que le player check si il est face a un mur ou non
    public float forwardObstacleCheckDistance = 0.5f;
    // A partir de quelle distance le player est considéré dans les airs
    public float groundCheckDistance = 0.5f;
    // A partir de quelle distance le player est considéré dans les airs
    public float airGroundCheckDistance = 0.1f;

    [Header("Slope")]
    public float maxSlopeWalkable = 35f;
    public float minSlopeAffectSpeed = 15;
    public float overSlopeDecceleration = 3;
    public float slideingAcceleration = 0.5f;
    public Vector2 slidingSpeedRange = new Vector2(2,15);
    
   
    [Header("Slope Settings")]
    public LayerMask castingMask;                  
    public float startDistanceFromBottom = 0.2f;   
    public float sphereCastRadius = 0.25f;
    public float sphereCastDistance = 0.75f;       
    public float raycastLength = 0.75f;
    public Vector3 rayOriginOffset1 = new Vector3(-0.2f, 0f, 0.16f);
    public Vector3 rayOriginOffset2 = new Vector3(0.2f, 0f, -0.16f);

    [Header("Wall climb")]
    public float handsSpace = 1;



    private Transform camTransform;
    private Transform leftFoot;
    private Transform rightFoot;
    private Animator anim;

    private Quaternion targetRotation;

    private Vector3 groundPosition;
    private Vector3 targetDir;
    private Vector3 rawTargetDir;
    private Vector3 lastPos;
    private Vector3 slideDirection;
    private Vector3 leftHandPos;
    private Vector3 rightHandPos;

    private float moveAmount;
    private float turnAmount;

    private float YVelocity;

    private float slideSpeed;
    private float groundSlopeAngle = 0f;           

    private float dotNewDirection;
    private float turnDirection;

    private float horizontal;
    private float vertical;
    private float rawHorizontal;
    private float rawVertical;

    private bool obstacleForward;
    private bool isSprinting;
    private bool isGrounded;
    private bool downSlope;
    private bool isSliding;
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
        CheckIfObstaceForward();

        if (isGrounded)
            CheckGroundSlope(transform.position);
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

            if (obstacleForward)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(leftHandPos, 0.2f);


                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(rightHandPos, 0.2f);
            }
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

    //Check l'inclinaison du sol et determine si on la monde ou descent
    public void CheckGroundSlope(Vector3 origin)
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

        if(groundSlopeAngle > 1)
        {
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
            Debug.DrawRay(transform.position, firstHit.normal, Color.green);
            Debug.DrawRay(secondRay, -Vector3.up * 3, Color.green);

            if (Physics.Raycast(secondRay, -Vector3.up, out slopeDirHit, 3, LayerMask.GetMask("Default")))
                slopeDirPosition = slopeDirHit.point;

            if (firstHitPosition.y > upOrDownPosition.y)
                downSlope = true;
            else
                downSlope = false;

            slideDirection = slopeDirPosition - firstHitPosition;

            Debug.DrawRay(firstHitPosition, slideDirection * 2, groundSlopeAngle > maxSlopeWalkable && !isSliding ? Color.yellow : isSliding ? Color.red : Color.green);
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
        if(groundSlopeAngle < minSlopeAffectSpeed)
        {
            if (targetMoveAmount > 0)
            {
                if (isSprinting == false)
                    moveAmount = Mathf.Lerp(moveAmount, targetMoveAmount, Time.unscaledDeltaTime * acceleration);
                else
                    moveAmount = Mathf.Lerp(moveAmount, 2, Time.unscaledDeltaTime * acceleration);            
            }
            else
                moveAmount = Mathf.Lerp(moveAmount, 0, Time.unscaledDeltaTime * (isGrounded ? 5 : 200));

            isSliding = false;
        }
        else
        {
            float slopeMoveAmount = Remap(groundSlopeAngle, minSlopeAffectSpeed, maxSlopeWalkable, 1, 0);

            slopeMoveAmount = Mathf.Clamp(slopeMoveAmount, 0, 1) * targetMoveAmount;

            Debug.Log(slopeMoveAmount);

            if(groundSlopeAngle < maxSlopeWalkable)
            {
             
                if (downSlope == false)
                    moveAmount = Mathf.Lerp(moveAmount, slopeMoveAmount, Time.deltaTime * 5);
                else
                    moveAmount = Mathf.Lerp(moveAmount, targetMoveAmount, Time.unscaledDeltaTime * acceleration);

                   

                isSliding = false;
            }
            else
            {
                if (isSliding == false)
                    slideSpeed = 0;

                moveAmount = Mathf.Lerp(moveAmount, 0, Time.unscaledDeltaTime * overSlopeDecceleration);

                float targetSlideSpeed = Remap(groundSlopeAngle, maxSlopeWalkable, 90, slidingSpeedRange.x, slidingSpeedRange.y);

                slideSpeed = Mathf.Lerp(slideSpeed, targetSlideSpeed, Time.deltaTime * slideingAcceleration);

                if(moveAmount < 0.1f)
                {
                    moveAmount = 0;
                    isSliding = true;
                }               
            }                  
        }

        Quaternion tr;
        if (isSliding == false)
        {
            tr = Quaternion.LookRotation(targetDir);
            targetRotation = Quaternion.Slerp(
                transform.rotation, tr,
                Time.deltaTime * rotationSpeed);
        }
        else
        {
            Vector3 lookDirection = slideDirection;
            lookDirection.y = 0;

            tr = Quaternion.LookRotation(lookDirection);
            targetRotation = Quaternion.Slerp(
                transform.rotation, tr,
                Time.deltaTime * rotationSpeed);
        }
       

        if (rawHorizontal == 0 && rawVertical == 0 && isSliding == false)
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
        if(isSliding == false)
            transform.position += anim.deltaPosition * speed;
        else
            transform.position += slideDirection.normalized * Time.deltaTime * slideSpeed;
           

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

    //Regarde si il y a un obstacle devant le player
    void CheckIfObstaceForward()
    {
        Vector3 forwardCheckOrigin = transform.position;
        forwardCheckOrigin.y += 1;

        Debug.DrawRay(forwardCheckOrigin, transform.forward * forwardObstacleCheckDistance, Color.cyan);
        RaycastHit forwardCheckHit;


        bool climbable = true;

        // regarde devant le joueur si il y a un mur
        if (Physics.Raycast(forwardCheckOrigin, transform.forward, out forwardCheckHit, forwardObstacleCheckDistance))
        {
            obstacleForward = true;

            Vector3 heightCheckOrigin = forwardCheckHit.point + -forwardCheckHit.normal / 2;
            heightCheckOrigin.y += 4;
           
            Debug.DrawRay(heightCheckOrigin, Vector3.down * 5, Color.cyan);
            RaycastHit heightCheckHit;

            // regarde la heuteur du mur
            if (Physics.Raycast(heightCheckOrigin, Vector3.down, out heightCheckHit, 5))
            {
                Vector3 leftNormal = Vector3.Cross(forwardCheckHit.normal, Vector3.up).normalized;
                leftHandPos = forwardCheckHit.point + leftNormal * (handsSpace / 2);
                leftHandPos.y = heightCheckHit.point.y;

                Vector3 rightNormal = -Vector3.Cross(forwardCheckHit.normal, Vector3.up).normalized;
                rightHandPos = forwardCheckHit.point + rightNormal * (handsSpace / 2);
                rightHandPos.y = heightCheckHit.point.y;


                bool leftHandFix = false;
                bool rightHandFix = false;

                //regarde si la main gauche sera dans le vide
                RaycastHit leftCheckHit;
                Vector3 leftCheckOrigin = -forwardCheckHit.normal / 3;
                leftCheckOrigin.y = 0;
                leftCheckOrigin += leftHandPos + (Vector3.up / 2);
                Debug.DrawRay(leftCheckOrigin, Vector3.down * 1, Color.cyan);
                if (Physics.Raycast(leftCheckOrigin, Vector3.down, out leftCheckHit, 1, LayerMask.GetMask("Default")))
                {
                    if (leftCheckHit.point.y == heightCheckHit.point.y)
                    {
                        leftHandFix = true;
                    }
                }

                RaycastHit secondLeftCheckHit;
                leftCheckOrigin += forwardCheckHit.normal / 1.5f;
                Debug.DrawRay(leftCheckOrigin, Vector3.down * (heightCheckHit.point.y - forwardCheckHit.point.y + 0.5f), Color.cyan);
                if (Physics.Raycast(leftCheckOrigin, Vector3.down, out secondLeftCheckHit, heightCheckHit.point.y - forwardCheckHit.point.y + 0.5f, LayerMask.GetMask("Default")))
                {
                    leftHandFix = false;
                }

                //regarde si la main droite sera dans le vide
                RaycastHit rightCheckHit;
                Vector3 rightCheckOrigin = -forwardCheckHit.normal / 3;
                rightCheckOrigin.y = 0;
                rightCheckOrigin += rightHandPos + (Vector3.up / 2);
                Debug.DrawRay(rightCheckOrigin, Vector3.down * 1, Color.cyan);
                if (Physics.Raycast(rightCheckOrigin, Vector3.down, out rightCheckHit, 1, LayerMask.GetMask("Default")))
                {
                    if (rightCheckHit.point.y == heightCheckHit.point.y)
                    {
                        rightHandFix = true;
                    }
                }

                RaycastHit secondRightCheckHit;
                rightCheckOrigin += forwardCheckHit.normal / 1.5f;
                Debug.DrawRay(rightCheckOrigin, Vector3.down * (heightCheckHit.point.y - forwardCheckHit.point.y + 0.5f), Color.cyan);
                if (Physics.Raycast(rightCheckOrigin, Vector3.down, out secondRightCheckHit, heightCheckHit.point.y - forwardCheckHit.point.y + 0.5f, LayerMask.GetMask("Default")))
                {                    
                    rightHandFix = false;
                }


                //si les deux main sont dans le vire, le mur ne peut pas etre monté
                if (leftHandFix == false && rightHandFix == false)
                {
                    climbable = false;
                    return;
                }

                if (leftHandFix == false)
                {
                    leftHandPos += rightNormal * (handsSpace / 2);
                    rightHandPos += rightNormal * (handsSpace / 2);
                }
                if(rightHandFix == false)
                {
                    leftHandPos += leftNormal * (handsSpace / 2);
                    rightHandPos += leftNormal * (handsSpace / 2);
                }



                Debug.DrawLine(forwardCheckHit.point, leftHandPos, Color.cyan);
               // Debug.DrawRay(forwardCheckHit.point + leftNormal / 2, Vector3.up * (heightCheckHit.point.y - forwardCheckHit.point.y), Color.cyan);

                Debug.DrawLine(forwardCheckHit.point, rightHandPos, Color.cyan);
               // Debug.DrawRay(forwardCheckHit.point + rightNormal / 2, Vector3.up *(heightCheckHit.point.y - forwardCheckHit.point.y), Color.cyan);
            }
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