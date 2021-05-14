using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


[System.Serializable]
public struct Wheel
{
    public GameObject model;
    public WheelCollider collider;
    public bool turningWheel;
}

public class CarController : MonoBehaviour
{
    Rigidbody _rb;
    WheelFrictionCurve sideFriction = new WheelFrictionCurve();
    
    #region Input Name
    [FoldoutGroup("Input", false)]
    [SerializeField, Indent, PropertySpace(10,5)] 
    string throttleInput = "Throttle";
    [FoldoutGroup("Input")]
    [SerializeField, Indent, PropertySpace(0, 5)] 
    string brakeInput = "Brake";
    [FoldoutGroup("Input")]
    [SerializeField, Indent, PropertySpace(0, 5)] 
    string turnInput = "Horizontal";
    [FoldoutGroup("Input")]
    [SerializeField, Indent, PropertySpace(0, 5)] 
    string jumpInput = "Jump";
    [FoldoutGroup("Input")]
    [SerializeField, Indent, PropertySpace(0, 5)] 
    string driftInput = "Drift";
    [FoldoutGroup("Input")]
    [SerializeField, Indent, PropertySpace(0, 10)] 
    string boostInput = "Boost";
    #endregion


    #region Wheels Reference
    [FoldoutGroup("Wheels References", false)]
    [SerializeField, Indent, PropertySpace(10,10)] 
    Wheel[] wheels;
    #endregion


    #region Car Movement Attributes
    [FoldoutGroup("Car Movement", false)]
    [Title("Movement Attributes")]
    [SerializeField, Indent, PropertySpace(10, 5)] 
    AnimationCurve motorTorque = new AnimationCurve(new Keyframe(0, 200), new Keyframe(50, 300), new Keyframe(200, 0));

    // Differential gearing ratio
    [FoldoutGroup("Car Movement")]
    [SerializeField, Range(2, 100), Indent, PropertySpace(0, 5)] 
    float diffGearing = 4.0f;

    // Basicaly how hard it brakes
    [FoldoutGroup("Car Movement")]
    [SerializeField, Indent, PropertySpace(0, 10)] 
    float brakeForce = 1500.0f;
    #endregion

    #region Car Turn Attributes
    [Title("Turn Attributes")]
    [FoldoutGroup("Car Movement")]
    [SerializeField, Indent, PropertySpace(0, 5)] 
    AnimationCurve turnInputCurve = AnimationCurve.Linear(-1.0f, -1.0f, 1.0f, 1.0f);

    // Max steering hangle, usualy higher for drift car
    [FoldoutGroup("Car Movement")]
    [SerializeField, Range(0f, 50.0f), Indent, PropertySpace(0, 5)] 
    float steerAngle = 30.0f;

    // The value used in the steering Lerp, 1 is instant (Strong power steering), and 0 is not turning at all
    [FoldoutGroup("Car Movement")]
    [SerializeField, Range(0.001f, 1.0f), Indent, PropertySpace(0, 10)] 
    float steerSpeed = 0.2f;
    #endregion


    #region Bosst Attributes
    // Disable boost
    [HideInInspector] public bool allowBoost = true;

    // Maximum boost available
    [FoldoutGroup("Car Boost", false)]
    [SerializeField, Indent, PropertySpace(10,5)] 
    float maxBoost = 10f;

    // Current boost available
    [FoldoutGroup("Car Boost")]
    [SerializeField, Indent, PropertySpace(0, 5), ReadOnly] 
    float currentBoost = 10f;

    // Regen boostRegen per second until it's back to maxBoost
    [FoldoutGroup("Car Boost")]
    [SerializeField, Range(0f, 1f), Indent, PropertySpace(0, 5)] 
    float boostRegen = 0.2f;

   
    //The force applied to the car when boosting
    //NOTE: the boost does not care if the car is grounded or not
    [FoldoutGroup("Car Boost")]
    [SerializeField, Indent, PropertySpace(0, 10)] 
    float boostForce = 5000;
    #endregion

