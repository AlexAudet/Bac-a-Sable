using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;

namespace RootMotion.Demos
{
    public class IkCurveOffset : MonoBehaviour
    {
        AnimatorStateInfo stateInfo;
        Animator anim;

        public EffectorOffset effectorOffset;

        public IkCurve leftHandCurves = new IkCurve();
        public IkCurve rightHandCurves = new IkCurve();
        public IkCurve leftFootCurves = new IkCurve();
        public IkCurve rightFootCurves = new IkCurve();
        // Start is called before the first frame update
        void Start()
        {
            anim = GetComponent<Animator>();
    

        }

        float animAge;
        // Update is called once per frame
        void Update()
        {
            stateInfo = anim.GetCurrentAnimatorStateInfo(0);



            animAge += Time.deltaTime;
            if (animAge >= stateInfo.length)
            {
                Debug.Log(1111111);
                animAge = 0;
            }
           

            float normalized = animAge / stateInfo.length;


            Debug.Log(normalized + "    " + animAge);

            effectorOffset.leftHandOffset.x = leftHandCurves.x.Evaluate(normalized);
            effectorOffset.leftHandOffset.y = leftHandCurves.y.Evaluate(normalized);
            effectorOffset.leftHandOffset.z = leftHandCurves.x.Evaluate(normalized);

            effectorOffset.rightHandOffset.x = rightHandCurves.x.Evaluate(normalized);
            effectorOffset.rightHandOffset.y = rightHandCurves.y.Evaluate(normalized);
            effectorOffset.rightHandOffset.z = rightHandCurves.x.Evaluate(normalized);

            effectorOffset.leftFootOffset.x = leftFootCurves.x.Evaluate(normalized);
            effectorOffset.leftFootOffset.y = leftFootCurves.y.Evaluate(normalized);
            effectorOffset.leftFootOffset.z = leftFootCurves.x.Evaluate(normalized);

            effectorOffset.rightFootOffset.x = rightFootCurves.x.Evaluate(normalized);
            effectorOffset.rightFootOffset.y = rightFootCurves.y.Evaluate(normalized);
            effectorOffset.rightFootOffset.z = rightFootCurves.x.Evaluate(normalized);
        }
    }
}


[System.Serializable]
public class IkCurve
{
    public AnimationCurve x;
    public AnimationCurve y;
    public AnimationCurve z;
}
