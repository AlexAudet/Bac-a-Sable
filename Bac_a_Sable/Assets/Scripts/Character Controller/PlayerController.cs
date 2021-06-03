using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;

public class PlayerController : MonoBehaviour
{
    [Space(50)]
    public string SprintInput = "Sprint";
    public string LockOnInput = "LockOn";
    public string JumpImput = "Jump";
    public string RollInput = "";
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
    [Space(20)]
    public float jumpForce = 3;
    public int airJumpAmount = 2;
    public float airControlSpeed = 5;
    public float rollDistanceMultiply = 2;
    [Space(20)]
    // la distance que le player check si il est face a un mur ou non
    public float forwardObstacleCheckDistance = 3f;
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
    public float startDistanceFromBottom = 0.2f;   
    public float sphereCastRadius = 0.25f;
    public float sphereCastDistance = 0.75f;       
    public float raycastLength = 0.75f;
    public Vector3 rayOriginOffset1 = new Vector3(-0.2f, 0f, 0.16f);
    public Vector3 rayOriginOffset2 = new Vector3(0.2f, 0f, -0.16f);

    [Header("Wall climb")]
    public float handsSpace = 1;


    #region privateVariable

    [HideInInspector] public PlayerState state;
    [HideInInspector] public OnSlopeData slopeDataResult = new OnSlopeData();
    [HideInInspector] public Transform camTransform;
    [HideInInspector] public Transform noRotCamTransform;
    [HideInInspector] public Rigidbody rigid;
    private Transform leftFoot;
    private Transform rightFoot;
    private Animator anim;

    [HideInInspector] public int currentAirJumpAmount;
    #endregion

    private void Start()
    {
        camTransform = Camera.main.transform;
        noRotCamTransform = CameraController.Instance.noRotCamTransform;
        anim = GetComponentInChildren<Animator>();
        rigid = GetComponent<Rigidbody>();

        leftFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);