    [FoldoutGroup("Suspension Attributes", false)]
    [SerializeField, Indent, Range(0, 20), PropertySpace(10, 5)]
    float naturalFrequency = 10;

    [FoldoutGroup("Suspension Attributes")]
    [SerializeField, Indent, Range(0, 3), PropertySpace(0, 5)]
    float dampingRatio = 0.8f;

    [FoldoutGroup("Suspension Attributes")]
    [SerializeField, Indent, Range(-1, 1), PropertySpace(0, 5)]
    float forceShift = 0.03f;

    [FoldoutGroup("Suspension Attributes")]
    [SerializeField, Indent, PropertySpace(0, 10)]
    bool setSuspensionDistance = true;


    #region Other Attributes
    // How hight do you want to jump?
    [FoldoutGroup("Car Other Attributes", false)]
    [SerializeField, Range(1f, 1.5f), Indent, PropertySpace(10,5)] 
    float jumpVel = 1.3f;

    // Use this to disable drifting
    [FoldoutGroup("Car Other Attributes")]
    [Indent, PropertySpace(0, 5)]
    public bool allowDrift = true;

    // How hard do you want to drift?
    [FoldoutGroup("Car Other Attributes")]
    [SerializeField, Range(0.0f, 2f), Indent, PropertySpace(0, 5)] 
    float driftIntensity = 1f;


    // Force aplied downwards on the car, proportional to the car speed
    [FoldoutGroup("Car Other Attributes")]
    [SerializeField, Range(0.5f, 10f), Indent, PropertySpace(0, 5)] 
    float downforce = 1.0f;

    // The Center of mass of the car
    [FoldoutGroup("Car Other Attributes")]
    [SerializeField, Indent, PropertySpace(0, 10)] 
    Transform centerOfMass;
    #endregion



    [FoldoutGroup("Wheels Friction Attributes", false)]
    [SerializeField, Indent, PropertySpace(10,10)]
    float wheelSideFriction = 1f;

    [FoldoutGroup("Wheels Friction Attributes")]
    [SerializeField, Indent(2)]
    float extrenumSlip = 0.2f;

    [FoldoutGroup("Wheels Friction Attributes")]
    [SerializeField, Indent(2)]
    float extrenumValue = 1f;

    [FoldoutGroup("Wheels Friction Attributes")]
    [SerializeField, Indent(2)]
    float asymptoteSlip = 0.5f;

    [FoldoutGroup("Wheels Friction Attributes")]
    [SerializeField, Indent(2), PropertySpace(0, 10)]
    float asymptoteValue = 0.75f;




    // Exhaust fumes
    [FoldoutGroup("Particles Reference", false)]
    [SerializeField, Indent, PropertySpace(10,5)]
    ParticleSystem[] gasParticles;

    // Boost fumes
    [FoldoutGroup("Particles Reference")]
    [SerializeField, Indent, PropertySpace(0, 10)] 
    ParticleSystem[] boostParticles;


    // [SerializeField] AudioClip boostClip;
    // [SerializeField] AudioSource boostSource;



    // When IsPlayer is false you can use this to control the steering
    [FoldoutGroup("Car Data", false), ReadOnly]
    float steering;


    // When IsPlayer is false you can use this to control the throttle
    [FoldoutGroup("Car Data"), ReadOnly]
    float throttle;

    // Like your own car handbrake, if it's true the car will not move
    [FoldoutGroup("Car Data"), ReadOnly]
    [SerializeField] bool handbrake;
    bool Handbrake { get { return handbrake; } set { handbrake = value; } }

    [FoldoutGroup("Car Data")]
    [SerializeField, ReadOnly]
    bool drift;

    // Use this to read the current car speed (you'll need this to make a speedometer)
    [FoldoutGroup("Car Data")]
    [SerializeField, ReadOnly]
    float speed = 0.0f;

