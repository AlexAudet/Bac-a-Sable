using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
using RootMotion.FinalIK;

public class PlayerController : MonoBehaviour
{
    //si on affiche les Ray debug ou non
    public bool showDebug = false;

    //Class avec les nom des input pour le input manager
    [FoldoutGroup("Input Name")]
    [PropertySpace(10,10)]
    [HideLabel]
    [Indent]
    public InputName InputName;

    //Class avec les reference de transform pour le IK
    [FoldoutGroup("IK References")]
    [PropertySpace(10, 10)]
    [HideLabel]
    [Indent]
    public IKTransformRef IkRef;

    //Class avec les attributs de movement
    [FoldoutGroup("Movement Variable")]
    [PropertySpace(10, 10)]
    [HideLabel]
    [Indent]
    public MovementAttribut Movement;

    //Class avec les attributs de movement sur les slope
    [FoldoutGroup("On Slope Movement Variable")]
    [PropertySpace(10, 10)]
    [Indent]
    public bool slopeAffectMovement;
    [FoldoutGroup("On Slope Movement Variable")]
    [ShowIf("@this.slopeAffectMovement == true")]
    [PropertySpace(0, 10)]
    [HideLabel]
    [Indent]
    public OnSlopeAttribut OnSlope;

    //Class avec les attributs de climb sur les murs
    [FoldoutGroup("Wall Climb Variable")]
    [PropertySpace(10, 10)]
    [Indent]
    public bool canClimbWall;
    [FoldoutGroup("Wall Climb Variable")]
    [ShowIf("@this.canClimbWall == true")]
    [PropertySpace(0, 10)]
    [HideLabel]
    [Indent]
    public WallClimbAttribut WallClimb;


    #region privateVariable

    [HideInInspector] public PlayerState state;
    [HideInInspector] public FullBodyBipedIK IK;
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
        IK = GetComponentInChildren<FullBodyBipedIK>();

        leftFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);

        state = new NormalMovement(this, anim);
    }

    private void Update()
    {
        state = state.Process();
        StayInConrtoller();
    }

    private void FixedUpdate()
    {
        state.FixedUpdate();
    }


    void OnDrawGizmos()
    {
        if (showDebug)
        {
            // Visualize SphereCast with two spheres and a line
            Vector3 startPoint = new Vector3(transform.position.x, transform.position.y - (2 / 2) + OnSlope.startDistanceFromBottom, transform.position.z);
            Vector3 endPoint = new Vector3(transform.position.x, transform.position.y - (2 / 2) + OnSlope.startDistanceFromBottom - OnSlope.sphereCastDistance, transform.position.z);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, OnSlope.sphereCastRadius);
          

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(startPoint, endPoint);

           
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(obstacleData.leftHandPos, 0.2f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(obstacleData.rightHandPos, 0.2f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(obstacleData.playerOnPos, 0.15f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(obstacleData.playerHangPos, 0.15f);

        }
    }

    float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    //Regarde si on est en train de viser
    public bool OnAiming()
    {
        return Input.GetButton(InputName.AimInput);
    }

    //Regarde si on est en train de sauter
    public bool Jump()
    {
        return Input.GetButtonDown(InputName.JumpImput);
    }

    //Regarde si on est en train de Rouller
    public bool Roll()
    {
        return Input.GetButtonDown(InputName.RollInput);
    }

    //Check si on touche les moving Input en regardant le rawAxis
    public bool TouchMovingInput()
    {
        bool result = true;
        if (Input.GetAxisRaw("Horizontal") == 0 && Input.GetAxisRaw("Vertical") == 0) 
            result = false;

        anim.SetBool("TouchInput", result);

        return result;
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
                    Time.deltaTime * Movement.rotationSpeed);
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
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

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
   
       

        return targetDir.normalized;
    }

    //Renvois le MoveAmount pour les animations
    private float currentMoveAmount;
    public float MoveAmount()
    {
        float targetMoveAmount;

        Vector3 targetDir = TargetDirection();
        if (TouchMovingInput())
        {
            targetMoveAmount = Vector3.Magnitude(targetDir);
        }
        else
            targetMoveAmount = 0;

        currentMoveAmount = Mathf.Lerp(currentMoveAmount, targetMoveAmount, Time.deltaTime * Movement.acceleration);

        return currentMoveAmount;
    }

    //Renvois le MoveAmount pour les animations
    public float MoveAmountOnSlope(OnSlopeData slopeData, bool canSprint = false)
    {
        float slopeMoveAmount;
        float targetMoveAmount;

        Vector3 targetDir = TargetDirection();
        if (TouchMovingInput())
        {
            targetMoveAmount = Vector3.Magnitude(targetDir);
        }
        else
            targetMoveAmount = 0;

        float slopDotDirection = Remap(Mathf.Clamp(slopeData.slopeDotDirection, -1, 0), -1, 0, 0, 1);
        slopeMoveAmount = Mathf.Clamp(Remap(slopeData.slopeAngle, OnSlope.minSlopeAffectSpeed, OnSlope.maxSlopeWalkable, 1, 0), 0, 1);
        slopeMoveAmount = Mathf.Clamp(slopeMoveAmount + slopDotDirection, 0, 1);
        slopeMoveAmount *= targetMoveAmount;

        if (slopeData.slopeAngle < OnSlope.maxSlopeWalkable)
        {
            if (slopeData.slopeDotDirection < 1)
                currentMoveAmount = Mathf.Lerp(currentMoveAmount, slopeMoveAmount, Time.deltaTime * OnSlope.overSlopeDecceleration);
            else
                currentMoveAmount = Mathf.Lerp(currentMoveAmount, targetMoveAmount * Remap(slopeData.slopeAngle, OnSlope.minSlopeAffectSpeed,
                    OnSlope.maxSlopeWalkable, 1, 2), Time.unscaledDeltaTime * Movement.rotationSpeed);
        }
        else
            currentMoveAmount = Mathf.Lerp(currentMoveAmount, 0, Time.unscaledDeltaTime * OnSlope.overSlopeDecceleration);

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
        Vector3 lookTargetPosition = Vector3.Lerp(IkRef.lookTarget.position, (anim.GetBoneTransform(HumanBodyBones.Head).position + camTransform.transform.forward * 10), Time.deltaTime * 8);
        lookTargetPosition.y = anim.GetBoneTransform(HumanBodyBones.Head).position.y;
        IkRef.lookTarget.position = lookTargetPosition;
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
   
    //Set les transform du jouer comme la rotation et la position et garde le model au centre du parent
    void StayInConrtoller()
    {
        anim.transform.localPosition = Vector3.zero;
        anim.transform.localEulerAngles = Vector3.zero;
    }



    //Check si le player touche au sol
    [HideInInspector] public bool grounded = false;
    private Vector3 groundPosition;
    public void CheckIfGrounded()
    {

        Vector3 dir = -Vector3.up;

        Vector3 origin = transform.position;
        origin += transform.up * 2;

        float dis;

        if (grounded)
        {
            dis = Movement.groundCheckDistance + 2;
        }
        else
        {
            dis = Movement.airGroundCheckDistance * Vector3.Dot(Vector3.up, transform.up) + 2;
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
            transform.position = Vector3.Lerp(transform.position, heightPos, Time.deltaTime * Movement.heightAdaptationSpeed);
        }
    }

    //Check l'inclinaison du sol et determine si on la monde ou descent
    [HideInInspector] public OnSlopeData groundSlopeDataResult = new OnSlopeData();
    public OnSlopeData CheckGroundSlope()
    {
        Vector3 origin = transform.position;

        RaycastHit hit;
        if (Physics.SphereCast(origin, OnSlope.sphereCastRadius, Vector3.down, out hit, OnSlope.sphereCastDistance))
        {
            groundSlopeDataResult.slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            Vector3 temp = Vector3.Cross(hit.normal, Vector3.down);
        }

        RaycastHit slopeHit1;
        RaycastHit slopeHit2;

        if (Physics.Raycast(origin + OnSlope.rayOriginOffset1, Vector3.down, out slopeHit1, OnSlope.raycastLength))
        {
            if (showDebug) { Debug.DrawLine(origin + OnSlope.rayOriginOffset1, slopeHit1.point, Color.red); }

            float angleOne = Vector3.Angle(slopeHit1.normal, Vector3.up);

            if (Physics.Raycast(origin + OnSlope.rayOriginOffset2, Vector3.down, out slopeHit2, OnSlope.raycastLength))
            {

                if (showDebug) { Debug.DrawLine(origin + OnSlope.rayOriginOffset2, slopeHit2.point, Color.red); }

                float angleTwo = Vector3.Angle(slopeHit2.normal, Vector3.up);

                float[] tempArray = new float[] { groundSlopeDataResult.slopeAngle, angleOne, angleTwo };
                System.Array.Sort(tempArray);
                groundSlopeDataResult.slopeAngle = tempArray[1];
            }
            else
            {
                float average = (groundSlopeDataResult.slopeAngle + angleOne) / 2;
                groundSlopeDataResult.slopeAngle = average;
            }
        }

        if (groundSlopeDataResult.slopeAngle > 1)
        {
            Vector3 firstRay = transform.position + transform.forward * 0.2f;
            firstRay.y += 1;
            Debug.DrawRay(firstRay, -Vector3.up * 3, Color.green);

            Vector3 upOrDownRay = transform.position + transform.forward * 0.6f;
            upOrDownRay.y += 1;
            Debug.DrawRay(upOrDownRay, -Vector3.up * 3, Color.green);

            RaycastHit firstHit;
            RaycastHit slopeDirHit;

            Vector3 firstHitPosition = Vector3.zero;
            Vector3 slopeDirPosition = Vector3.zero;

            if (Physics.Raycast(firstRay, -Vector3.up, out firstHit, 3, LayerMask.GetMask("Default")))
            {
                firstHitPosition = firstHit.point;

            }

            Vector3 secondRay = firstHit.normal + transform.position;
            Debug.DrawRay(transform.position, firstHit.normal, Color.green);
            Debug.DrawRay(secondRay, -Vector3.up * 3, Color.green);

            if (Physics.Raycast(secondRay, -Vector3.up, out slopeDirHit, 3, LayerMask.GetMask("Default")))
                slopeDirPosition = slopeDirHit.point;

            groundSlopeDataResult.slopeDirection = slopeDirPosition - firstHitPosition;
            groundSlopeDataResult.slopeDotDirection = Vector3.Dot(transform.forward, groundSlopeDataResult.slopeDirection);

            Debug.DrawRay(firstHitPosition, groundSlopeDataResult.slopeDirection * 2, Color.green);
        }

        return groundSlopeDataResult;
    }

    // Regarde si il y a un obstacle devant le player
    private bool leftHandFix;
    private bool rightHandFix;
    public ObstacleForwardData obstacleData = new ObstacleForwardData();
    public ObstacleForwardData ObstacleForward()
    {

        Vector3 forwardCheckOrigin = transform.position;
        forwardCheckOrigin.y += 1;


        Vector3 forwardCheckDirection = transform.forward;

        // if(leftHandFix && rightHandFix)
        // {
        //     if (Vector3.Distance(transform.position, obstacleData.leftHandPos) > Vector3.Distance(transform.position, obstacleData.rightHandPos))
        //         forwardCheckDirection = Vector3.Lerp(-transform.right, transform.forward, obstacleDotProduct);
        //     else
        //         forwardCheckDirection = Vector3.Lerp(transform.right, transform.forward, obstacleDotProduct);
        //     forwardCheckDirection = Vector3.Lerp(forwardCheckDirection, transform.forward, 0.5f);
        // }


        Debug.DrawRay(forwardCheckOrigin, forwardCheckDirection * Movement.forwardObstacleCheckDistance, Color.cyan);


        RaycastHit forwardCheckHit;
        // regarde devant le joueur si il y a un mur
        if (Physics.Raycast(forwardCheckOrigin, forwardCheckDirection, out forwardCheckHit, Movement.forwardObstacleCheckDistance))
        {
            obstacleData.distance = Vector3.Distance(forwardCheckOrigin, forwardCheckHit.point);
            obstacleData.playerHangdAngle = -forwardCheckHit.normal;
            obstacleData.normal = forwardCheckHit.normal;


            //obstacleDotProduct = Vector3.Dot(-forwardCheckHit.normal, transform.forward);


            Vector3 heightCheckOrigin = forwardCheckHit.point + -forwardCheckHit.normal / 2;
            heightCheckOrigin.y += 4;

            RaycastHit heightCheckHit;

            // regarde la hauteur du mur
            if (Physics.Raycast(heightCheckOrigin, Vector3.down, out heightCheckHit, 5))
            {

                Vector3 leftHandSurfaceNormal;
                Vector3 rightHandSurfaceNormal;

                obstacleData.playerOnPos = heightCheckHit.point;
                obstacleData.relativeHeight = heightCheckHit.point.y - transform.position.y;


                //donne la direction gauche de la normal de l'obstacle
                Vector3 leftNormal = -Vector3.Cross(forwardCheckHit.normal, Vector3.up).normalized;
                obstacleData.leftHandPos = forwardCheckHit.point + leftNormal * (WallClimb.handsSpace / 2);
                obstacleData.leftHandPos.y = heightCheckHit.point.y;

                //donne la direction droite de la normal de l'obstacle
                Vector3 rightNormal = Vector3.Cross(forwardCheckHit.normal, Vector3.up).normalized;
                obstacleData.rightHandPos = forwardCheckHit.point + rightNormal * (WallClimb.handsSpace / 2);
                obstacleData.rightHandPos.y = heightCheckHit.point.y;

                #region First Hands Place Check
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                ///Regarde la place de la main gauche
                RaycastHit leftCheckHit;
                Vector3 leftOrigin = -forwardCheckHit.normal / 3;
                leftOrigin.y = 0;
                leftOrigin += obstacleData.leftHandPos + (Vector3.up / 2);

                if (obstacleData.canBeClimb) Debug.DrawRay(leftOrigin, Vector3.down * 1, Color.cyan);
                if (Physics.Raycast(leftOrigin, Vector3.down, out leftCheckHit, 1, LayerMask.GetMask("Default")))
                {
                    if (leftCheckHit.point.y == heightCheckHit.point.y)
                    {

                        leftHandFix = true;
                    }
                    else
                    {

                        //regarde si les normals sont plus ou moins parreille pour savoir si c'est un angle ou un palier
                        if (leftCheckHit.normal == heightCheckHit.normal)
                        {

                            leftHandFix = true;
                        }
                        else
                        {

                            leftHandFix = false;
                        }

                    }
                }
                else
                    leftHandFix = false;

                leftOrigin += forwardCheckHit.normal / 1.5f;
                if (obstacleData.canBeClimb) Debug.DrawRay(leftOrigin, Vector3.down * (heightCheckHit.point.y - forwardCheckHit.point.y + 0.5f), Color.cyan);
                if (Physics.Raycast(leftOrigin, Vector3.down, heightCheckHit.point.y - forwardCheckHit.point.y + 0.5f, LayerMask.GetMask("Default")))
                {
                    leftHandFix = false;
                }

                ///Regarde la place de la main droite
                RaycastHit rightCheckHit;
                Vector3 rightOrigin = -forwardCheckHit.normal / 3;
                rightOrigin.y = 0;
                rightOrigin += obstacleData.rightHandPos + (Vector3.up / 2);

                if (obstacleData.canBeClimb) Debug.DrawRay(rightOrigin, Vector3.down * 1, Color.cyan);
                if (Physics.Raycast(rightOrigin, Vector3.down, out rightCheckHit, 1, LayerMask.GetMask("Default")))
                {
                    if (rightCheckHit.point.y == heightCheckHit.point.y)
                    {
                        rightHandFix = true;
                    }
                    else
                    {
                        //regarde si les normals sont plus ou moins parreille pour savoir si c'est un angle ou un palier
                        if (rightCheckHit.normal == heightCheckHit.normal)
                        {
                            rightHandFix = true;
                        }
                        else
                            rightHandFix = false;
                    }
                }
                else
                    rightHandFix = false;

                rightOrigin += forwardCheckHit.normal / 1.5f;
                if (obstacleData.canBeClimb) Debug.DrawRay(rightOrigin, Vector3.down * (heightCheckHit.point.y - forwardCheckHit.point.y + 0.5f), Color.cyan);
                if (Physics.Raycast(rightOrigin, Vector3.down, heightCheckHit.point.y - forwardCheckHit.point.y + 0.5f, LayerMask.GetMask("Default")))
                {
                    rightHandFix = false;
                }
                /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                #endregion

                //si les deux main sont dans le vire, le mur ne peut pas etre monté

                bool recheck = false;
                if (rightHandFix == false && leftHandFix == true)
                {
                    obstacleData.playerOnPos += leftNormal * (WallClimb.handsSpace / 2);
                    obstacleData.leftHandPos += leftNormal * (WallClimb.handsSpace / 2);
                    obstacleData.rightHandPos += leftNormal * (WallClimb.handsSpace / 2);

                    leftOrigin += leftNormal * (WallClimb.handsSpace / 2);
                    rightOrigin += leftNormal * (WallClimb.handsSpace / 2);

                    recheck = true;
                }
                else if (leftHandFix == false && rightHandFix == true)
                {
                    obstacleData.playerOnPos += rightNormal * (WallClimb.handsSpace / 2);
                    obstacleData.leftHandPos += rightNormal * (WallClimb.handsSpace / 2);
                    obstacleData.rightHandPos += rightNormal * (WallClimb.handsSpace / 2);

                    leftOrigin += rightNormal * (WallClimb.handsSpace / 2);
                    rightOrigin += rightNormal * (WallClimb.handsSpace / 2);

                    recheck = true;
                }


                if (recheck)
                {
                    #region Second Hands Place Check
                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    ///Regarde la place de la main gauche
                    leftOrigin -= forwardCheckHit.normal / 1.5f;
                    leftOrigin += Vector3.up * 2;
                    if (obstacleData.canBeClimb) Debug.DrawRay(leftOrigin, Vector3.down * 4, Color.red);
                    if (Physics.Raycast(leftOrigin, Vector3.down, out leftCheckHit, 4, LayerMask.GetMask("Default")))
                    {
                        leftHandSurfaceNormal = leftCheckHit.normal;

                        //si la hauteur est pareille ca veut dire que c'est droit
                        if (leftCheckHit.point.y == heightCheckHit.point.y)
                        {
                            leftHandFix = true;
                        }
                        else
                        {
                            //regarde si les normals sont plus ou moins parreille pour savoir si c'est un angle ou un palier
                            if (leftCheckHit.normal == heightCheckHit.normal)
                            {
                                leftHandFix = true;
                            }
                            else
                                leftHandFix = false;
                        }
                    }
                    else
                        leftHandFix = false;

                    leftOrigin += forwardCheckHit.normal / 1.5f;
                    if (obstacleData.canBeClimb) Debug.DrawRay(leftOrigin, Vector3.down * (heightCheckHit.point.y - forwardCheckHit.point.y + 3.5f), Color.red);
                    if (Physics.Raycast(leftOrigin, Vector3.down, heightCheckHit.point.y - forwardCheckHit.point.y + 3.5f, LayerMask.GetMask("Default")))
                    {
                        leftHandFix = false;
                    }

                    ///Regarde la place de la main droite
                    rightOrigin -= forwardCheckHit.normal / 1.5f;
                    rightOrigin += Vector3.up * 2;
                    if (obstacleData.canBeClimb) Debug.DrawRay(rightOrigin, Vector3.down * 4, Color.red);
                    if (Physics.Raycast(rightOrigin, Vector3.down, out rightCheckHit, 4, LayerMask.GetMask("Default")))
                    {
                        rightHandSurfaceNormal = rightCheckHit.normal;

                        //si la hauteur est pareille ca veut dire que c'est droit
                        if (rightCheckHit.point.y == heightCheckHit.point.y)
                        {
                            rightHandFix = true;
                        }
                        else
                        {
                            //regarde si les normals sont plus ou moins parreille pour savoir si c'est un angle ou un palier
                            if (rightCheckHit.normal == heightCheckHit.normal)
                            {
                                rightHandFix = true;
                            }
                            else
                                rightHandFix = false;
                        }
                    }
                    else
                        rightHandFix = false;

                    rightOrigin += forwardCheckHit.normal / 1.5f;
                    if (obstacleData.canBeClimb) Debug.DrawRay(rightOrigin, Vector3.down * (heightCheckHit.point.y - forwardCheckHit.point.y + 3.5f), Color.red);
                    if (Physics.Raycast(rightOrigin, Vector3.down, heightCheckHit.point.y - forwardCheckHit.point.y + 3.5f, LayerMask.GetMask("Default")))
                    {
                        rightHandFix = false;
                    }
                    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    #endregion
                }


                // Si tout est beau et que les deux main sont fixés
                if (leftHandFix == true && rightHandFix == true)
                {
                    obstacleData.canBeClimb = true;

                    obstacleData.leftHandPos.y = leftCheckHit.point.y;
                    obstacleData.rightHandPos.y = rightCheckHit.point.y;
                    obstacleData.playerHangPos = Vector3.Lerp(obstacleData.leftHandPos, obstacleData.rightHandPos, 0.5f);
                    obstacleData.playerHangPos += forwardCheckHit.normal * WallClimb.hangDistanceFromWall;
                    obstacleData.playerHangPos.y -= WallClimb.hangDistanceFromTop;
                }
                else
                {
                    obstacleData.canBeClimb = false;
                }

            }





            if (obstacleData.canBeClimb) Debug.DrawLine(forwardCheckHit.point, obstacleData.leftHandPos, Color.cyan);
            if (obstacleData.canBeClimb) Debug.DrawLine(forwardCheckHit.point, obstacleData.rightHandPos, Color.cyan);



            if (obstacleData.canBeClimb) Debug.DrawRay(heightCheckOrigin, Vector3.down * 5, Color.cyan);

        }
        else
        {
            obstacleData.canBeClimb = false;
        }

        return obstacleData;
    }


}
[System.Serializable]
public class OnSlopeData
{
    public float slopeAngle;
    public Vector3 slopeDirection;
    public float slopeDotDirection;
}


public class ObstacleForwardData
{
    public bool canBeClimb;
    public float distance;
    public float relativeHeight;
    public Vector3 normal;
    public Vector3 playerHangPos;
    public Vector3 playerHangdAngle;
    public Vector3 playerOnPos;
    public Vector3 leftHandPos;
    public Vector3 rightHandPos;
}

[System.Serializable]
public class IKTransformRef
{
    //le point que le player regarde
    [PropertySpace(0,10)]
    public Transform lookTarget;

    //le point que la main gauche se fixe
    [PropertySpace(0, 5)]
    public Transform leftHandTransform;

    //le point que la main droite se fixe
    [PropertySpace(0, 10)]
    public Transform rightHandTransform;

    //le point que la main gauche se fixe
    [PropertySpace(0, 5)]
    public Transform leftFootTransform;

    //le point que la main droite se fixe
    public Transform rightFootransform;
}

[System.Serializable]
public class InputName
{
    [PropertySpace(0, 5)]
    public string AimInput = "Aim";

    [PropertySpace(0, 5)]
    public string JumpImput = "Jump";

    [PropertySpace(0, 5)]
    public string RollInput = "Roll";
}

[System.Serializable]
public class MovementAttribut
{
    [Title("Speed", TitleAlignment = TitleAlignments.Centered)]
    // speed multiply du joueur
    [PropertySpace(0,5), Range(1,5)]
    public float speed = 1;
    // l'acceleleration du joueur
    [PropertySpace(0, 5), Range(1, 10)]
    public float acceleration = 4;
    // la vitesse de rotation du joueur
    [PropertySpace(0, 5), Range(1, 10)]
    public float rotationSpeed = 5;
    //La vitesse a que la hauteur du joueur s'adapte au sol
    [PropertySpace(0, 5), Range(1, 100)]
    public float heightAdaptationSpeed = 20;
    //Multiplicateur du rootmotion des animations de roulade
    [PropertySpace(0, 5), Range(1, 5)]
    public float rollDistanceMultiply = 2;



    [Title("Jump And In Air", TitleAlignment = TitleAlignments.Centered)]
    //Force des Saut en hauteur
    [PropertySpace(0, 5), Range(1, 20)]
    public float jumpUpForce = 3;
    //Force des Saut en avant
    [PropertySpace(0, 5), Range(1, 10)]
    public float jumpForwardForce = 4.5f;
    //le nombre de saut que l'on peut faire dans les air
    [PropertySpace(0, 5), Range(1, 10)]
    public int airJumpAmount = 2;
    //la vitesse d'acceleration pour bouger dans les airs
    [PropertySpace(0, 5), Range(1, 100)]
    public float airControlSpeed = 5;
    //la force de la gravité si le joueur ne touche pas le sol
    [PropertySpace(0, 5), Range(-10, 0)]
    public float gravityForce = -5;


    [Title("Raycast Checker Distance", TitleAlignment = TitleAlignments.Centered)]
    // la distance que le player check si il est face a un mur ou non
    [PropertySpace(0, 5)]
    public float forwardObstacleCheckDistance = 3f;
    // A partir de quelle distance le player est considéré dans les airs
    [PropertySpace(0, 5)]
    public float groundCheckDistance = 0.5f;
    // A partir de quelle distance le player est considéré dans les airs
    [PropertySpace(0, 5)]
    public float airGroundCheckDistance = 0.1f;
}

[System.Serializable]
public class OnSlopeAttribut
{
    //l'angle maximum que l'on peut monter
    public float maxSlopeWalkable = 35f;
    //l'angle minimum qui affecte la vitesse de déplacement
    public float minSlopeAffectSpeed = 15;
    //la vitesse de decceleration sur les slopes
    public float overSlopeDecceleration = 3;

    //Est-ce qu'on peut glisser lorsque que l'angle du slope est plus grande que maxSlopeWalkable
    public bool canSlide;
    //l'acceleration du glissement 
    public float slideingAcceleration = 0.5f;
    //range de vitesse de glissement (si angle = maxSlopeWalkable = minimum vitesse / angle 90 degré = maximum vitesse)
    public Vector2 slidingSpeedRange = new Vector2(2, 15);

    [Space(20)]
    public float startDistanceFromBottom = 0.2f;
    public float sphereCastRadius = 0.5f;
    public float sphereCastDistance = 0.2f;
    public float raycastLength = 1f;
    public Vector3 rayOriginOffset1 = new Vector3(-0.2f, 0.5f, 0.16f);
    public Vector3 rayOriginOffset2 = new Vector3(0.2f, 0.5f, -0.16f);

}

[System.Serializable]
public class WallClimbAttribut
{
    //Distance entre les main lorsque qu'on est sur un mur
    public float handsSpace = 1;
    //La distance que le joueur se tien du top du mur lorsqu'il est accrocher sur le rebord
    public float hangDistanceFromTop = 1.7f;
    //La distance que le joueur se tien du du mur lorsqu'il est accrocher sur le rebord
    public float hangDistanceFromWall = 0.5f;
    //la distance maximum du top du mur pour pouvoir s'accrocher au rebord
    public float distanceFromTopToHang = 2;
    //la distance maximum du mur pour pouvoir s'accrocher au rebord
    public float distanceFromWallToHang = 1;
}