        state = new NormalMovement(this, anim);
    }

    private void Update()
    {
        state = state.Process();
        StayInConrtoller();
        ObstacleForward();
    }

    private void FixedUpdate()
    {

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

    float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    //Regarde si on est en train de viser
    public bool OnAiming()
    {
        return Input.GetButton(LockOnInput);
    }

    //Regarde si on est en train de sauter
    public bool Jump()
    {
        return Input.GetButtonDown(JumpImput);
    }

    //Regarde si on est en train de Rouller
    public bool Roll()
    {
        return Input.GetButtonDown(RollInput);
    }

    //Regarde si on est en train de sprinter
    public bool Sprint()
    {
        return Input.GetButton(SprintInput);
    }


    // Regarde si il y a un obstacle devant le player
    private bool leftHandFix = false;
    private bool rightHandFix = false;
    private Vector3 leftHandPos;
    private Vector3 rightHandPos;
    private float obstacleDotProduct;
    private bool obstacleForward;
    public bool ObstacleForward()
    {
        obstacleForward = false;

        Vector3 forwardCheckOrigin = transform.position;
        forwardCheckOrigin.y += 1;

        Debug.DrawRay(forwardCheckOrigin, transform.forward * forwardObstacleCheckDistance, Color.cyan);

  
        Vector3 forwardCheckDirection = transform.forward;

        if(leftHandFix && rightHandFix)
        {
            if (Vector3.Distance(transform.position, leftHandPos) > Vector3.Distance(transform.position, rightHandPos))
                forwardCheckDirection = Vector3.Lerp(-transform.right, transform.forward, obstacleDotProduct);
            else
                forwardCheckDirection = Vector3.Lerp(transform.right, transform.forward, obstacleDotProduct);
            forwardCheckDirection = Vector3.Lerp(forwardCheckDirection, transform.forward, 0.5f);
        }
     

        Debug.DrawRay(forwardCheckOrigin, forwardCheckDirection * 2, Color.cyan);


        RaycastHit forwardCheckHit;
        // regarde devant le joueur si il y a un mur
        if (Physics.Raycast(forwardCheckOrigin, forwardCheckDirection, out forwardCheckHit, forwardObstacleCheckDistance))
        {
            obstacleForward = true;
            obstacleDotProduct = Vector3.Dot(-forwardCheckHit.normal, transform.forward);

            //Si l'angle entre le personnage et le mur est trop grand annule
            if (obstacleDotProduct > 0.3f)
            {
                Vector3 heightCheckOrigin = forwardCheckHit.point + -forwardCheckHit.normal / 2;
                heightCheckOrigin.y += 4;

                Debug.DrawRay(heightCheckOrigin, Vector3.down * 5, Color.cyan);
                RaycastHit heightCheckHit;

                // regarde la hauteur du mur
                if (Physics.Raycast(heightCheckOrigin, Vector3.down, out heightCheckHit, 5))
                {

                    Vector3 leftNormal = Vector3.Cross(forwardCheckHit.normal, Vector3.up).normalized;
                    leftHandPos = forwardCheckHit.point + leftNormal * (handsSpace / 2);
                    leftHandPos.y = heightCheckHit.point.y;

                    Vector3 rightNormal = -Vector3.Cross(forwardCheckHit.normal, Vector3.up).normalized;
                    rightHandPos = forwardCheckHit.point + rightNormal * (handsSpace / 2);
                    rightHandPos.y = heightCheckHit.point.y;


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
                    else
                        leftHandFix = false;

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
                    else
                        rightHandFix = false;

                    RaycastHit secondRightCheckHit;
                    rightCheckOrigin += forwardCheckHit.normal / 1.5f;
                    Debug.DrawRay(rightCheckOrigin, Vector3.down * (heightCheckHit.point.y - forwardCheckHit.point.y + 0.5f), Color.cyan);
                    if (Physics.Raycast(rightCheckOrigin, Vector3.down, out secondRightCheckHit, heightCheckHit.point.y - forwardCheckHit.point.y + 0.5f, LayerMask.GetMask("Default")))
                    {
                        rightHandFix = false;
                    }

                    //si les deux main sont dans le vire, le mur ne peut pas etre monté
                    if (leftHandFix == true || rightHandFix == true)
                    {
                        if (leftHandFix == false)
                        {
                            leftHandPos += rightNormal * (handsSpace / 2);
                            rightHandPos += rightNormal * (handsSpace / 2);
                        }
                        if (rightHandFix == false)
                        {
                            leftHandPos += leftNormal * (handsSpace / 2);
                            rightHandPos += leftNormal * (handsSpace / 2);
                        }
                    }



                    Debug.DrawLine(forwardCheckHit.point, leftHandPos, Color.cyan);
                    // Debug.DrawRay(forwardCheckHit.point + leftNormal / 2, Vector3.up * (heightCheckHit.point.y - forwardCheckHit.point.y), Color.cyan);

                    Debug.DrawLine(forwardCheckHit.point, rightHandPos, Color.cyan);
                    // Debug.DrawRay(forwardCheckHit.point + rightNormal / 2, Vector3.up *(heightCheckHit.point.y - forwardCheckHit.point.y), Color.cyan);
                }
            }                         
        }
        else
        {
            obstacleDotProduct = 1;
        }

        return obstacleForward;
    }


    //Renvoie le target rotation du joueur selon le vector de direction envoyer
    public Quaternion TargetRotation(Vector3 targetDir, bool forceTurn = false, bool instantTurn = false)
    {
        Quaternion targetRotation = Quaternion.identity;

        if(!instantTurn)
        {
            if (!TouchMovingInput() && forceTurn == false)
            {
                targetRotation = transform.rotation;
            }
            else
            {
                Quaternion tr = Quaternion.LookRotation(targetDir);
                targetRotation = Quaternion.Slerp(
                   transform.rotation, tr,
                    Time.deltaTime * rotationSpeed);
            }
        }
        else
        {
            targetRotation = Quaternion.LookRotation(targetDir);
        }
       

        return targetRotation;
    }

    //Renvoie le targetDirection des input
    private Vector3 targetDir;
    public Vector3 TargetDirection(bool notRotTransform = false, bool baseOnlyOnCam = false)
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (!baseOnlyOnCam)
        {
            targetDir = (notRotTransform ? noRotCamTransform.forward : camTransform.forward) * vertical;
            targetDir += (notRotTransform ? noRotCamTransform.right : camTransform.right) * horizontal;
            targetDir.y = 0;
        }
        else
        {
            targetDir = (notRotTransform ? noRotCamTransform.forward : camTransform.forward);
        }
   


        return targetDir;
    }

    //Renvois le MoveAmount pour les animations
    private float currentMoveAmount;
    public float MoveAmount(bool canSprint = false)
    {
        float targetMoveAmount;

        Vector3 targetDir = TargetDirection();
        if (TouchMovingInput())
        {      
            if (!Sprint())
                targetMoveAmount = Vector3.Magnitude(targetDir);
            else
                targetMoveAmount = canSprint ? 2 : Vector3.Magnitude(targetDir);
        }
        else
            targetMoveAmount = 0;

        currentMoveAmount = Mathf.Lerp(currentMoveAmount, targetMoveAmount, Time.deltaTime * acceleration);

        return currentMoveAmount;
    }
    public void RestartMoveAmount()
    {
        currentMoveAmount = 0;
    }

    //Renvois le MoveAmount pour les animations
    public float MoveAmountOnSlope(OnSlopeData slopeData, bool canSprint = false)
    {
        float slopeMoveAmount;
        float targetMoveAmount;

        Vector3 targetDir = TargetDirection();
        if (TouchMovingInput())
        {
            if (!Sprint())
                targetMoveAmount = Vector3.Magnitude(targetDir);
            else
                targetMoveAmount = canSprint ? 2 : Vector3.Magnitude(targetDir);
        }
        else
            targetMoveAmount = 0;

        float slopDotDirection = Remap(Mathf.Clamp(slopeData.slopeDotDirection, -1, 0), -1, 0, 0, 1);
        slopeMoveAmount = Mathf.Clamp(Remap(slopeData.slopeAngle, minSlopeAffectSpeed, maxSlopeWalkable, 1, 0), 0, 1);
        slopeMoveAmount = Mathf.Clamp(slopeMoveAmount + slopDotDirection, 0, 1);
        slopeMoveAmount *= targetMoveAmount;

        if (slopeData.slopeAngle < maxSlopeWalkable)
        {
            if (slopeData.slopeDotDirection < 1)
                currentMoveAmount = Mathf.Lerp(currentMoveAmount, slopeMoveAmount, Time.deltaTime * overSlopeDecceleration);
            else
                currentMoveAmount = Mathf.Lerp(currentMoveAmount, targetMoveAmount * Remap(slopeData.slopeAngle, minSlopeAffectSpeed, 
                    maxSlopeWalkable, 1, 2), Time.unscaledDeltaTime * rotationSpeed);
        }
        else
            currentMoveAmount = Mathf.Lerp(currentMoveAmount, 0, Time.unscaledDeltaTime * overSlopeDecceleration);

        return currentMoveAmount;
    }

    //Renvois le TurnAmount pour les animations
    private float currentTurnAmount;
    public float TurnAmount()
    {
        float targetTurnAmount;

        if ((TargetRotation(TargetDirection()).eulerAngles - transform.eulerAngles).sqrMagnitude > 100)
        {
            targetTurnAmount = Vector3.Dot(transform.right, TargetDirection());

            currentTurnAmount = Mathf.Lerp(currentTurnAmount, targetTurnAmount, Time.unscaledDeltaTime * 10);
        }
        else
        {
            currentTurnAmount = Mathf.Lerp(currentTurnAmount, 0, Time.unscaledDeltaTime * 10);
        }

        return currentTurnAmount;
    }


    //Set la position du LookTarget pour que la tete du player tourne dans la direction avec le component LookAtIk
    public void LookTarget()
    {
        Vector3 lookTargetPosition = Vector3.Lerp(lookTarget.position, (anim.GetBoneTransform(HumanBodyBones.Head).position + camTransform.transform.forward * 10), Time.deltaTime * 8);
        lookTargetPosition.y = anim.GetBoneTransform(HumanBodyBones.Head).position.y;
        lookTarget.position = lookTargetPosition;
    }


    //Check quelle pied est devant pour pouvoir lancé les animation en conséquence du pied qui est en avant
    public bool RightFootForward()
    {
        bool result = false;

        Vector3 lf_relativPos = transform.InverseTransformPoint(leftFoot.position);
        Vector3 rf_relativPos = transform.InverseTransformPoint(rightFoot.position);

        result = false;
        if (rf_relativPos.z > lf_relativPos.z)
            result = true;

        return result;
    }

  
    //Check si on touche les moving Input en regardant le rawAxis
    public bool TouchMovingInput()
    {
        bool result = true;
        if (Input.GetAxisRaw("Horizontal") == 0 && Input.GetAxisRaw("Vertical") == 0)
            result = false;

        return result;
    }

    
    //Set les transform du jouer comme la rotation et la position et garde le model au centre du parent
    void StayInConrtoller()
    {
        anim.transform.localPosition = Vector3.zero;
        anim.transform.localEulerAngles = Vector3.zero;
    }


    //Check si le player touche au sol
    public bool grounded = false;
    private Vector3 groundPosition;
    public void CheckIfGrounded()
    {     
        Vector3 dir = -Vector3.up;

        Vector3 origin = transform.position;
        origin += transform.up * 2;

        float dis;

        if (grounded)
        {
            dis = groundCheckDistance + 2;
        }
        else
        {
            dis = airGroundCheckDistance * Vector3.Dot(Vector3.up, transform.up) + 2;
        }

        Debug.DrawRay(origin, dir * dis, Color.blue);
        
        RaycastHit hit;

        if (Physics.Raycast(origin, dir, out hit, dis, LayerMask.GetMask("Default")))
        {
            if (hit.transform.gameObject != gameObject)
            {
                groundPosition = hit.point;

                grounded = true;
            }
        }
        else
        {
            grounded = false;
        }


        if (grounded)
        {
            Vector3 heightPos = transform.position;
            heightPos.y = groundPosition.y;
            transform.position = Vector3.Lerp(transform.position, heightPos, Time.deltaTime * heightFromGroundAdaptation);
        }
    }


    //Check l'inclinaison du sol et determine si on la monde ou descent
    private bool sliding;
    public OnSlopeData CheckGroundSlope(Vector3 origin)
    {
        RaycastHit hit;
        if (Physics.SphereCast(origin, sphereCastRadius, Vector3.down, out hit, sphereCastDistance))
        {
            slopeDataResult.slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
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

                float[] tempArray = new float[] { slopeDataResult.slopeAngle, angleOne, angleTwo };
                System.Array.Sort(tempArray);
                slopeDataResult.slopeAngle = tempArray[1];
            }
            else
            {
                float average = (slopeDataResult.slopeAngle + angleOne) / 2;
                slopeDataResult.slopeAngle = average;
            }
        }

        if (slopeDataResult.slopeAngle > 1)
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

            slopeDataResult.slopeDirection = slopeDirPosition - firstHitPosition;
            slopeDataResult.slopeDotDirection = Vector3.Dot(transform.forward, slopeDataResult.slopeDirection);

            Debug.DrawRay(firstHitPosition, slopeDataResult.slopeDirection * 2, slopeDataResult.slopeAngle > maxSlopeWalkable && !sliding ? Color.yellow : sliding ? Color.red : Color.green);
        }

        return slopeDataResult;
    }



  
}

public class OnSlopeData
{
    public float slopeAngle;
    public Vector3 slopeDirection;
    public float slopeDotDirection;
}