    // Use this to boost when IsPlayer is set to false
    [FoldoutGroup("Car Data")]
    [SerializeField, ReadOnly]
    bool boosting = false;

    // Use this to jump when IsPlayer is set to false
    [FoldoutGroup("Car Data")]
    [SerializeField, ReadOnly]
    bool jumping = false;


    [FoldoutGroup("Car Data")]
    [SerializeField, ReadOnly]
    bool isGrounded = false;

    int lastGroundCheck = 0;

    public bool IsGrounded
    {
        get
        {
            if (lastGroundCheck == Time.frameCount)
                return isGrounded;

            lastGroundCheck = Time.frameCount;
            isGrounded = true;
            foreach (Wheel wheel in wheels)
            {
                if (!wheel.collider.gameObject.activeSelf || !wheel.collider.isGrounded)
                    isGrounded = false;
            }
            return isGrounded;
        }
    }



    void Start()
    {
      //  if (boostClip != null)
      //  {
      //      boostSource.clip = boostClip;
      //  }

        currentBoost = maxBoost;

        _rb = GetComponent<Rigidbody>();

        if (_rb != null && centerOfMass != null)
        {
            _rb.centerOfMass = centerOfMass.localPosition;
        }


        // Set the motor torque to a non null value because 0 means the wheels won't turn no matter what
        foreach (Wheel wheel in wheels)
        {
            wheel.collider.motorTorque = 0.0001f;
        }



        sideFriction.stiffness = wheelSideFriction;
        sideFriction.extremumSlip = extrenumSlip;
        sideFriction.extremumValue = extrenumValue;
        sideFriction.asymptoteSlip = asymptoteSlip;
        sideFriction.asymptoteValue = asymptoteValue;
    }


    // Visual feedbacks and boost regen
    void Update()
    {

        foreach (ParticleSystem gasParticle in gasParticles)
        {
            gasParticle.Play();
            ParticleSystem.EmissionModule em = gasParticle.emission;
            em.rateOverTime = handbrake ? 0 : Mathf.Lerp(em.rateOverTime.constant, Mathf.Clamp(150.0f * throttle, 30.0f, 100.0f), 0.1f);
        }

        if (allowBoost)
        {
            currentBoost += Time.deltaTime * boostRegen;
            if (currentBoost > maxBoost) { currentBoost = maxBoost; }
        }


        AnimatedWheels();
        SuspensionManager();
    }


    // Update everything
    void FixedUpdate()
    {
        // Mesure current speed
        speed = transform.InverseTransformDirection(_rb.velocity).z * 3.6f;

        // Get all the inputs!

        // Accelerate and brake
        if (throttleInput != "" && throttleInput != null)
        {
            throttle = GetInput(throttleInput) - GetInput(brakeInput);
        }
        // Boost
        boosting = (GetInput(boostInput) > 0.5f);
        // Turn
        steering = turnInputCurve.Evaluate(GetInput(turnInput)) * steerAngle;
        // Dirft
        drift = GetInput(driftInput) > 0 && _rb.velocity.sqrMagnitude > 100;
        // Jump
        jumping = GetInput(jumpInput) != 0;


        foreach (Wheel wheel in wheels)
        {
            if (wheel.turningWheel)
                wheel.collider.steerAngle = Mathf.Lerp(wheel.collider.steerAngle, steering, steerSpeed);

            wheel.collider.brakeTorque = 0;
        }

        // Movement and Brake
        if (handbrake)
        {
            foreach (Wheel wheel in wheels)
            {
                // Don't zero out this value or the wheel completly lock up
                wheel.collider.motorTorque = 0.0001f;
                wheel.collider.brakeTorque = brakeForce;
            }
        }
        else if (Mathf.Abs(speed) < 4 || Mathf.Sign(speed) == Mathf.Sign(throttle))
        {       
            foreach (Wheel wheel in wheels)
            {
                wheel.collider.motorTorque = throttle * motorTorque.Evaluate(speed) * diffGearing / wheels.Length;
            }                  
        }
        else
        {
      
            foreach (Wheel wheel in wheels)
            {
                wheel.collider.brakeTorque = Mathf.Abs(throttle) * brakeForce;
            }
        }

        // Jump
        if (jumping)
        {
            if (!IsGrounded)
                return;

            _rb.velocity += transform.up * jumpVel;
        }

        // Boost
        if (boosting && allowBoost && currentBoost > 0.1f)
        {
            _rb.AddForce(transform.forward * boostForce);

            currentBoost -= Time.fixedDeltaTime;
            if (currentBoost < 0f) { currentBoost = 0f; }

            if (boostParticles.Length > 0 && !boostParticles[0].isPlaying)
            {
                foreach (ParticleSystem boostParticle in boostParticles)
                {
                    boostParticle.Play();
                }
            }

          // if (boostSource != null && !boostSource.isPlaying)
          // {
          //     boostSource.Play();
          // }
        }
        else
        {
            if (boostParticles.Length > 0 && boostParticles[0].isPlaying)
            {
                foreach (ParticleSystem boostParticle in boostParticles)
                {
                    boostParticle.Stop();
                }
            }

          //  if (boostSource != null && boostSource.isPlaying)
          //  {
          //      boostSource.Stop();
          //  }
        }

        // Drift
        if (drift && allowDrift)
        {
            sideFriction.stiffness = 1;
            foreach (Wheel wheel in wheels)
            {
                wheel.collider.sidewaysFriction = sideFriction;
            }

            Vector3 driftForce = -transform.right;
            driftForce.y = 0.0f;
            driftForce.Normalize();

            if (steering != 0)
                driftForce *= _rb.mass * speed / 7f * throttle * steering / steerAngle;
            Vector3 driftTorque = transform.up * 0.1f * steering / steerAngle;


            _rb.AddForce(driftForce * driftIntensity, ForceMode.Force);
            _rb.AddTorque(driftTorque * driftIntensity, ForceMode.VelocityChange);
        }
        else
        {
            sideFriction.stiffness = wheelSideFriction;

            foreach (Wheel wheel in wheels)
            {
                wheel.collider.sidewaysFriction = sideFriction;
            }

        }

        // Downforce
        _rb.AddForce(-transform.up * speed * downforce);
    }


    // Use this method if you want to use your own input manager
    private float GetInput(string input)
    {
        return Input.GetAxis(input);
    }

    private void AnimatedWheels()
    {
        foreach (Wheel wheel in wheels)
        {
            Quaternion _rot;
            Vector3 _pos;
            wheel.collider.GetWorldPose(out _pos, out _rot);
            wheel.model.transform.position = _pos;
            wheel.model.transform.rotation = _rot;

        }
    }

    private void SuspensionManager()
    {
        // work out the stiffness and damper parameters based on the better spring model
        foreach (WheelCollider wc in GetComponentsInChildren<WheelCollider>())
        {
            JointSpring spring = wc.suspensionSpring;

            spring.spring = Mathf.Pow(Mathf.Sqrt(wc.sprungMass) * naturalFrequency, 2);
            spring.damper = 2 * dampingRatio * Mathf.Sqrt(spring.spring * wc.sprungMass);

            wc.suspensionSpring = spring;

            Vector3 wheelRelativeBody = transform.InverseTransformPoint(wc.transform.position);
            float distance = GetComponent<Rigidbody>().centerOfMass.y - wheelRelativeBody.y + wc.radius;

            wc.forceAppPointDistance = distance - forceShift;

            // the following line makes sure the spring force at maximum droop is exactly zero
            if (spring.targetPosition > 0 && setSuspensionDistance)
                wc.suspensionDistance = wc.sprungMass * Physics.gravity.magnitude / (spring.targetPosition * spring.spring);
        }
    }
}
