using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Step Flow")]
[ExecuteInEditMode]
public class StepFlow : MonoBehaviour
{
    #region CustomVariables

    public enum Orientation
    {
        X,
        Y,
        Z,
        NegativeX,
        NegativeY,
        NegativeZ
    };

    struct line
    {
        public Vector2 a;
        public Vector2 b;
    }
    struct legTransform
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 SurfaceNormal;
    }

    public struct boxCastInfo
    {
        public Vector3 Center;
        public Vector3 Size;
        public Vector3 Direction;
        public Quaternion Orientation;
    }
    [System.Serializable]
    public class LegBounds
    {
        public Orientation Axis;
        public Vector3 size;
        [Range(-1f, 1f)]
        public float ShiftCenterX;
        [Range(-1f, 1f)]
        public float ShiftCenterY;
        [Range(-1f, 1f)]
        public float ShiftCenterZ;
    }

    [System.Serializable]
    public class LegPosition
    {
        [Range(-0.5f, 0.5f)]
        public float positionX;
        [Range(-0.5f, 0.5f)]
        public float positionY;

        [Range(-90, 90)]
        public float YRotation;
    }
    #endregion

    [Tooltip("Animator component asigned to the character you want to control")]
    [SerializeField]
    public Animator anim;
    [Tooltip("Parent Object that is being moved")]
    [SerializeField]
    public Transform ParentObject;
    [Tooltip("Character's hips")]
    [SerializeField]
    public Transform Hips;
    [Tooltip("Speed at which the ")]
    [SerializeField]
    public float DirectionLerpSpeed = 20;
    [SerializeField]
    public float SpeedLerpSpeed = 6;
    [SerializeField]
    public LayerMask mask;
    [SerializeField]
    public float FeetHeight;
    [SerializeField]
    public bool ShowLegBounds;
    [SerializeField]
    public LegBounds leftLegBounds;
    [SerializeField]
    public LegBounds rightLegBounds;
    [SerializeField]
    public LegPosition LeftLegPlacement;
    [SerializeField]
    public LegPosition RightLegPlacement;
    [SerializeField]
    public float StepDistanceMultiplier = 0.7f;
    [SerializeField]
    public float MinStepDistance = 0.03f;
    [SerializeField]
    public float MaxStepDistance = 1.85f;
    [SerializeField]
    public float RotationError = 60;
    [SerializeField]
    public Vector2 HipsHeight;
    [SerializeField]
    public bool AvoidObstacles = true;
    [SerializeField]
    public int ColisionQuality = 20;
    [SerializeField]
    public bool SmoothSteppingOver = true;
    public int SmoothingQuality = 100;
    [SerializeField]
    public float MaxFeetAngle = 50;
    [SerializeField]
    public Transform LowestPointOfCharacter;
    [SerializeField]
    public float maxStepHeight = 0.5f;
    [SerializeField]
    public float walkSpeed;
    [SerializeField]
    public float RunSpeed;
    public float settleFeetSpeed = 2;
    public bool animateHips = true;
    public Vector2 HipsRotMultiplier = new Vector2(2, 5);
    public float MaximumStepTime = 1.5f;

    public float ForwardMultiplier;
    public bool ShowWarnings = false;
    public bool bendToes = true;
    public Vector2 bendOnStep = new Vector2(30, 85);

    public bool AdaptiveForwardMultiplier = true;
    public Vector2 UDMultiplier;
    public float AMaxAngle;

    [SerializeField]
    public AnimationCurve RotationForWalking = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.796f, -0.367f), new Keyframe(1, 0));
    [SerializeField]
    public float RotationWMultiplier = 40;
    [SerializeField]
    public AnimationCurve RaiseFootWalking = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
    [SerializeField]
    public float RaiseFootWMultiplier = 0.045f;

    [SerializeField]
    public AnimationCurve RotationForRunning = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.32f, -0.35f), new Keyframe(0.625f, 0.557f), new Keyframe(1, 0));
    [SerializeField]
    public float RotationRMultiplier = 46;
    [SerializeField]
    public AnimationCurve RaiseFootRunning = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.43f, 1), new Keyframe(1, 0));
    [SerializeField]
    public float RaiseFootRMultiplier = 0.25f;

    public bool SmartStep = true;
    public float Scal = 0.65f;
    public float DistanceWeight = 1.4f;
    public int Qual = 30;
    
    // DEBUGGER
    public bool EnableDebugger = true;
    public bool ShowStepDistance = true;
    public float MoveSpeed;
    public bool ShowFootPath = true;
    public bool ShowSmartStepRadius = true;
    public bool ShowFootPlacement = true;

    private bool RecomputeLegPlacement = true;
    public bool UpdateHips = true;
    public float HipsMovementSpeed = 16;
    public float HipsUpDownMultiplier = 0.4f;

    public bool animateSpine = true;
    public Vector2 SpineMultiplier = new Vector2(1.5f, 10);
    public Vector2 LeanMultiplier = new Vector2(5, 20);
    public Vector2 Spine_Rotate_Amplitude = new Vector2(1.5f, 10);

    public int PredictQ = 31;

    public bool AlignHead = true;
    [Range(0f, 100f)]
    public float HeadPercentage = 92.5f;

    [Header("ArmSway")]
    public bool SwayArms = true;

    public Vector4 ShoulderValues = new Vector4(1, -2.5f, 5, -8);
    public Vector4 ArmValues = new Vector4(1.7f, -1, 30, -45);
    public Vector4 ForearmValues = new Vector4(16, 1, 130, 60);
    public Vector4 BendInward = new Vector4(5, -5, -45, -7);

    [Range(0f, 1f)] public float positionWeight = 1;
    [Range(0f, 1f)] public float rotationWeight = 1;
    [Range(0f, 1f)] public float HintWeight = 1;

    #region privateVariables

    float StepDIS;
    float UPH;
    float forwardMul;
    private bool StartedW;
    bool downG = false;
    bool StepSEnded = false;
    private Vector3 lastBestL;
    private Vector3 lastBestR;
    private Vector3 LastPos;
    private Vector2 movementDir;
    Vector2 lastDir;
    private float Speed;
    private float RotationSpeed;
    private Vector2 LeftPosOffset;
    private Vector2 RightPosOffset;
    private Vector2 D;
    float stepDistance;

    private Vector3 LeftLocOffset;
    private Vector3 RightLocOffset;

    private Vector3 lastHipPosL;
    private Vector3 lastHipPosR;

    private Vector3 ToeAnkleLeft;
    private Vector3 ToeAnkleRight;
    private float LeftRotateAngle;
    private float RightRotateAngle;

    private float AngleChangeReduction;

    private legTransform leftLeg;
    private legTransform rightLeg;

    private legTransform leftLegLast;
    private legTransform rightLegLast;

    private legTransform leftLegLastL;
    private legTransform rightLegLastL;

    private boxCastInfo leftBox;
    private boxCastInfo rightBox;
    float boosterL = 0;
    float boosterR = 0;
    bool reRead;

    Quaternion hipsRot = Quaternion.identity;
    float hipsA;
    float hipsYrot;
    Quaternion HipsQ;

    Transform leftFoot;
    Transform rightFoot;

    Transform leftToe;
    Transform rightToe;

    Transform leftKnee;
    Transform rightKnee;

    Transform leftUPleg;
    Transform rightUPleg;

    Quaternion leftToesQ;
    Quaternion rightToesQ;

    Transform LowerSpine;
    Transform MiddleSpine;
    Transform UpperSpine;

    Quaternion LowerSpineQ;
    Quaternion MiddleSpineQ;
    Quaternion UpperSpineQ;

    Transform LeftShoulder;
    Transform LeftArm;
    Transform LeftForearm;

    Quaternion LeftShoulderQ;
    Quaternion LeftArmQ;
    Quaternion LeftForearmQ;

    Transform RightShoulder;
    Transform RightArm;
    Transform RightForearm;

    Quaternion RightShoulderQ;
    Quaternion RightArmQ;
    Quaternion RightForearmQ;

    float Left_arm_value;
    float Right_arm_value;

    Transform Neck;
    Quaternion NeckQ;
    line DirLine;
    line LineSensor;

    private float TimeToStep;
    private float LTimeToStep;
    private float RTimeToStep;

    float LeftValue;
    float RightValue;

    float Lspeed;
    float Rspeed;

    float aditionalLRot;
    float aditionalRRot;
    float aditionalLR;
    float aditionalRR;
    float reduceXFootOffset = 0;

    legTransform leftTransform;
    legTransform rightTransform;

    legTransform leftToeCC;
    legTransform rightToeCC;

    private BoxCollider leftCollider;
    private BoxCollider rightCollider;

    Vector3 leftP;
    Vector3 rightP;

    private GameObject leftCollP;
    private GameObject rightCollP;

    Vector3 leftHint;
    Vector3 rightHint;

    float LbeforeDistance;
    float LafterDistance;
    float LargestRadius;

    float RbeforeDistance;
    float RafterDistance;
    float WalkFrequency;
    float Wanted_WalkFrequency;
    float SinceLastStep;
    bool steping = false;
    bool LeftLast;
    bool LeftClipped;
    bool RightClipped;
    bool ReComputeHeight;
    Vector3 leftFootDir;
    Vector3 rightFootDir;
    Vector3 d3MovementDir;

    private Quaternion lastRot;
    AnimationCurve leftCurve;
    AnimationCurve rightCurve;
    private bool leftToeGrounded;
    private bool rightToeGrounded;

    private float LwantedAlpha;
    private float RwantedAlpha;
    private float LAlpha;
    private float RAlpha;

    private float leftLegLenght;
    private float rightLegLenght;

    private float leftLift;
    private float rightLift;

    private float leftRot;
    private float rightRot;
    private float AngularMomentum;
    private Vector3 lastD;
    float hipsANG;

    private float La;
    private float Ra;
    bool canRetry = true;

    float leftFix;
    float rightFix;

    float LeftHLif;
    float RightHLif;
    float SlowSpeed;

    float totalDistanceL;
    float distanceHorizontalL;

    float totalDistanceR;
    float distanceHorizontalR;

    float leftHLiftT;
    float rightHLiftT;

    float multL;
    float multR;
    float LHeightMultiplier;
    float RHeightMultiplier;
    int countR = 0;

    float hipsH;
    Quaternion RotatedP;
    Vector3[] leftBoxVertices;
    Vector3[] rightBoxVertices;
    Quaternion fixHead = Quaternion.identity;

    bool refreshL;
    bool refreshR;

    float LStepMaxTime;
    float RStepMaxTime;

    bool Stopped = false;
    bool first = true;
    #endregion


    #region Unity_Functions

    private void OnEnable()
    {
        leftFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);
        CalculateBoxInformation(leftLeg.position, rightLeg.position);
        leftFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);
        ReComputeHeight = true;
        if (ShowWarnings)
        {
            if (anim != null)
            {
                if (anim.applyRootMotion)
                {
                    Debug.LogWarning("'Apply root motion' is enabled on the animator, this might cause jittering.");
                }
            }

            if (walkSpeed == RunSpeed)
            {
                Debug.LogWarning("Walk speed is equal to Run speed.");
            }

            if (walkSpeed == 0 || RunSpeed == 0)
            {
                Debug.LogError("Walk speed or Run speed is set to zero.");
                if (Application.isPlaying)
                {
                    Debug.Break();
                }
            }
        }

        if (Application.isPlaying)
        {
            Stopped = true;
            GetLegOffsets();
            GatherBasicData();
            TakeAStep(1);
            TakeAStep(2);
            RightValue = 1f;
            LeftValue = 1f;
            RecomputeLegPos();
            if (AvoidObstacles)
            {
                ComputeColision();
            }
            GetMovementDir(true);
            GetMovementLine();
            GetTime();
            GetRotationSpeed(true);

            leftLegLastL = leftLeg;
            leftLegLast = leftLeg;

            rightLegLastL = rightLeg;
            rightLegLast = rightLeg;
        }

        if (Scal < LargestRadius && ShowWarnings)
        {
            Debug.LogWarning("Scan area is too small, this will cause artifacts.");
        }
    }

    void Update()
    {
        if (Application.isPlaying && Time.timeScale != 0)
        {
            CalculateBoxInformation(leftLeg.position, rightLeg.position);
            GetMovementDir(true);
            GetLegOffsets();
            GetMovementLine();
            GetTime();
            GetRotationSpeed(true);
            ComputeAMomentum();
            ComputeColision();
            SettleFeet();
            GetFootSpeed();
            RecomputeLegPos();
            AdaptiveFM();
            ComputeLiftNRot();
            ComputeHints();

            SinceLastStep += Time.deltaTime;
            if (ReComputeHeight && false)
            {
                leftLeg = FireCast(leftLeg.position + Vector3.up * maxStepHeight, leftLeg.rotation, leftLegBounds, true);
                rightLeg = FireCast(rightLeg.position + Vector3.up * maxStepHeight, rightLeg.rotation, rightLegBounds, false);
            }

            if (ToWalk() && !Stopped)
            {
                StartedW = true;
                if (steping)
                {
                    TakeAStep(0, true);
                }
                else
                {
                    TakeAStep();
                }
            }
        }
        if (anim != null)
        {
            DrawLegBounds(leftFoot, rightFoot);
            leftFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
            rightFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);
        }
    } 

    private void FixedUpdate()
    {
        if (Physics.CheckSphere(leftLeg.position - leftLeg.rotation * ToeAnkleLeft + leftLeg.SurfaceNormal * FeetHeight, leftLegBounds.size.x / 2, mask))
        {
            leftToeGrounded = true;
        }
        else
        {
            leftToeGrounded = false;
        }

        if (Physics.CheckSphere(rightLeg.position - rightLeg.rotation * ToeAnkleRight + rightLeg.SurfaceNormal * FeetHeight, rightLegBounds.size.x / 2, mask))
        {
            rightToeGrounded = true;
        }
        else
        {
            rightToeGrounded = false;
        }
    }

    private void OnAnimatorIK()
    {
        if (Application.isPlaying)
        {
            anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, positionWeight);
            anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, positionWeight);
            anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, rotationWeight);
            anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, rotationWeight);

            anim.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, HintWeight);
            anim.SetIKHintPositionWeight(AvatarIKHint.RightKnee, HintWeight);

            anim.SetIKHintPosition(AvatarIKHint.LeftKnee, leftHint);
            anim.SetIKHintPosition(AvatarIKHint.RightKnee, rightHint);

            leftTransform = CalculateFootT(leftLegLastL, LeftValue, LeftRotateAngle + leftRot,
            ToeAnkleLeft, LafterDistance, leftCurve.Evaluate(LeftValue), 1, leftLift * LHeightMultiplier);

            leftToeCC = CalculateFootT(leftLegLastL, LeftValue, 0, new Vector3(), 0, 0, 0);

            rightTransform = CalculateFootT(rightLegLastL, RightValue, RightRotateAngle + rightRot,
            ToeAnkleRight, RafterDistance, rightCurve.Evaluate(RightValue), 2, rightLift * RHeightMultiplier);

            rightToeCC = CalculateFootT(rightLegLastL, RightValue, 0, new Vector3(), 0, 0, 0);

            leftTransform.rotation *= Quaternion.AngleAxis(LeftLegPlacement.YRotation, Vector3.up);
            rightTransform.rotation *= Quaternion.AngleAxis(RightLegPlacement.YRotation, Vector3.up);

            anim.SetIKPosition(AvatarIKGoal.LeftFoot, leftTransform.position);
            anim.SetIKPosition(AvatarIKGoal.RightFoot, rightTransform.position);

            anim.SetIKRotation(AvatarIKGoal.LeftFoot, leftTransform.rotation);
            anim.SetIKRotation(AvatarIKGoal.RightFoot, rightTransform.rotation);
        }

        leftToesQ = anim.GetBoneTransform(HumanBodyBones.LeftToes).localRotation;
        rightToesQ = anim.GetBoneTransform(HumanBodyBones.RightToes).localRotation;

        LowerSpineQ = anim.GetBoneTransform(HumanBodyBones.Spine).localRotation;
        MiddleSpineQ = anim.GetBoneTransform(HumanBodyBones.Chest).localRotation;
        UpperSpineQ = anim.GetBoneTransform(HumanBodyBones.UpperChest).localRotation;
        HipsQ = Quaternion.Euler(0, Hips.rotation.eulerAngles.y, 0);

        LeftShoulderQ = anim.GetBoneTransform(HumanBodyBones.LeftShoulder).localRotation;
        LeftArmQ = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm).localRotation;
        LeftForearmQ = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm).localRotation;

        RightShoulderQ = anim.GetBoneTransform(HumanBodyBones.RightShoulder).localRotation;
        RightArmQ = anim.GetBoneTransform(HumanBodyBones.RightUpperArm).localRotation;
        RightForearmQ = anim.GetBoneTransform(HumanBodyBones.RightLowerArm).localRotation;
        NeckQ = anim.GetBoneTransform(HumanBodyBones.Neck).localRotation;

        if (animateSpine)
        {
            AnimateSpine();
        }
        RotateHipsWhenSideSteping();
        MoveHips();
        if (SwayArms)
        {
            MoveArms();
        }
        if (AlignHead && animateSpine)
        {
            AlignH();
        }
        if (bendToes)
        {
            BendToes(leftToeCC, rightToeCC);
        }
        UpdateHipsHeight();

        anim.bodyRotation = HipsQ;
        anim.bodyPosition = new Vector3(anim.bodyPosition.x, hipsH, anim.bodyPosition.z);
        anim.SetBoneLocalRotation(HumanBodyBones.Spine, LowerSpineQ);
        anim.SetBoneLocalRotation(HumanBodyBones.Chest, MiddleSpineQ);
        anim.SetBoneLocalRotation(HumanBodyBones.UpperChest, UpperSpineQ);
        anim.SetBoneLocalRotation(HumanBodyBones.Neck, NeckQ);

        anim.SetBoneLocalRotation(HumanBodyBones.LeftShoulder, LeftShoulderQ);
        anim.SetBoneLocalRotation(HumanBodyBones.LeftUpperArm, LeftArmQ);
        anim.SetBoneLocalRotation(HumanBodyBones.LeftLowerArm, LeftForearmQ);

        anim.SetBoneLocalRotation(HumanBodyBones.RightShoulder, RightShoulderQ);
        anim.SetBoneLocalRotation(HumanBodyBones.RightUpperArm, RightArmQ);
        anim.SetBoneLocalRotation(HumanBodyBones.RightLowerArm, RightForearmQ);

        anim.SetBoneLocalRotation(HumanBodyBones.LeftToes, leftToesQ);
        anim.SetBoneLocalRotation(HumanBodyBones.RightToes, rightToesQ);
    }

    #endregion


    #region Final_Computations

    void GetFootSpeed()
    {
        float Tim = Mathf.Clamp(LTimeToStep, 0.01f, MaximumStepTime);

        Wanted_WalkFrequency = Mathf.Clamp(Wanted_WalkFrequency, 0.5f, 1 / (MaxStepDistance / RunSpeed));
        WalkFrequency = Mathf.Lerp(WalkFrequency, Wanted_WalkFrequency, Time.deltaTime * 10);

        LAlpha = Mathf.Lerp(LAlpha, LwantedAlpha, 15 * Time.deltaTime);
        RAlpha = Mathf.Lerp(RAlpha, RwantedAlpha, 15 * Time.deltaTime);
        multL = 1;

        if (SmoothSteppingOver && !Stopped)
        {
            if (LeftValue < 0.99f && distanceHorizontalL > 0)
            {
                multL = FindMultiplier(leftLegLast.position, leftLeg.position, LeftValue, leftCurve, 1, true);
            }
        }

        Lspeed = (1 - LeftValue) / Tim;
        Lspeed = Mathf.Clamp(Lspeed, 1, 100);
        if (Stopped)
        {
            Lspeed = settleFeetSpeed;
        }
        LeftValue += (Lspeed * Time.deltaTime) / Mathf.Clamp(multL, 0.01f, 100);
        LeftValue = Mathf.Clamp01(LeftValue);

        Tim = Mathf.Clamp(RTimeToStep, 0.01f, MaximumStepTime);

        multR = 1;
        if (SmoothSteppingOver && !Stopped)
        {
            if (RightValue < 0.99f && distanceHorizontalR > 0)
            {
                multR = FindMultiplier(rightLegLast.position, rightLeg.position, RightValue, rightCurve, 1, false);
            }
        }
        Rspeed = (1 - RightValue) / Tim;
        Rspeed = Mathf.Clamp(Rspeed, 1, 100);
        if (Stopped)
        {
            Rspeed = settleFeetSpeed;
        }
        RightValue += (Rspeed * Time.deltaTime) / Mathf.Clamp(multR, 0.01f, 100);
        RightValue = Mathf.Clamp01(RightValue);
    }

    void ComputeLiftNRot()
    {
        float RunWalk;
        RunWalk = Mathf.Clamp01((Speed - walkSpeed) / (RunSpeed - walkSpeed));

        LHeightMultiplier = Mathf.Clamp01((Vector2.Distance(v3To2(leftLeg.position), v3To2(leftLegLast.position)) / (walkSpeed * StepDistanceMultiplier)) * 2);
        RHeightMultiplier = Mathf.Clamp01((Vector2.Distance(v3To2(rightLeg.position), v3To2(rightLegLast.position)) / (walkSpeed * StepDistanceMultiplier)) * 2);
        //raise
        leftLift = RaiseFootWalking.Evaluate(LeftValue) * RaiseFootWMultiplier;
        leftLift = Mathf.Lerp(leftLift, RaiseFootRunning.Evaluate(LeftValue) * RaiseFootRMultiplier, RunWalk);

        rightLift = RaiseFootWalking.Evaluate(RightValue) * RaiseFootWMultiplier;
        rightLift = Mathf.Lerp(rightLift, RaiseFootRunning.Evaluate(RightValue) * RaiseFootRMultiplier, RunWalk);

        if (Mathf.Abs(AngularMomentum) > 0 && RunWalk < 0)
        {
            leftLift = RaiseFootWalking.Evaluate(LeftValue) * RaiseFootWMultiplier;
            rightLift = RaiseFootWalking.Evaluate(RightValue) * RaiseFootWMultiplier;
        }

        //rotate
        float multip = Mathf.Clamp01(Vector2.Dot(v3To2(ParentObject.forward).normalized, movementDir.normalized));

        leftRot = RotationForWalking.Evaluate(LeftValue) * RotationWMultiplier;
        leftRot = Mathf.Lerp(leftRot, RotationForRunning.Evaluate(LeftValue) * RotationRMultiplier, RunWalk) * multip;
        leftRot *= Mathf.Clamp01(Speed / walkSpeed);

        rightRot = RotationForWalking.Evaluate(RightValue) * RotationWMultiplier;
        rightRot = Mathf.Lerp(rightRot, RotationForRunning.Evaluate(RightValue) * RotationRMultiplier, RunWalk) * multip;
        rightRot *= Mathf.Clamp01(Speed / walkSpeed);
    }

    legTransform CalculateFootT(legTransform last, float value, float RotationAngle = 0, Vector3 toeAnkle = new Vector3(), float afterD = 0, float ObstacleRaise = 0, int leftF = 0, float raiseFoot = 0)
    {
        legTransform T = new legTransform();

        Vector3 Lposition = last.position + last.SurfaceNormal * FeetHeight;
        T.position = Lposition;
        T.position.y = Mathf.Lerp(Lposition.y, Lposition.y, value + afterD * Mathf.Clamp01(value * 3));
        T.rotation = last.rotation;

        T.position += Vector3.up * (raiseFoot + ObstacleRaise);


        if (leftF == 1)
        {
            T.position = ClampedPosition(T.position, leftUPleg.position, leftLeg.position, leftLegLenght, LAlpha, true, 0.94f);
        }
        else if (leftF == 2)
        {
            T.position = ClampedPosition(T.position, rightUPleg.position, rightLeg.position, rightLegLenght, RAlpha, false, 0.94f);
        }


        if (bendToes)
        {
            T.rotation *= Quaternion.AngleAxis(-RotationAngle, Vector3.right);
        }

        float b = T.position.y;
        if (bendToes)
        {
            T.position = RotateAroundV(T.position, T.position + T.rotation * toeAnkle, RotationAngle, T.rotation * Vector3.right);
        }

        if (leftF == 1)
        {
            LeftHLif = b - T.position.y;
            if (b - T.position.y != 0) 
            {
                //Debug.Log(b - T.position.y);
            }
        }
        else if (leftF == 2)
        {
            RightHLif = b - T.position.y;
        }


        if (value < 0.99999f)
        {
            float v = 1 - Mathf.Abs(value - 0.5f) * 2;
            if (leftF == 1)
            {
                leftFix = v * Mathf.Clamp01((1 - reduceXFootOffset) - 0.2f);
                T.position += RotatedP * Vector3.right * LeftLegPlacement.positionX * leftFix;
            }
            else if (leftF == 2)
            {
                rightFix = v * Mathf.Clamp01((1 - reduceXFootOffset) - 0.2f);
                T.position += RotatedP * Vector3.right * -RightLegPlacement.positionX * rightFix;
            }
        }

        return T;
    }

    Vector3 ClampedPosition(Vector3 CurrentPos, Vector3 startPos, Vector3 FinalPos, float LegLenght, float Lerp, bool L, float percentage_ToPull = 0.9f)
    {
        Vector3 returnPos = CurrentPos;

        float Distance = Mathf.Max(startPos.y - (FinalPos.y + FeetHeight), 0);

        Distance -= LegLenght * 1.2f;
        Distance = Mathf.Max(Distance, 0);
        if (Distance >= LegLenght)
        {
            if (L)
            {
                LwantedAlpha = 0;
                LAlpha = 0;
            }
            else
            {
                RwantedAlpha = 0;
                RAlpha = 0;
            }

        }
        else
        {
            if (L)
            {
                LwantedAlpha = 1;
            }
            else
            {
                RwantedAlpha = 1;
            }
        }

        if (Distance >= LegLenght || Lerp < 0.93f)
        {
            Vector3 dir = CurrentPos - startPos;
            dir.Normalize();

            returnPos = startPos + dir * (Mathf.Min(LegLenght, Distance) * percentage_ToPull);
            returnPos = Vector3.Lerp(returnPos, CurrentPos, Lerp);
        }

        return returnPos;
    }

    private void ComputeHints()
    {
        Quaternion leftRot = Quaternion.Lerp(ParentObject.rotation, leftTransform.rotation, 0.5f);
        float leftHeight = Mathf.Abs(leftTransform.position.y - Hips.position.y);
        Vector3 forward = SetY0(leftRot * Vector3.forward).normalized;
        Vector3 right = SetY0(leftRot * Vector3.right).normalized;
        Vector3 HIPL = SetY0(Hips.position) + Vector3.up * leftTransform.position.y;
        Vector3 leftL = GetClosestPointOnInfiniteLine(leftTransform.position - leftLegLastL.SurfaceNormal * FeetHeight, HIPL, HIPL + forward);

        Vector3 v1 = (Hips.position - leftL).normalized;
        v1 = RotateVectorAroundAxis(v1, right, 110);
        v1 = Vector3.Lerp(v1, forward, 0.5f);
        leftHint = leftTransform.position + leftHeight * Vector3.up + v1 * 10;

        Quaternion rightRot = Quaternion.Lerp(ParentObject.rotation, rightTransform.rotation, 0.5f);
        float rightHeight = Mathf.Abs(rightTransform.position.y - Hips.position.y);
        forward = SetY0(rightRot * Vector3.forward).normalized;
        right = SetY0(rightRot * Vector3.right).normalized;
        Vector3 HIPR = SetY0(Hips.position) + Vector3.up * rightTransform.position.y;
        Vector3 rightL = GetClosestPointOnInfiniteLine(rightTransform.position - rightLegLastL.SurfaceNormal * FeetHeight, HIPR, HIPR + forward);

        v1 = (Hips.position - rightL).normalized;
        v1 = RotateVectorAroundAxis(v1, right, 110);
        v1 = Vector3.Lerp(v1, forward, 0.6f);
        rightHint = rightTransform.position + rightHeight * Vector3.up + v1 * 10;
    }

    #endregion


    #region Walk_Logic

    void AdaptiveFM()
    {
        if (AdaptiveForwardMultiplier)
        {
            float dot = Vector2.Dot(movementDir, v3To2(ParentObject.forward).normalized);

            Vector3 AVGUp = ((leftLeg.SurfaceNormal + rightLeg.SurfaceNormal) / 2).normalized;
            bool Uphil = Vector3.Angle(v2T3(movementDir).normalized, AVGUp) > 90;
            UPH = Vector3.Angle(AVGUp, Vector3.up);
            UPH = Mathf.Clamp01(UPH / AMaxAngle);
            UPH *= (Convert.ToInt32(Uphil) * 2) - 1;
            float FM = Mathf.Lerp(ForwardMultiplier, UDMultiplier.x, Mathf.Clamp01(UPH));
            FM = Mathf.Lerp(FM, UDMultiplier.y, Mathf.Clamp01(-UPH));
            forwardMul = Mathf.Lerp(forwardMul, FM * dot, Time.deltaTime * 4);
        }
        else
        {
            forwardMul = Mathf.Lerp(forwardMul,  ForwardMultiplier, Time.deltaTime * 4);
        }
    }

    void GetTime()
    {
        float StepD;
        Vector2 movD;
        if (movementDir != Vector2.zero)
        {
            movD = movementDir.normalized;
        }
        else
        {
            movD = v3To2(Hips.forward);
        }
        ComputeMovementLine(movD, Hips.position);
        Vector2 left = v3To2(leftLeg.position - LeftLocOffset);
        Vector2 right = v3To2(rightLeg.position - RightLocOffset);
        Vector2 CenterPosition = (left + right) / 2;
        float CenterDot = Vector2.Dot(movD, (CenterPosition - v3To2(Hips.position)).normalized);

        float CenterDistance = PointDistanceToLine(LineSensor, CenterPosition) * (CenterDot / Math.Abs(CenterDot));
        float Trigger = stepDistance * forwardMul;
        StepD = CenterDistance - Trigger;

        Vector3 leftDir = SetY0(leftLeg.rotation * Quaternion.AngleAxis(-La, Vector3.up) * Quaternion.AngleAxis(-hipsANG, Vector3.up) * Vector3.forward).normalized;
        Vector3 rightDir = SetY0(rightLeg.rotation * Quaternion.AngleAxis(-Ra, Vector3.up) * Quaternion.AngleAxis(-hipsANG, Vector3.up) * Vector3.forward).normalized;
        Vector3 parentDir = SetY0(ParentObject.rotation * Vector3.forward).normalized;

        float maxRot = Vector3.Angle(parentDir, leftDir) + Vector3.Angle(parentDir, rightDir);
        float rotTime = Mathf.Abs(RotationError - maxRot) / (RotationSpeed * 2);;

        if (Speed > walkSpeed * 0.8f)
        {
            rotTime = Mathf.Infinity;
        }
        if (reRead)
        {
            SinceLastStep = StepD / Speed;
            reRead = false;
        }
        float DS = StepD / Speed;
        if (Speed <= walkSpeed / 20)
        {
            DS = MaximumStepTime / 2;
        }
        TimeToStep = Math.Max(Mathf.Min(DS, rotTime), 0.0001f);
        if (LeftLast)
        {
            LTimeToStep = TimeToStep;
        }
        else
        {
            RTimeToStep = TimeToStep;
        }

        if (refreshL)
        {
            LStepMaxTime = LTimeToStep;
            refreshL = false;
        }

        if (refreshR)
        {
            RStepMaxTime = RTimeToStep;
            refreshR = false;
        }
    }

    bool ToWalk()
    {
        float RunWalk;
        RunWalk = Mathf.Clamp01((Speed - walkSpeed) / (RunSpeed - walkSpeed));

        float Dot = Mathf.Max(Vector2.Dot(movementDir.normalized, v3To2(SetY0(ParentObject.forward).normalized)), Vector2.Dot(movementDir.normalized, v3To2(SetY0(-ParentObject.forward).normalized)), 0.7f);
        Dot = Mathf.Clamp01(Dot);
        float Sp;
        Sp = Mathf.Clamp(Speed, 0, RunSpeed);

        float multipiers = Sp * StepDistanceMultiplier;
        float angleC = Mathf.Clamp(Mathf.Pow(AngleChangeReduction, 3), Mathf.Lerp(0.1f, 0.45f, RunWalk), 1);
        StepDIS = Mathf.Clamp(multipiers, 0, Mathf.Min(MaxStepDistance, MaxStepDistance)) * Dot * angleC;

        bool Step = false;
        Vector2 movD;
        if (movementDir != Vector2.zero)
        {
            movD = movementDir.normalized;
        }
        else
        {
            movD = v3To2(Hips.forward);
        }
        ComputeMovementLine(movD, Hips.position);
        Vector2 left = v3To2(leftLeg.position - LeftLocOffset);
        Vector2 right = v3To2(rightLeg.position - RightLocOffset);
        Vector2 CenterPosition = (left + right) / 2;
        float CenterDot = Vector2.Dot(movD, (CenterPosition - v3To2(Hips.position)).normalized);

        float CenterDistance = PointDistanceToLine(LineSensor, CenterPosition) * (CenterDot / Math.Abs(CenterDot));
        float Trigger = stepDistance * forwardMul;

        if (CenterDistance <= Trigger && StepDIS > MinStepDistance)
        {
            Step = true;
        }

        Vector3 leftDir = SetY0(leftLeg.rotation * Quaternion.AngleAxis(-La, Vector3.up) * Quaternion.AngleAxis(-hipsANG, Vector3.up) * Vector3.forward).normalized;
        Vector3 rightDir = SetY0(rightLeg.rotation * Quaternion.AngleAxis(-Ra, Vector3.up) * Quaternion.AngleAxis(-hipsANG, Vector3.up) * Vector3.forward).normalized;
        Vector3 parentDir = SetY0(ParentObject.rotation * Vector3.forward).normalized;

        float maxRot = Vector3.Angle(parentDir, leftDir) + Vector3.Angle(parentDir, rightDir);
        bool rot = maxRot > RotationError;

        Step &= Speed > walkSpeed / 20;
        steping = Step;
        return Step || rot;
    }

    int GetLegToMove(legTransform leftLegPosition, legTransform rightLegPosition)
    {
        int Vukan;
        Vector2 LF = v3To2(leftLegPosition.position - LeftLocOffset);
        Vector2 RF = v3To2(rightLegPosition.position - RightLocOffset);

        float dot1 = Vector2.Dot((v3To2(Hips.position) - LF).normalized, movementDir);
        float dot2 = Vector2.Dot((v3To2(Hips.position) - RF).normalized, movementDir);

        float d1 = Vector2.Distance(LF, v3To2(Hips.position)) * dot1;
        float d2 = Vector2.Distance(RF, v3To2(Hips.position)) * dot2;

        float r1 = Mathf.Abs(ParentObject.rotation.eulerAngles.y - leftLegPosition.rotation.eulerAngles.y);
        float r2 = Mathf.Abs(ParentObject.rotation.eulerAngles.y - rightLegPosition.rotation.eulerAngles.y);

        if (Mathf.Round(d1 * 1000) != Mathf.Round(d2 * 1000))
        {
            if (d1 > d2)
            {
                Vukan = 0;
            }
            else
            {
                Vukan = 1;
            }
        }
        else
        {
            if (r1 > r2)
            {
                Vukan = 0;
            }
            else
            {
                Vukan = 1;
            }
        }
        return Vukan;
    }

    void SettleFeet()
    {
        float MinDistance = Mathf.Max(Vector2.Distance(v3To2(leftLeg.position), v3To2(leftLegLastL.position)), Vector2.Distance(v3To2(rightLeg.position), v3To2(rightLegLastL.position)));

        if (Speed < walkSpeed / 20 && RotationSpeed < 0.01f && !Stopped && LeftValue == 1 && RightValue == 1 && canRetry && StepSEnded && MinDistance < 0.001f)
        {
            canRetry = false;
            Stopped = true;
            StartCoroutine(ToDefaultState());
        }
        else if (Speed > walkSpeed / 20 || RotationSpeed > 0.01f)
        {
            Stopped = false;
            ReComputeHeight = false;
        }
    }

    IEnumerator ToDefaultState()
    {
        aditionalLRot = 0;
        aditionalRRot = 0;
        Speed = 0;
        yield return new WaitForSeconds(2 / settleFeetSpeed);
        if (!Stopped && !StepSEnded)
        {
            canRetry = true;
            yield break;
        }
        if (Vector2.Distance(v3To2(rightLeg.position - RightLocOffset), v3To2(Hips.position)) > 0.001f)
        {
            TakeAStep(2);
        }

        yield return new WaitForSeconds(2 / settleFeetSpeed);
        if (!Stopped && !StepSEnded)
        {
            canRetry = true;
            yield break;
        }
        canRetry = true;
        if (Vector2.Distance(v3To2(leftLeg.position - LeftLocOffset), v3To2(Hips.position)) > 0.001f)
        {
            TakeAStep(1);
        }
        yield return new WaitForSeconds(2 / settleFeetSpeed);
        if (!Stopped && !StepSEnded)
        {
            canRetry = true;
            yield break;
        }
        ReComputeHeight = true;
    }

    #endregion


    #region Set_Foot_Position

    Vector2 PredictStepPos(Vector2 movementDir, float Sp, Vector3 HipsPos, bool Capture = false, bool left = false, bool Rec = false)
    {
        float RunWalk;
        RunWalk = Mathf.Clamp01((Speed - walkSpeed) / (RunSpeed - walkSpeed));

        float Rot;
        float max = RotationError / 2;
        if (left)
        {
            Rot = Mathf.Clamp(-aditionalLR, -max, max);
        }
        else
        {
            Rot = Mathf.Clamp(aditionalRR, -max, max);
        }

        float Dot = Mathf.Max(Vector2.Dot(movementDir.normalized, v3To2(SetY0(ParentObject.forward).normalized)), Vector2.Dot(movementDir.normalized, v3To2(SetY0(-ParentObject.forward).normalized)), 0.7f);
        Dot = Mathf.Clamp01(Dot);
        Sp = Mathf.Clamp(Sp, 0, RunSpeed);

        float multipiers = Sp * StepDistanceMultiplier;
        Vector2 stepPos;
        float angleC = Mathf.Clamp(Mathf.Pow(AngleChangeReduction, 5), Mathf.Lerp(0, 0.6f, RunWalk), 1);
        float STP = Mathf.Clamp(multipiers, 0, Mathf.Min(MaxStepDistance, stepDistance + MaxStepDistance * 0.35f)) * Dot;
        if (Capture || STP < stepDistance)
        {
            stepDistance = STP;
        }
        stepDistance = Mathf.Clamp(stepDistance, 0, MaxStepDistance);
        stepPos = v3To2(SetY0(HipsPos) + (Quaternion.AngleAxis(Rot, Vector3.up) * v2T3(movementDir.normalized)) * stepDistance * angleC);
        stepPos = MaxDistance(v2T3(stepPos), left, HipsPos);

        return stepPos;
    }

    Vector2 MaxDistance(Vector3 predictP, bool left, Vector3 hipsPos)
    {
        RaycastHit hit;
        Vector2 landPos = Vector2.zero;

        if (left)
        {
            float M;
            float y;
            M = rightLeg.position.y;
            y = M + leftLegLenght + maxStepHeight;

            boxCastInfo cast;
            Vector3 dir = (SetY0(leftLeg.rotation * Vector3.forward)).normalized;
            bool low = false;
            for (int i = 0; i < PredictQ; i++)
            {
                float maximum = Mathf.Max(Vector3.Distance(SetY0(hipsPos), SetY0(predictP + LeftLocOffset)) / PredictQ, leftLegBounds.size.z);

                cast.Center = SetY0(Vector3.Lerp(hipsPos, predictP + LeftLocOffset, (float)i / (float)PredictQ)) + y * Vector3.up;
                cast.Size = new Vector3(leftLegBounds.size.x, 0.1f, maximum);
                cast.Orientation = Quaternion.LookRotation(dir, Vector3.up);

                cast.Direction = Vector3.down;
                cast.Center += SetY0(cast.Orientation * Vector3.right * leftLegBounds.ShiftCenterX * leftLegBounds.size.x
                + cast.Orientation * Vector3.up * leftLegBounds.ShiftCenterY * leftLegBounds.size.y
                + cast.Orientation * Vector3.forward * leftLegBounds.ShiftCenterZ * leftLegBounds.size.z);

                Physics.Raycast(cast.Center, cast.Direction, out hit, Mathf.Infinity, mask);

                if (rightLeg.position.y - hit.point.y <= -maxStepHeight * 0.9f)
                {
                    i = PredictQ - 1;
                    landPos = v3To2(cast.Center);
                }
                if (i == PredictQ - 1 || rightLeg.position.y - hit.point.y >= maxStepHeight * 0.9f && !low)
                {
                    landPos = v3To2(cast.Center);
                    low = true;
                }
            }
        }
        else
        {
            float M;
            float y;
            M = leftLeg.position.y;
            y = M + leftLegLenght + maxStepHeight;

            boxCastInfo cast;
            Vector3 dir = (SetY0(rightLeg.rotation * Vector3.forward)).normalized;
            bool low = false;
            for (int i = 0; i < PredictQ; i++)
            {
                float maximum = Mathf.Max(Vector3.Distance(SetY0(hipsPos), SetY0(predictP + RightLocOffset)) / PredictQ, rightLegBounds.size.z);

                cast.Center = SetY0(Vector3.Lerp(hipsPos, predictP + RightLocOffset, (float)i / (float)PredictQ)) + y * Vector3.up;
                cast.Size = new Vector3(rightLegBounds.size.x, 0.1f, maximum);
                cast.Orientation = Quaternion.LookRotation(dir, Vector3.up);

                cast.Direction = Vector3.down;
                cast.Center += SetY0(cast.Orientation * Vector3.right * rightLegBounds.ShiftCenterX * rightLegBounds.size.x
                + cast.Orientation * Vector3.up * rightLegBounds.ShiftCenterY * rightLegBounds.size.y
                + cast.Orientation * Vector3.forward * rightLegBounds.ShiftCenterZ * rightLegBounds.size.z);

                Physics.Raycast(cast.Center, cast.Direction, out hit, Mathf.Infinity, mask);

                if (leftLeg.position.y - hit.point.y <= -maxStepHeight * 0.9f)
                {
                    i = PredictQ - 1;
                    landPos = v3To2(cast.Center);
                }
                if (i == PredictQ - 1 || leftLeg.position.y - hit.point.y >= maxStepHeight * 0.9f && !low)
                {
                    landPos = v3To2(cast.Center);
                    low = true;
                }
            }
        }
        return FindNearestPointOnLine(v3To2(hipsPos), movementDir, landPos);
    }

    Vector3 BestPosition(Vector3 pos, Vector3 CurrentHeight, Quaternion rotation, float scale, int Quality, float MaxH, bool left, Vector3 last_Point = new Vector3(), float stepV = 0, bool compute = false)
    {
        int x, y;
        x = Quality;
        y = Quality / 7;

        int Qsq = x * y;

        Vector3[] positions = new Vector3[Qsq];
        Vector3[] positions1 = new Vector3[Qsq];
        Vector3[] normals = new Vector3[Qsq];
        float[] Heights = new float[Qsq];
        float[] places = new float[Qsq];
        int check_distnce = (int)(Quality * 0.2f);
        int check_Interval = 1; // 1 for the most precise results, higher numbers give less acurate results but faster execution

        for (int i = 0; i < x; i++)
        {
            for (int s = 0; s < y; s++)
            {
                positions[i + s * x] = pos
                    - rotation * Vector3.forward * (((float)(y - 1) / 2f - s) / (float)y) * scale / 7f
                    - rotation * Vector3.right * (((float)(x - 1) / 2f - i) / (float)x) * scale;
            }
        }

        RaycastHit hit;
        for (int i = 0; i < Qsq; i++)
        {
            positions1[i] = positions[i];
            boxCastInfo castInfo = new boxCastInfo();
            castInfo.Center = positions[i];
            castInfo.Orientation = Quaternion.LookRotation(rotation * Vector3.forward, Vector3.up);
            castInfo.Direction = Vector3.down;
            if (Physics.Raycast(castInfo.Center, castInfo.Direction, out hit, Mathf.Infinity, mask))
            {

            }
            else
            {
                hit.point = Vector3.up * - 10000000;
            }
            
            float h = pos.y - hit.point.y;
            Heights[i] = h;
            positions[i] = new Vector3(castInfo.Center.x, hit.point.y, castInfo.Center.z);
            normals[i] = hit.normal;
        }
        int best = 0;
        float WH = -10000;

        for (int i = 0; i < Qsq; i++)
        {
            #region Height

            float heightAdd;
            Vector3 UpdatedY = positions[i];
            float multiply = MathF.Max(0, 30 - Vector3.Angle(normals[i], Vector3.up)) / 30;

            if (Mathf.Abs(CurrentHeight.y - positions[i].y) < MaxH)
            {
                heightAdd = ((UpdatedY.y - CurrentHeight.y) / MaxH) * multiply;
            }
            else if ((CurrentHeight.y - positions[i].y) > MaxH)
            {
                heightAdd = ((UpdatedY.y - CurrentHeight.y) / MaxH) * multiply;
                heightAdd -= 100;
            }
            else
            {
                heightAdd = -((UpdatedY.y - CurrentHeight.y) / MaxH) * multiply;
                heightAdd -= 100;
            }

            float percentage = MaxH * 0.05f;
            #endregion

            #region Check_For_Sides

            int v = i / x;
            int c = i % x;
            int currentElement;

            for (int l = -check_distnce; l <= check_distnce; l += check_Interval)
            {
                for (int g = -check_distnce; g <= check_distnce; g += check_Interval)
                {
                    if (betwenNumbers(v + l, 0, y - 1) && betwenNumbers(c + g, 0, x - 1))
                    {
                        currentElement = (v + l) * y + c + g;

                        if (Mathf.Abs(positions[currentElement].y - positions[i].y) < percentage)
                        {
                            heightAdd += 0.1f / check_distnce;
                        }
                    }
                }
            }
            #endregion

            places[i] = -((Vector2.Distance(v3To2(pos), v3To2(positions1[i])) / scale) * DistanceWeight) + heightAdd;
            if (compute && StartedW)
            {
                places[i] -= Vector2.Distance(v3To2(last_Point), v3To2(positions[i])) * Mathf.Pow(stepV, 3) * 14;
            }

            if (places[i] > WH)
            {
                WH = places[i];
                best = i;
            }
        }
        return positions1[best];
    }

    legTransform FireCast(Vector3 legPos, Quaternion legRot, LegBounds bounds, bool left)
    {
        RaycastHit hit;
        legTransform legT = new legTransform();

        //Calculate information for the boxcast

        Vector3 dir = (SetY0(legRot * Vector3.forward)).normalized;

        boxCastInfo cast = new boxCastInfo();
        float Hpos;
        if (left)
        {
            Hpos = Mathf.Max(rightLeg.position.y + FeetHeight + rightHLiftT, LowestPointOfCharacter.position.y);
        }
        else
        {
            Hpos = Mathf.Max(leftLeg.position.y + FeetHeight + leftHLiftT, LowestPointOfCharacter.position.y);
        }

        cast.Center = SetY0(legPos) + (maxStepHeight + Hpos + maxStepHeight * 1.5f) * Vector3.up;
        cast.Size = new Vector3(bounds.size.x, bounds.size.z, 0.1f);
        cast.Orientation = Quaternion.LookRotation(Vector3.down, dir);

        if (!left)
        {
            cast.Direction = Quaternion.LookRotation(Vector3.down, dir) * Vector3.forward;
            cast.Center += cast.Orientation * Vector3.right * bounds.ShiftCenterX * bounds.size.x
            + cast.Orientation * Vector3.up * bounds.ShiftCenterY * -bounds.size.y
            + cast.Orientation * Vector3.forward * bounds.ShiftCenterZ * bounds.size.z;
        }
        else
        {
            cast.Direction = Quaternion.LookRotation(Vector3.down, dir) * Vector3.forward;
            cast.Center += cast.Orientation * Vector3.right * bounds.ShiftCenterX * bounds.size.x
            + cast.Orientation * Vector3.up * bounds.ShiftCenterY * bounds.size.y
            + cast.Orientation * Vector3.forward * bounds.ShiftCenterZ * bounds.size.z;
        }

        if (Physics.BoxCast(cast.Center, cast.Size / 2, cast.Direction, out hit, cast.Orientation, Mathf.Infinity, mask))
        {
            //DrawBoxCastBox(cast, Mathf.Abs(hit.point.y - cast.Center.y), Color.red, 0.1f);
        }
        else
        {
            Physics.BoxCast(cast.Center + Vector3.up * (leftLegLenght + Mathf.Abs(leftLeg.position.y - rightLeg.position.y)), cast.Size / 2, cast.Direction, out hit, cast.Orientation, Mathf.Infinity, mask);
        }
        Vector3 slopeCorrected = Vector3.Cross(hit.normal, -Vector3.right);
        Quaternion footRotation = Quaternion.LookRotation(slopeCorrected, hit.normal);
        float a = Quaternion.Angle(Quaternion.identity, footRotation);

        legT.rotation = Quaternion.Lerp(Quaternion.identity, footRotation, MaxFeetAngle / a);
        legT.rotation *= Quaternion.AngleAxis(hipsANG, Vector3.up);

        legT.rotation *= Quaternion.AngleAxis(ParentObject.rotation.eulerAngles.y, Vector3.up);

        if (left)
        {
            La = Mathf.Clamp(-aditionalLRot / 3, -RotationError / 1.4f, 0);
            legT.rotation *= Quaternion.AngleAxis(La, Vector3.up);
        }
        else
        {
            Ra = Mathf.Clamp(aditionalRRot / 3, 0, RotationError / 1.4f);
            legT.rotation *= Quaternion.AngleAxis(Ra, Vector3.up);
        }
        //Compute leg height

        legPos = new Vector3(legPos.x, hit.point.y, legPos.z);
        Ray ray = new Ray(legPos, Vector3.down);
        Plane plane = new Plane(Vector3.Lerp(Vector3.up, hit.normal, MaxFeetAngle / Vector3.Angle(Vector3.up, hit.normal)), hit.point);
        Vector3 hitPoint = Vector3.zero;

        if (v3To2(hit.normal).magnitude < 0.05f)
        {
            hitPoint = new Vector3(legPos.x, hit.point.y, legPos.z);
        }
        float enter;

        if (plane.Raycast(ray, out enter))
        {
            hitPoint = ray.GetPoint(enter);
        }

        if (Mathf.Abs(hitPoint.y - hit.point.y) < Mathf.Max(leftLegBounds.size.y, rightLegBounds.size.y))
        {
            legT.position = hitPoint;
        }
        else
        {
            legT.position = hit.point;
        }
        legT.SurfaceNormal = hit.normal;

        RaycastHit HHHT;
        bool test = false;
        Vector3 offset;
        Vector3 HPosW = SetY0(cast.Center) + Vector3.up * hit.point.y; ;
        if (left)
        {
            offset = SetY0(legT.rotation * Vector3.forward + Vector3.up * 0.1f).normalized;
            if (Physics.Raycast(HPosW - offset * (leftLegBounds.size.z / 2) + Vector3.up * 0.1f, offset.normalized, out HHHT, leftLegBounds.size.z * 0.99f, mask))
            {
                test = true;
            }
        }
        else
        {
            offset = SetY0(legT.rotation * Vector3.forward + Vector3.up * 0.1f).normalized;
            if (Physics.Raycast(HPosW - offset * (rightLegBounds.size.z / 2) + Vector3.up * 0.1f, offset.normalized, out HHHT, rightLegBounds.size.z * 0.99f, mask))
            {
                test = true;
            }

        }

        if (Vector3.Angle(HHHT.normal, Vector3.up) >= 70 && test)
        {
            legT.position.y = Mathf.Min(leftLeg.position.y, rightLeg.position.y);
            if (left)
            {
                LeftClipped = true;
            }
            else
            {
                RightClipped = true;
            }
        }
        return legT;
    }

    void TakeAStep(int g = 0, bool measureFrequency = false)
    {
        Vector3 predict_Pos;
        LeftClipped = false;
        RightClipped = false;
        if (LeftLast)
        {
            if (leftLeg.position.y < rightLeg.position.y)
                countR++;
            else
                countR = 0;
        }
        else
        {
            if (leftLeg.position.y > rightLeg.position.y)
                countR++;
            else
                countR = 0;
        }
        countR = Mathf.Min(countR, 1);

        if (countR == 1)
        {
            downG = true;
        }
        else
        {
            downG = false;
        }

        if (measureFrequency)
        {
            reRead = true;
            Wanted_WalkFrequency = 1 / SinceLastStep;
        }

        if (GetLegToMove(leftLeg, rightLeg) == 0 && g == 0 || g == 1)
        {
            refreshL = true;
            predict_Pos = v2T3(PredictStepPos(movementDir, Speed, Hips.position, true, true));
            LeftLast = true;
            lastHipPosL = Hips.position;
            LeftValue = 0;
            Vector3 legP = predict_Pos + LeftLocOffset;

            leftLegLast = leftLeg;
            Vector3 p = legP + Vector3.up * (maxStepHeight + leftLegLenght);

            if (SmartStep)
            {
                float y = Mathf.Max(leftLeg.position.y, rightLeg.position.y);
                p = BestPosition(p + leftLeg.position.y * Vector3.up, y * Vector3.up, Quaternion.Euler(0, leftLeg.rotation.eulerAngles.y, 0), Scal, Qual, maxStepHeight, true);
            }
            lastBestL = p;

            leftLeg = FireCast(p, leftLeg.rotation, leftLegBounds, true);
            //leftLegLastL = leftLegLast;
            if (measureFrequency && SmoothSteppingOver)
            {
                FindDistances(leftLeg.position, leftLegLast.position, 1, SmoothingQuality, leftCurve, true);
            }
        }
        else
        {
            refreshR = true;
            predict_Pos = v2T3(PredictStepPos(movementDir, Speed, Hips.position, true, false));
            LeftLast = false;
            lastHipPosR = Hips.position;
            RightValue = 0;
            rightLegLast = rightLeg;
            Vector3 legP = predict_Pos + RightLocOffset;
            Vector3 p = legP + Vector3.up * (maxStepHeight + leftLegLenght);

            if (SmartStep)
            {
                float y = Mathf.Max(leftLeg.position.y, rightLeg.position.y);
                p = BestPosition(p + rightLeg.position.y * Vector3.up, y * Vector3.up, Quaternion.Euler(0, rightLeg.rotation.eulerAngles.y, 0), Scal, Qual, maxStepHeight, false);
            }
            lastBestR = p;

            rightLeg = FireCast(p, rightLeg.rotation, rightLegBounds, false);
            //rightLegLastL = rightLegLast;
            if (measureFrequency && SmoothSteppingOver)
            {
                FindDistances(rightLeg.position, rightLegLast.position, 2, SmoothingQuality, rightCurve, false);
            }
        }
    }

    void LerpAdaptivePosition()
    {
        if (LeftValue > 0.999f && RightValue > 0.999f)
        {
            StepSEnded = true;
        }
        else
            StepSEnded = false;

        float TT;
        if (!Stopped)
        {
            if (Speed <= walkSpeed / 20 && RotationSpeed <= RotationError / 50)
            {
                LStepMaxTime -= Time.deltaTime;
                TT = (Time.deltaTime / Mathf.Max(LStepMaxTime, 0.0001f)) / Mathf.Clamp(multL, 0.01f, 100);
            }
            else
            {
                TT = (Time.deltaTime / LTimeToStep) / Mathf.Clamp(multL, 0.01f, 100);
            }
            TT = Mathf.Clamp01(TT);
            leftLegLastL.position = Vector3.Lerp(leftLegLastL.position, leftLeg.position, TT);
            leftLegLastL.SurfaceNormal = Vector3.Lerp(leftLegLastL.SurfaceNormal, leftLeg.SurfaceNormal, TT);
            leftLegLastL.rotation = Quaternion.Lerp(leftLegLastL.rotation, leftLeg.rotation, TT);
        }
        else if (LeftValue < 0.999f)
        {
            leftLegLastL.position = Vector3.Lerp(leftLegLast.position, leftLeg.position, LeftValue);
            leftLegLastL.SurfaceNormal = Vector3.Lerp(leftLegLast.SurfaceNormal, leftLeg.SurfaceNormal, LeftValue);
            leftLegLastL.rotation = Quaternion.Lerp(leftLegLast.rotation, leftLeg.rotation, LeftValue);
        }

        if (!Stopped)
        {
            if (Speed <= walkSpeed / 20 && RotationSpeed <= RotationError / 50)
            {
                RStepMaxTime -= Time.deltaTime;
                TT = (Time.deltaTime / Mathf.Max(RStepMaxTime, 0.0001f)) / Mathf.Clamp(multR, 0.01f, 100);
            }
            else
            {
                TT = (Time.deltaTime / RTimeToStep) / Mathf.Clamp(multR, 0.01f, 100);
            }
            TT = Mathf.Clamp01(TT);
            rightLegLastL.position = Vector3.Lerp(rightLegLastL.position, rightLeg.position, TT);
            rightLegLastL.SurfaceNormal = Vector3.Lerp(rightLegLastL.SurfaceNormal, rightLeg.SurfaceNormal, TT);
            rightLegLastL.rotation = Quaternion.Lerp(rightLegLastL.rotation, rightLeg.rotation, TT);
        }
        else if (RightValue < 0.999f)
        {
            rightLegLastL.position = Vector3.Lerp(rightLegLast.position, rightLeg.position, RightValue);
            rightLegLastL.SurfaceNormal = Vector3.Lerp(rightLegLast.SurfaceNormal, rightLeg.SurfaceNormal, RightValue);
            rightLegLastL.rotation = Quaternion.Lerp(rightLegLast.rotation, rightLeg.rotation, RightValue);
        }
    }

    private void RecomputeLegPos()
    {
        if (RecomputeLegPlacement && !Stopped)
        {
            if (LeftLast && LeftValue < 0.99f)
            {
                Vector3 predict_Pos = v2T3(PredictStepPos(movementDir, Speed, lastHipPosL, false, true, true));
                Vector3 legP = predict_Pos + LeftLocOffset;
                Vector3 p = legP + Vector3.up * (maxStepHeight + leftLegLenght);

                if (SmartStep)
                {
                    float y = Mathf.Max(leftLeg.position.y, rightLeg.position.y);
                    p = BestPosition(p + leftLeg.position.y * Vector3.up, y * Vector3.up, Quaternion.Euler(0, leftLeg.rotation.eulerAngles.y, 0), Scal, Qual, maxStepHeight,
                    true, lastBestL, LeftValue, true);
                }
                lastBestL = p;

                leftLeg = FireCast(p, leftLeg.rotation, leftLegBounds, true);
                if (SmoothSteppingOver)
                {
                    FindDistances(leftLeg.position, leftLegLast.position, 1, 100, leftCurve, true);
                }
            }

            else if (RightValue < 0.99f)
            {
                Vector3 predict_Pos = v2T3(PredictStepPos(movementDir, Speed, lastHipPosR, false, false, true));
                Vector3 legP = predict_Pos + RightLocOffset;
                Vector3 p = legP + Vector3.up * (maxStepHeight + leftLegLenght);

                if (SmartStep)
                {
                    float y = Mathf.Max(leftLeg.position.y, rightLeg.position.y);
                    p = BestPosition(p + rightLeg.position.y * Vector3.up, y * Vector3.up, Quaternion.Euler(0, rightLeg.rotation.eulerAngles.y, 0), Scal, Qual, maxStepHeight,
                    false, lastBestR, RightValue, true);
                }
                lastBestR = p;

                rightLeg = FireCast(p, rightLeg.rotation, rightLegBounds, false);
                if (SmoothSteppingOver)
                {
                    FindDistances(rightLeg.position, rightLegLast.position, 2, 100, rightCurve, false);
                }
            }
        }
        LerpAdaptivePosition();
    }

    #endregion


    #region Colision

    private void ComputeColision()
    {
        if (AvoidObstacles)
        {
            RaycastHit Colide;
            int Cnumber = Mathf.Max(ColisionQuality, 3);

            boxCastInfo info;

            float[] heightsL = new float[Cnumber + 3];
            //left foot
            Vector3 RealDir = leftLeg.position - leftLegLast.position;
            RealDir.Normalize();
            Vector3 dir = SetY0(leftLeg.position - leftLegLast.position);
            dir.Normalize();
            Vector3 leftCP = leftLegLast.position + leftLegLast.SurfaceNormal * FeetHeight;
            Vector3 leftCPL = leftLeg.position + leftLeg.SurfaceNormal * FeetHeight;
            bool Cancel = false;
            if (dir == Vector3.zero)
            {
                dir = new Vector3(1, 0, 1).normalized;
            }
            if (RealDir == Vector3.zero)
            {
                RealDir = new Vector3(1, 0, 1).normalized;
            }

            Plane planeL = new Plane(leftLegLast.SurfaceNormal, leftLegLast.position);
            Vector3 LeftLocalizedL = Physics.ClosestPoint(planeL.ClosestPointOnPlane(leftLegLast.position + dir), leftCollider, leftCP, leftLegLast.rotation);
            Vector3 LeftLocalizedLV = Physics.ClosestPoint(planeL.ClosestPointOnPlane(leftLegLast.position - dir), leftCollider, leftCP, leftLegLast.rotation);

            planeL = new Plane(leftLeg.SurfaceNormal, leftLeg.position);
            Vector3 LeftLocalizedC = Physics.ClosestPoint(planeL.ClosestPointOnPlane(leftLeg.position - dir), leftCollider, leftCPL, leftLeg.rotation);
            Vector3 LeftLocalizedCV = Physics.ClosestPoint(planeL.ClosestPointOnPlane(leftLeg.position + dir), leftCollider, leftCPL, leftLeg.rotation);

            float dist = Mathf.Max(Vector3.Distance(leftLeg.position, leftLegLast.position), 0.001f);

            LbeforeDistance = Vector3.Distance(LeftLocalizedC, leftLeg.position) / dist;
            LafterDistance = Vector3.Distance(LeftLocalizedL, leftLegLast.position) / dist;

            heightsL[0] = 0;
            heightsL[Cnumber + 2] = 0;
            float dif = Mathf.Abs(leftLeg.position.y - rightLeg.position.y);

            for (int i = 1; i <= Cnumber; i++)
            {
                float height = maxStepHeight * 1.4f + dif;
                info.Center = Vector3.Lerp(LeftLocalizedLV, LeftLocalizedCV, 1.0f / (Cnumber * 2.0f) + (i - 1.0f) / Cnumber) + Vector3.up * height;
                info.Size = new Vector3(leftLegBounds.size.x, 0.03f, Vector3.Distance(leftLeg.position, leftLegLast.position) / Cnumber);
                info.Direction = Vector3.down;
                info.Orientation = Quaternion.LookRotation(RealDir);

                if (Physics.BoxCast(info.Center, info.Size / 2, info.Direction, out Colide, info.Orientation, Mathf.Infinity, mask))
                {
                    heightsL[i] = Mathf.Round((height - Mathf.Min(Mathf.Abs(info.Center.y - Colide.point.y), height)) * 1000) / 1000;
                    if (height - Mathf.Min(Mathf.Abs(info.Center.y - Colide.point.y)) < -maxStepHeight)
                    {
                        Cancel = true;
                    }
                }
                else
                {
                    heightsL[i] = 0;
                    Cancel = true;
                }
            }
            if (!LeftClipped && !Cancel)
            {
                leftCurve = SmoothArray(heightsL, LafterDistance, LbeforeDistance);
            }
            else
            {
                leftCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));
            }
            Cancel = false;

            // right foot

            float[] heightsR = new float[Cnumber + 3];
            RealDir = rightLeg.position - rightLegLast.position;
            RealDir.Normalize();
            dir = SetY0(rightLeg.position - rightLegLast.position);
            dir.Normalize();
            Vector3 rightCP = rightLegLast.position + rightLegLast.SurfaceNormal * FeetHeight;
            Vector3 rightCPL = rightLeg.position + rightLeg.SurfaceNormal * FeetHeight;
            if (dir == Vector3.zero)
            {
                dir = new Vector3(1, 0, 1).normalized;
            }

            if (RealDir == Vector3.zero)
            {
                RealDir = new Vector3(1, 0, 1).normalized;
            }

            Plane planeR = new Plane(rightLegLast.SurfaceNormal, rightLegLast.position);
            Vector3 RightLocalizedL = Physics.ClosestPoint(planeR.ClosestPointOnPlane(rightLegLast.position + dir), rightCollider, rightCP, rightLegLast.rotation);
            Vector3 RightLocalizedLV = Physics.ClosestPoint(planeR.ClosestPointOnPlane(rightLegLast.position - dir), rightCollider, rightCP, rightLegLast.rotation);

            planeR = new Plane(rightLeg.SurfaceNormal, rightLeg.position);
            Vector3 RightLocalizedC = Physics.ClosestPoint(planeR.ClosestPointOnPlane(rightLeg.position - dir), rightCollider, rightCPL, rightLeg.rotation);
            Vector3 RightLocalizedCV = Physics.ClosestPoint(planeR.ClosestPointOnPlane(rightLeg.position + dir), rightCollider, rightCPL, rightLeg.rotation);

            dist = Mathf.Max(Vector3.Distance(rightLeg.position, rightLegLast.position), 0.001f);
            RbeforeDistance = Vector3.Distance(RightLocalizedC, rightLeg.position) / dist;
            RafterDistance = Vector3.Distance(RightLocalizedL, rightLegLast.position) / dist;

            heightsR[0] = 0;
            heightsR[Cnumber + 2] = 0;

            for (int i = 1; i <= Cnumber; i++)
            {
                float height = maxStepHeight * 1.4f + dif;
                info.Center = Vector3.Lerp(RightLocalizedLV, RightLocalizedCV, 1.0f / (Cnumber * 2.0f) + (i - 1.0f) / Cnumber) + Vector3.up * height;
                info.Size = new Vector3(rightLegBounds.size.x, 0.03f, Vector3.Distance(rightLeg.position, rightLegLast.position) / Cnumber);
                info.Direction = Vector3.down;
                info.Orientation = Quaternion.LookRotation(RealDir);

                if (Physics.BoxCast(info.Center, info.Size / 2, info.Direction, out Colide, info.Orientation, Mathf.Infinity, mask))
                {
                    heightsR[i] = Mathf.Round((height - Mathf.Min(Mathf.Abs(info.Center.y - Colide.point.y), height)) * 1000) / 1000;
                    //DrawBoxCastBox(info, Mathf.Abs(info.Center.y - Colide.point.y), Color.red, 0.2f);
                    if (height - Mathf.Min(Mathf.Abs(info.Center.y - Colide.point.y)) < -maxStepHeight)
                    {
                        Cancel = true;
                    }
                }
                else
                {
                    heightsR[i] = 0;
                    Cancel = true;
                }
            }

            if (!RightClipped && !Cancel)
            {
                rightCurve = SmoothArray(heightsR, RafterDistance, RbeforeDistance);
            }
            else
            {
                rightCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));
            }
        }
    }

    AnimationCurve SmoothArray(float[] aray, float afterD, float beforeD)
    {
        AnimationCurve curve = new AnimationCurve();

        int point1 = 0;
        int point2 = LargestValueInArray(aray);
        curve.AddKey(new Keyframe(0, 0));
        float s = (Mathf.Max((float)point2 - 0.5f, 0.5f)) / (aray.Length - 3);
        s -= afterD;
        s = Mathf.Clamp(s, 0.1f, 0.9f);

        curve.AddKey(new Keyframe(s, aray[point2]));
        curve.AddKey(new Keyframe(1, 0));

        if (Mathf.Abs(point2 - point1) > 1)
        {
            //look for back edges
            for (int i = point1 + 1; i < point2; i++)
            {
                float num = (float)(aray[point1] * Mathf.Abs(point1 - i) + aray[point2] * Mathf.Abs(point2 - i)) / (float)Mathf.Abs(point2 - point1);
                if (aray[i] > num)
                {
                    point1 = i;
                    float t = ((float)i - 1) / (aray.Length - 3);
                    t -= afterD;
                    t = Mathf.Clamp(t, 0.1f, 0.9f);
                    curve.AddKey(new Keyframe(t, aray[i]));
                }
                else
                {
                    aray[i] = num;
                }
            }
        }
        point1 = point2;
        point2 = aray.Length - 1;

        if (Mathf.Abs(point2 - point1) > 1)
        {
            //look for front edges
            for (int i = point1 + 1; i < point2; i++)
            {
                float num = (float)(aray[point1] * Mathf.Abs(point1 - i) + aray[point2] * Mathf.Abs(point2 - i)) / (float)Mathf.Abs(point2 - point1);
                if (aray[i] > num)
                {
                    point1 = i;
                    float t = (Mathf.Min((float)i, (float)point2 - 2.5f)) / (aray.Length - 3);
                    t -= afterD;
                    t = Mathf.Clamp(t, 0.1f, 0.9f);
                    curve.AddKey(new Keyframe(t, aray[i]));
                }
                else
                {
                    aray[i] = num;
                }
            }
        }

        List<Keyframe> frames = new List<Keyframe>(curve.keys);

        for (int i = 0; i < curve.keys.Length; i++)
        {
            Keyframe key = new Keyframe(Mathf.Clamp(curve.keys[i].time + beforeD + afterD, 0.1f, 0.9f), curve.keys[i].value);

            if (curve.Evaluate(key.time) < key.value)
            {
                frames.Add(key);
            }
        }

        frames = RemoveDips(frames);
        AnimationCurve curve2 = new AnimationCurve(frames.ToArray());

        return curve2;
    }

    List<Keyframe> RemoveDips(List<Keyframe> keys)
    {
        int count = keys.Count;
        keys.Sort((kf1, kf2) => kf1.time.CompareTo(kf2.time));

        int point1 = 0;
        int point2 = LargestValueInList(keys);

        int[] toRemove = new int[count];
        for (int i = 0; i < count; i++)
        {
            toRemove[i] = -1;
        }
        int k = 0;

        if (Mathf.Abs(point2 - point1) > 1)
        {
            //look for back edges
            for (int i = point1 + 1; i < point2; i++)
            {
                bool foundBigger = false;

                for (int s = i + 1; s <= point2; s++)
                {
                    if (keys[i].value < keys[s].value)
                    {
                        foundBigger = true;
                        break;
                    }
                }

                float num = Mathf.Lerp(keys[point1].value, keys[point2].value, (keys[i].time - keys[point1].time) / (keys[point2].time - keys[point1].time));
                if (foundBigger || keys[i].value < num)
                {
                    toRemove[k] = i;
                }
                else
                {
                    point1 = i;
                }
            }
        }

        point1 = point2;
        point2 = keys.Count - 1;


        if (Mathf.Abs(point2 - point1) > 1)
        {
            //look for front edges
            for (int i = point1 + 1; i < point2; i++)
            {
                bool foundBigger = false;

                for (int s = i; s <= point2; s++)
                {
                    if (keys[s].value > keys[i].value)
                    {
                        foundBigger = true;
                        break;
                    }
                }

                float num = Mathf.Lerp(keys[point1].value, keys[point2].value, (keys[i].time - keys[point1].time) / (keys[point2].time - keys[point1].time));
                if (foundBigger || keys[i].value < num)
                {
                    toRemove[k] = i;
                    k++;
                }
                else
                {
                    point1 = i;
                }
            }
        }
        for (int i = 0; i < k; i++)
        {
            keys.RemoveAt(toRemove[i] - i);
        }

        return keys;
    }

    #endregion


    #region Proces_Raw_Data

    Vector2 GetMovementDir(bool Update_Lerp = false)
    {
        Vector2 movement_Dir;
        movement_Dir = v3To2(ParentObject.position - LastPos);

        float SpeedThisFrame = Vector3.Distance(SetY0(ParentObject.position), SetY0(LastPos)) / Time.deltaTime;
        movement_Dir.Normalize();

        Vector3 movement3_Dir;
        movement3_Dir = ParentObject.position - LastPos;
        movement3_Dir.Normalize();
        LastPos = ParentObject.position;

        if (Update_Lerp)
        {
            movementDir = v3To2(Vector3.Slerp(v2T3(movementDir), v2T3(movement_Dir), Time.deltaTime * DirectionLerpSpeed));
            d3MovementDir = Vector3.Slerp(d3MovementDir, movement3_Dir, Time.deltaTime * DirectionLerpSpeed);
            if (SpeedThisFrame > Speed)
            {
                Speed = Mathf.Lerp(Speed, SpeedThisFrame, Time.deltaTime * SpeedLerpSpeed);
            }
            else
            {
                Speed = Mathf.Lerp(Speed, SpeedThisFrame, Time.deltaTime * SpeedLerpSpeed * 2);
            }

            AngleChangeReduction = Mathf.Min(AngleChangeReduction, (180 - Vector2.Angle(movementDir, lastDir)) / 180);
            AngleChangeReduction = Mathf.Lerp(AngleChangeReduction, 1, 6 * Time.deltaTime);

            lastDir = movement_Dir;

            float D = Mathf.Pow(Mathf.Clamp01(Vector2.Dot(v3To2(ParentObject.forward).normalized, movementDir.normalized)), 15);
            float sp = Mathf.Clamp01((Speed - walkSpeed) / (RunSpeed - walkSpeed));
            reduceXFootOffset = Mathf.Clamp01((1 - (sp * D)) + 0.35f);
        }

        return movementDir.normalized;
    }

    void GatherBasicData()
    {
        leftFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        rightFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);
        lastHipPosL = Hips.position;
        lastHipPosR = Hips.position;
        LargestRadius = (Mathf.Max(leftLegBounds.size.x, leftLegBounds.size.y, leftLegBounds.size.z)
            + Mathf.Max(rightLegBounds.size.x, rightLegBounds.size.y, rightLegBounds.size.z)) / 3;
        D = Vector2.one;
        Stopped = true;
        leftCurve = new AnimationCurve();
        rightCurve = new AnimationCurve();

        LowerSpine = anim.GetBoneTransform(HumanBodyBones.Spine);
        MiddleSpine = anim.GetBoneTransform(HumanBodyBones.Chest);
        UpperSpine = anim.GetBoneTransform(HumanBodyBones.UpperChest);
        Neck = anim.GetBoneTransform(HumanBodyBones.Neck);

        lastRot = ParentObject.rotation;
        leftToe = anim.GetBoneTransform(HumanBodyBones.LeftToes);
        rightToe = anim.GetBoneTransform(HumanBodyBones.RightToes);

        leftKnee = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        rightKnee = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);

        leftUPleg = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        rightUPleg = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);

        LeftShoulder = anim.GetBoneTransform(HumanBodyBones.LeftShoulder);
        LeftArm = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        LeftForearm = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);

        RightShoulder = anim.GetBoneTransform(HumanBodyBones.RightShoulder);
        RightArm = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
        RightForearm = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);

        leftLegLenght = Vector3.Distance(leftUPleg.position, leftKnee.position) +
        Vector3.Distance(leftKnee.position, anim.GetBoneTransform(HumanBodyBones.LeftFoot).position);

        rightLegLenght = Vector3.Distance(rightUPleg.position, rightKnee.position) +
        Vector3.Distance(rightKnee.position, anim.GetBoneTransform(HumanBodyBones.RightFoot).position);


        LastPos = ParentObject.position;
        leftLeg.position = Vector3.zero;
        leftLeg.rotation = Quaternion.identity;
        rightLeg.position = Vector3.zero;
        rightLeg.rotation = Quaternion.identity;

        LeftPosOffset.x = LeftLegPlacement.positionX;
        LeftPosOffset.y = LeftLegPlacement.positionY;

        RightPosOffset.x = RightLegPlacement.positionX;
        RightPosOffset.y = RightLegPlacement.positionY;

        CalculateBoxInformation(leftLeg.position, rightLeg.position);

        Vector3 dir;
        Vector3 forw;
        Transform left = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        Vector3[] directions = {left.rotation * Vector3.forward, left.rotation * -Vector3.forward, left.rotation * Vector3.right, left.rotation * -Vector3.right,
            left.rotation * Vector3.up, left.rotation * -Vector3.up};

        forw = directions[0];
        for (int i = 1; i < 5; i++)
        {
            if (Vector2.Dot(v3To2(forw).normalized, v3To2(ParentObject.forward).normalized) < Vector2.Dot(v3To2(directions[i]).normalized, v3To2(ParentObject.forward).normalized))
            {
                forw = directions[i];
            }
        }
        forw = SetY0(forw).normalized;
        dir = forw;

        if (left.Find("Colider") == null)
        {
            leftCollP = Instantiate(new GameObject(), left.position, Quaternion.LookRotation(dir, Vector3.up));
            leftCollP.transform.SetParent(left);
            leftCollP.name = "Colider";
            leftCollP.layer = left.gameObject.layer;

            leftCollider = leftCollP.gameObject.AddComponent<BoxCollider>();
        }
        else
        {
            leftCollP = left.Find("Colider").gameObject;
            leftCollider = leftCollP.gameObject.GetComponent<BoxCollider>();
        }
        leftCollider.size = new Vector3(leftLegBounds.size.x, leftLegBounds.size.y, leftLegBounds.size.z) * 1.1f;
        leftCollider.center = leftBox.Center;


        Transform right = anim.GetBoneTransform(HumanBodyBones.RightFoot);
        Vector3[] directions1 = {right.rotation * Vector3.forward, right.rotation * -Vector3.forward, right.rotation * Vector3.right, right.rotation * -Vector3.right,
            right.rotation * Vector3.up, right.rotation * -Vector3.up};

        forw = directions1[0];
        for (int i = 1; i < 5; i++)
        {
            if (Vector2.Dot(v3To2(forw).normalized, v3To2(ParentObject.forward).normalized) < Vector2.Dot(v3To2(directions1[i]).normalized, v3To2(ParentObject.forward).normalized))
            {
                forw = directions1[i];
            }
        }
        forw = SetY0(forw).normalized;
        dir = forw;
        //add object on foot then collier

        if (right.Find("Colider") == null)
        {
            rightCollP = Instantiate(new GameObject(), right.position, Quaternion.LookRotation(dir, Vector3.up));
            rightCollP.transform.SetParent(right);
            rightCollP.name = "Colider";
            rightCollP.layer = right.gameObject.layer;

            rightCollider = rightCollP.gameObject.AddComponent<BoxCollider>();
        }
        else
        {
            rightCollP = right.Find("Colider").gameObject;
            rightCollider = rightCollP.gameObject.GetComponent<BoxCollider>();
        }
        rightCollider.size = new Vector3(rightLegBounds.size.x, rightLegBounds.size.y, rightLegBounds.size.z) * 1.1f;
        rightCollider.center = rightBox.Center;

        leftLegLast = leftLeg;
        rightLegLast = rightLeg;

        LAlpha = 1;
        RAlpha = 1;

        LeftValue = 1;
        RightValue = 1;
        Wanted_WalkFrequency = 0.5f;
        WalkFrequency = 0.5f;

        ToeAnkleLeft = (left.position - leftToe.position);
        ToeAnkleRight = (right.position - rightToe.position);
    }

    void GetLegOffsets()
    {
        Vector2 localMovDir = v3To2(ParentObject.rotation * v2T3(new Vector2(-movementDir.x, movementDir.y)));
        if (localMovDir.magnitude < 0.1f)
        {
            localMovDir = Vector2.one;
        }
        else
        {
            localMovDir.Normalize();
        }

        Vector2 Lwanted;
        Vector2 Rwanted;

        if (!Stopped)
        {
            Lwanted.x = LeftLegPlacement.positionX * reduceXFootOffset;
            Lwanted.y = LeftLegPlacement.positionY * Mathf.Abs(localMovDir.x);

            Rwanted.x = RightLegPlacement.positionX * reduceXFootOffset;
            Rwanted.y = RightLegPlacement.positionY * Mathf.Abs(localMovDir.x);
        }
        else
        {
            Lwanted.x = LeftLegPlacement.positionX;
            Lwanted.y = LeftLegPlacement.positionY;

            Rwanted.x = RightLegPlacement.positionX;
            Rwanted.y = RightLegPlacement.positionY;

            if (first)
            {
                LeftPosOffset = Lwanted;
                RightPosOffset = Rwanted;
                first = false;
            }
        }
        if (!LeftLast || Stopped)
        {
            LeftPosOffset = Vector2.Lerp(LeftPosOffset, Lwanted, Time.deltaTime * 8);
        }
        if (LeftLast || Stopped)
        {
            RightPosOffset = Vector2.Lerp(RightPosOffset, Rwanted, Time.deltaTime * 8);
        }
        RotatedP = ParentObject.rotation * Quaternion.AngleAxis(hipsANG, Vector3.up);

        LeftLocOffset = RotatedP * Vector3.forward * LeftPosOffset.y + RotatedP * Vector3.right * LeftPosOffset.x;
        RightLocOffset = RotatedP * Vector3.forward * RightPosOffset.y + RotatedP * Vector3.right * -RightPosOffset.x;

    }

    private float GetRotationSpeed(bool UpdateVariable = false)
    {
        float rotSpeed;
        rotSpeed = Vector3.Angle(SetY0(lastRot * Vector3.forward), SetY0(ParentObject.rotation * Vector3.forward)) / Time.deltaTime;

        if (UpdateVariable)
        {
            RotationSpeed = Mathf.Lerp(RotationSpeed, rotSpeed, SpeedLerpSpeed * Time.deltaTime);
        }
        lastRot = ParentObject.rotation;
        return rotSpeed;
    }

    void CalculateBoxInformation(Vector3 LeftP, Vector3 RightP)
    {
        Vector3 dir;

        switch (leftLegBounds.Axis)
        {
            case Orientation.X:
                dir = leftFoot.rotation * Vector3.right;
                break;
            case Orientation.Y:
                dir = leftFoot.rotation * Vector3.up;
                break;
            case Orientation.Z:
                dir = leftFoot.rotation * Vector3.forward;
                break;
            case Orientation.NegativeX:
                dir = leftFoot.rotation * Vector3.left;
                break;
            case Orientation.NegativeY:
                dir = leftFoot.rotation * Vector3.down;
                break;
            default:
                dir = leftFoot.rotation * Vector3.back;
                break;
        }
        leftFootDir = dir;

        leftBox.Center = LeftP;
        leftBox.Size = leftLegBounds.size;
        leftBox.Direction = Quaternion.LookRotation(Vector3.down, dir) * Vector3.forward;
        leftBox.Orientation = Quaternion.LookRotation(Vector3.down, dir);
        leftBox.Center += leftBox.Orientation * Vector3.right * leftLegBounds.ShiftCenterX * leftLegBounds.size.x
        + leftBox.Orientation * Vector3.up * -leftLegBounds.ShiftCenterY * -leftLegBounds.size.y
        + leftBox.Orientation * Vector3.forward * leftLegBounds.ShiftCenterZ * leftLegBounds.size.z;

        //right

        switch (rightLegBounds.Axis)
        {
            case Orientation.X:
                dir = rightFoot.rotation * Vector3.right;
                break;
            case Orientation.Y:
                dir = rightFoot.rotation * Vector3.up;
                break;
            case Orientation.Z:
                dir = rightFoot.rotation * Vector3.forward;
                break;
            case Orientation.NegativeX:
                dir = rightFoot.rotation * Vector3.left;
                break;
            case Orientation.NegativeY:
                dir = rightFoot.rotation * Vector3.down;
                break;
            default:
                dir = rightFoot.rotation * Vector3.back;
                break;
        }
        rightFootDir = dir;
        rightBox.Center = RightP;
        rightBox.Size = rightLegBounds.size;
        rightBox.Direction = Quaternion.LookRotation(Vector3.down, dir) * Vector3.forward;
        rightBox.Orientation = Quaternion.LookRotation(Vector3.down, dir);
        rightBox.Center += rightBox.Orientation * Vector3.right * rightLegBounds.ShiftCenterX * rightLegBounds.size.x
        + rightBox.Orientation * Vector3.up * rightLegBounds.ShiftCenterY * -rightLegBounds.size.y
        + rightBox.Orientation * Vector3.forward * rightLegBounds.ShiftCenterZ * rightLegBounds.size.z;
    }

    private void GetMovementLine()
    {
        Vector2 dir = movementDir.normalized;
        DirLine.a = new Vector2(Hips.position.x, Hips.position.z) + dir * 5;
        DirLine.b = new Vector2(Hips.position.x, Hips.position.z) + dir * 5;

        DirLine.a += RotateVector(dir, 90, 0);
        DirLine.b += RotateVector(dir, -90, 0);
    }

    private void ComputeMovementLine(Vector2 Direction, Vector3 center)
    {
        LineSensor.a = v3To2(center) + RotateVector(Direction, 90, 0) * leftLegLenght * 3;
        LineSensor.b = v3To2(center) + RotateVector(Direction, -90, 0) * leftLegLenght * 3;
    }

    private void ComputeAMomentum()
    {
        float angle = Vector3.SignedAngle(lastD, SetY0(ParentObject.forward).normalized, Vector3.up) / Time.deltaTime;
        AngularMomentum = Mathf.Lerp(AngularMomentum, angle, DirectionLerpSpeed * Time.deltaTime);

        lastD = SetY0(ParentObject.forward).normalized;

        //aditionalLRot
        if (AngularMomentum < 0)
        {
            aditionalLRot = Mathf.Lerp(aditionalLRot, Mathf.Abs(AngularMomentum), DirectionLerpSpeed * Time.deltaTime * 7);
            aditionalRRot = Mathf.Lerp(aditionalRRot, 0, DirectionLerpSpeed * Time.deltaTime * 7);

            aditionalRR = Mathf.Lerp(aditionalRR, Mathf.Abs(angle) / 3, DirectionLerpSpeed * Time.deltaTime * 8);
            aditionalLR = Mathf.Lerp(aditionalLR, Mathf.Abs(angle), DirectionLerpSpeed * Time.deltaTime * 8);
        }
        else if (AngularMomentum > 0)
        {
            aditionalRRot = Mathf.Lerp(aditionalRRot, Mathf.Abs(AngularMomentum), DirectionLerpSpeed * Time.deltaTime * 7);
            aditionalLRot = Mathf.Lerp(aditionalLRot, 0, DirectionLerpSpeed * Time.deltaTime * 7);

            aditionalRR = Mathf.Lerp(aditionalRR, Mathf.Abs(angle), DirectionLerpSpeed * Time.deltaTime * 8);
            aditionalLR = Mathf.Lerp(aditionalLR, Mathf.Abs(angle) / 3, DirectionLerpSpeed * Time.deltaTime * 8);
        }

    }

    float FindMultiplier(Vector3 StartPosition, Vector3 EndPosition, float CurentValue, AnimationCurve Curve, float VerticalMultiplier, bool left)
    {
        float averageSpeedMultip;
        if (left)
        {
            averageSpeedMultip = totalDistanceL / distanceHorizontalL;
        }
        else
        {
            averageSpeedMultip = totalDistanceR / distanceHorizontalR;
        }
        float MoveValue = 0.00001f;
        Vector3 p1 = Vector3.Lerp(StartPosition, EndPosition, CurentValue);
        Vector3 p2 = Vector3.Lerp(StartPosition, EndPosition, CurentValue + MoveValue);

        float D1 = Vector3.Distance(p1, p2);

        p1 += Vector3.up * Curve.Evaluate(CurentValue) * VerticalMultiplier;
        p2 += Vector3.up * Curve.Evaluate(CurentValue + MoveValue) * VerticalMultiplier;

        float D2 = Vector3.Distance(p1, p2);
        float TimeMultip;
        if (D2 != D1)
        {
            TimeMultip = (D2 / D1) / averageSpeedMultip;
        }
        else
        {
            TimeMultip = 1;
        }

        return TimeMultip;
    }

    void FindDistances(Vector3 StartPosition, Vector3 EndPosition, float VerticalMultiplier, int Precision, AnimationCurve curve, bool left)
    {
        if (left)
        {
            distanceHorizontalL = Vector3.Distance(StartPosition, EndPosition);
            totalDistanceL = 0;
            for (int i = 0; i < Precision; i++)
            {
                Vector3 p3 = Vector3.Lerp(StartPosition, EndPosition, (1f / (float)Precision) * i);
                p3.y += curve.Evaluate(1 - (1f / (float)Precision) * i) * VerticalMultiplier;
                Vector3 p4 = Vector3.Lerp(StartPosition, EndPosition, (1f / (float)Precision) * (i + 1));
                p4.y += curve.Evaluate(1 - (1f / (float)Precision) * (i + 1)) * VerticalMultiplier;

                float D3 = Vector3.Distance(p3, p4);
                totalDistanceL += D3;
            }
        }
        else
        {
            distanceHorizontalR = Vector3.Distance(StartPosition, EndPosition);
            totalDistanceR = 0;
            for (int i = 0; i < Precision; i++)
            {
                Vector3 p3 = Vector3.Lerp(StartPosition, EndPosition, (1f / (float)Precision) * i);
                p3.y += curve.Evaluate((1f / (float)Precision) * i) * VerticalMultiplier;
                Vector3 p4 = Vector3.Lerp(StartPosition, EndPosition, (1f / (float)Precision) * (i + 1));
                p4.y += curve.Evaluate((1f / (float)Precision) * (i + 1)) * VerticalMultiplier;

                float D3 = Vector3.Distance(p3, p4);
                totalDistanceR += D3;
            }
        }
    }

    #endregion


    #region Animations

    float Left_arm_clock()
    {
        //float clock;
        float SupposedV;

        float rightR = 0;
        if (LeftValue >= 0.86f && RightValue < 1)
        {
            rightR = RightValue;
        }

        if (LeftValue == 1 && RightValue == 1)
        {
            if (!LeftLast)
            {
                SupposedV = 0;
            }
            else
            {
                SupposedV = 1;
            }
        }
        else
        {
            SupposedV = Mathf.Max(LeftValue, 1 - RightValue) - rightR;
        }

        /*TotalTime += Time.deltaTime * WalkFrequency * 6.4f;
        clock = (Mathf.Sin(TotalTime) + 1) / 2;*/

        return SupposedV;
    }

    private void UpdateHipsHeight()
    {
        if (UpdateHips)
        {
            float HH = Mathf.Lerp(HipsHeight.x, HipsHeight.y, (SlowSpeed - walkSpeed) / (RunSpeed - walkSpeed));

            float Wantedheight;
            float DistanceBased;

            leftP = leftTransform.position + Vector3.up * LeftHLif;
            rightP = rightTransform.position + Vector3.up * RightHLif;


            float wantedF;
            float minV;

            if (!downG)
            {
                minV = Mathf.Max(rightP.y, leftP.y) - Mathf.Lerp(maxStepHeight,  (maxStepHeight / 2.3f), Mathf.Clamp01(UPH));
            }
            else
            {
                minV = Mathf.Min(rightP.y, leftP.y) - maxStepHeight / 2;
            }

            if (rightP.y > leftP.y)
            {
                wantedF = Mathf.Clamp(leftP.y, minV, Mathf.Max(rightP.y, leftP.y));
            }
            else
            {
                wantedF = Mathf.Clamp(rightP.y, minV, Mathf.Max(rightP.y, leftP.y));
            }

            Wantedheight = wantedF + HH;

            float MaxDist = Mathf.Max(Vector2.Distance(v3To2(Hips.position), v3To2(rightToeCC.position - RightLocOffset)),
                Vector2.Distance(v3To2(leftToeCC.position - LeftLocOffset), v3To2(Hips.position)));
            DistanceBased = Mathf.Max(MaxDist / (MaxStepDistance / 2), 0);
            Wantedheight -= DistanceBased * 0.35f * HipsUpDownMultiplier;

            hipsH = Mathf.Lerp(hipsH, Wantedheight, HipsMovementSpeed * Time.deltaTime);
        }
    }

    void AnimateSpine()
    {
        fixHead = Quaternion.identity;
        bool Low = LowerSpine != null;
        bool Mid = MiddleSpine != null;
        bool Upp = UpperSpine != null;
        int num = Convert.ToInt32(Low) + Convert.ToInt32(Mid) + Convert.ToInt32(Upp);
        Vector2 localizedDir = v3To2(ParentObject.rotation * v2T3(new Vector2(-movementDir.x, movementDir.y)));
        localizedDir = new Vector2(-localizedDir.x, localizedDir.y);
        if (movementDir != Vector2.zero)
        {
            float State = Mathf.Lerp(0, SpineMultiplier.x, Speed / walkSpeed);
            State = Mathf.Lerp(State, SpineMultiplier.y, (Speed - walkSpeed) / (RunSpeed - walkSpeed));
            float multiplier = State * Mathf.Clamp01(Mathf.Pow(Vector2.Dot(v3To2(ParentObject.forward).normalized, movementDir), 3) + 0.2f);
            if (Low)
            {
                Quaternion r1 = Quaternion.AngleAxis(multiplier * 3 / num, LowerSpine.InverseTransformDirection(Quaternion.LookRotation(v2T3(localizedDir), Vector3.up) * ParentObject.right));
                LowerSpineQ *= r1;
            }
            if (Mid)
            {
                Quaternion r1 = Quaternion.AngleAxis(multiplier * 3 / num, MiddleSpine.InverseTransformDirection(Quaternion.LookRotation(v2T3(localizedDir), Vector3.up) * ParentObject.right));
                MiddleSpineQ *= r1;
            }
            if (Upp)
            {
                Quaternion r1 = Quaternion.AngleAxis(multiplier * 3 / num, UpperSpine.InverseTransformDirection(Quaternion.LookRotation(v2T3(localizedDir), Vector3.up) * ParentObject.right));
                UpperSpineQ *= r1;
            }

            D = localizedDir;
        }
        float dot = Mathf.Clamp(Vector2.Dot(v3To2(ParentObject.forward).normalized, movementDir.normalized), 0.4f, 1);
        //Rotation Add
        float Mx = Mathf.Lerp(0, LeanMultiplier.x, Speed / walkSpeed);
        Mx = Mathf.Lerp(Mx, LeanMultiplier.y, (Speed - walkSpeed) / (RunSpeed - walkSpeed)) / 5;

        float Mul = -Mathf.Clamp(AngularMomentum / 50, -Mx, Mx);

        float spineRA = Mathf.Lerp(0, Spine_Rotate_Amplitude.x, Speed / walkSpeed);
        if (Speed > walkSpeed)
        {
            spineRA = Mathf.Lerp(Spine_Rotate_Amplitude.x, Spine_Rotate_Amplitude.y, (Speed - walkSpeed) / (RunSpeed - walkSpeed)) * dot;
        }

        if (Low)
        {
            Quaternion r1 = Quaternion.AngleAxis(Mul * 3 / num, Quaternion.LookRotation(v2T3(D).normalized, Vector3.up) * Vector3.forward);
            Quaternion r2 = Quaternion.AngleAxis(-Mul * 3 / num, Quaternion.LookRotation(v2T3(D).normalized, Vector3.up) * Vector3.up);
            Quaternion r3 = Quaternion.AngleAxis((Left_arm_value * 2 - 1) * spineRA * 3 / num, LowerSpine.InverseTransformDirection(Vector3.up));
            fixHead *= Quaternion.Inverse(r3);

            LowerSpineQ = LowerSpineQ * r1 * r2 * r3;
        }
        if (Mid)
        {
            Quaternion r1 = Quaternion.AngleAxis(Mul * 3 / num, Quaternion.LookRotation(v2T3(D).normalized, Vector3.up) * Vector3.forward);
            Quaternion r2 = Quaternion.AngleAxis(-Mul * 3 / num, Quaternion.LookRotation(v2T3(D).normalized, Vector3.up) * Vector3.up);
            Quaternion r3 = Quaternion.AngleAxis(-(Left_arm_value * 2 - 1) * spineRA * 3 / num, MiddleSpine.InverseTransformDirection(Vector3.up));
            fixHead *= Quaternion.Inverse(r3);

            MiddleSpineQ = MiddleSpineQ * r1 * r2 * r3;
        }
        if (Upp)
        {
            Quaternion r1 = Quaternion.AngleAxis(Mul * 3 / num, Quaternion.LookRotation(v2T3(D).normalized, Vector3.up) * Vector3.forward);
            Quaternion r2 = Quaternion.AngleAxis(-Mul * 3 / num, Quaternion.LookRotation(v2T3(D).normalized, Vector3.up) * Vector3.up);
            Quaternion r3 = Quaternion.AngleAxis(-(Left_arm_value * 2 - 1) * spineRA * 3 / num, UpperSpine.InverseTransformDirection(Vector3.up));
            fixHead *= Quaternion.Inverse(r3);

            UpperSpineQ = UpperSpineQ * r1 * r2 * r3;
        }

        //LeftShoulderQ *= Quaternion.AngleAxis(-(Left_arm_value * 2 - 1) * spineRA, Vector3.up);
        //RightShoulderQ *= Quaternion.AngleAxis(-(Right_arm_value * 2 - 1) * spineRA, Vector3.up);
    }

    void RotateHipsWhenSideSteping()
    {
        float WantedRotation = Vector2.SignedAngle(v3To2(ParentObject.forward).normalized, movementDir.normalized);
        if (WantedRotation > 105 - boosterL)
        {
            boosterL = 15;
            WantedRotation = -(180 - WantedRotation);
        }
        else
        {
            boosterL = 0;
        }
        if (WantedRotation < -105 + boosterR)
        {
            WantedRotation = 180 + WantedRotation;
            boosterR = 15;
        }
        else
        {
            boosterR = 0;
        }
        WantedRotation /= 105;

        float multiplier = Mathf.Clamp01(Speed / walkSpeed);
        WantedRotation = -WantedRotation * 50 * multiplier;

        hipsANG = Mathf.Lerp(hipsANG, WantedRotation, Time.deltaTime * 5);
        HipsQ *= Quaternion.AngleAxis(hipsANG, Vector3.up);

        int count = 0;
        if (LowerSpine != null)
        {
            count++;
        }
        if (MiddleSpine != null)
        {
            count++;
        }
        if (UpperSpine != null)
        {
            count++;
        }
        count = Mathf.Max(count, 1);
        if (LowerSpine != null)
        {
            LowerSpineQ *= Quaternion.AngleAxis(-hipsANG / count, Hips.InverseTransformDirection(Vector3.up));
        }
        if (MiddleSpine != null)
        {
            MiddleSpineQ *= Quaternion.AngleAxis(-hipsANG / count, Hips.InverseTransformDirection(Vector3.up));
        }
        if (UpperSpine != null)
        {
            UpperSpineQ *= Quaternion.AngleAxis(-hipsANG / count, Hips.InverseTransformDirection(Vector3.up));
        }

    }

    void AlignH()
    {
        if (movementDir != Vector2.zero)
        {
            float State = Mathf.Lerp(0, SpineMultiplier.x, Speed / walkSpeed);
            State = Mathf.Lerp(State, SpineMultiplier.y, Speed / RunSpeed);

            float multip = 1 + 1 / 3 + 1 / 5;

            float multiplier = State * Mathf.Clamp01(Mathf.Pow(Vector2.Dot(v3To2(ParentObject.forward).normalized, movementDir.normalized), 3) + 0.2f);
            NeckQ *= Quaternion.AngleAxis(-multiplier * multip * HeadPercentage / 100, Neck.InverseTransformDirection(ParentObject.right));
        }

        NeckQ *= fixHead;
    }

    void MoveArms()
    {
        Vector3 LShDir = LeftShoulder.InverseTransformDirection(-ParentObject.right);
        Vector3 LADir = LeftArm.InverseTransformDirection(-ParentObject.right);
        Vector3 LFaDir = LeftForearm.InverseTransformDirection(-ParentObject.right);

        Vector3 RShDir = RightShoulder.InverseTransformDirection(-ParentObject.right);
        Vector3 RADir = RightArm.InverseTransformDirection(-ParentObject.right);
        Vector3 RFaDir = RightForearm.InverseTransformDirection(-ParentObject.right);

        // Left
        if (SlowSpeed < Speed)
        {
            SlowSpeed = Mathf.Lerp(SlowSpeed, Speed, Time.deltaTime * 2);
        }
        else
        {
            SlowSpeed = Mathf.Lerp(SlowSpeed, Speed, Time.deltaTime * 7);
        }
        Left_arm_value = Left_arm_clock();
        Right_arm_value = 1 - Left_arm_value;

        float SL = (SlowSpeed - walkSpeed) / (RunSpeed - walkSpeed);

        float InX = Mathf.Lerp(BendInward.x, BendInward.z, SL);
        float InY = Mathf.Lerp(BendInward.y, BendInward.w, SL);

        float value = Mathf.Clamp01(Speed / walkSpeed) * Mathf.Clamp(Vector2.Dot(v3To2(ParentObject.forward).normalized, movementDir.normalized), 0.2f, 1);

        float LSX = Mathf.Lerp(ShoulderValues.x, ShoulderValues.z, SL);
        float LSY = Mathf.Lerp(ShoulderValues.y, ShoulderValues.w, SL);

        float LAX = Mathf.Lerp(ArmValues.x, ArmValues.z, SL);
        float LAY = Mathf.Lerp(ArmValues.y, ArmValues.w, SL);

        float LFX = Mathf.Lerp(ForearmValues.x, ForearmValues.z, SL);
        float LFY = Mathf.Lerp(ForearmValues.y, ForearmValues.w, SL);

        LeftShoulderQ *= Quaternion.AngleAxis(Mathf.Lerp(LSX, LSY, Left_arm_value) * value, LShDir);
        LeftArmQ *= Quaternion.AngleAxis(Mathf.Lerp(LAX, LAY, Left_arm_value) * value, LADir);
        LeftForearmQ *= Quaternion.AngleAxis(Mathf.Lerp(LFX, LFY, Left_arm_value) * value, LFaDir);


        LeftArmQ *= Quaternion.AngleAxis(Mathf.Lerp(InX, InY, Left_arm_value) * value, -LeftArm.InverseTransformDirection(Vector3.up));
        // Right

        float RSX = Mathf.Lerp(ShoulderValues.x, ShoulderValues.z, SL);
        float RSY = Mathf.Lerp(ShoulderValues.y, ShoulderValues.w, SL);

        float RAX = Mathf.Lerp(ArmValues.x, ArmValues.z, SL);
        float RAY = Mathf.Lerp(ArmValues.y, ArmValues.w, SL);

        float RFX = Mathf.Lerp(ForearmValues.x, ForearmValues.z, SL);
        float RFY = Mathf.Lerp(ForearmValues.y, ForearmValues.w, SL);

        RightShoulderQ *= Quaternion.AngleAxis(Mathf.Lerp(RSX, RSY, Right_arm_value) * value, RShDir);
        RightArmQ *= Quaternion.AngleAxis(Mathf.Lerp(RAX, RAY, Right_arm_value) * value, RADir);
        RightForearmQ *= Quaternion.AngleAxis(Mathf.Lerp(RFX, RFY, Right_arm_value) * value, RFaDir);

        RightArmQ *= Quaternion.AngleAxis(Mathf.Lerp(InX, InY, Right_arm_value) * value, RightArm.InverseTransformDirection(Vector3.up));
    }

    void MoveHips()
    {
        if (animateHips)
        {
            float Rot = Mathf.Lerp(HipsRotMultiplier.x, HipsRotMultiplier.y, SlowSpeed - walkSpeed / RunSpeed - walkSpeed);
            float value = Mathf.Clamp01(Speed / walkSpeed) * Mathf.Clamp(Vector2.Dot(v3To2(ParentObject.forward).normalized, movementDir.normalized), 0.2f, 1);

            float parabola = Left_arm_value * 2 - 1;
            hipsA = Rot * parabola * value;
            hipsRot = Quaternion.AngleAxis(hipsA, Vector3.forward);
            hipsYrot = hipsA * 1.5f;
            hipsRot *= Quaternion.AngleAxis(hipsYrot, Vector3.up);

            HipsQ *= hipsRot;
            LowerSpineQ *= Quaternion.AngleAxis(-hipsA, Vector3.forward);
            LowerSpineQ *= Quaternion.AngleAxis(-hipsYrot, LowerSpine.InverseTransformDirection(Vector3.up));
        }
    }

    void BendToes(legTransform left, legTransform right)
    {
        float sp = Mathf.Clamp01(1 - (Speed - walkSpeed) / (RunSpeed - walkSpeed));
        float LeftFA = Vector3.SignedAngle(left.rotation * Vector3.forward, SetY0(left.rotation * Vector3.forward).normalized, left.rotation * Vector3.right);
        LeftFA = -Mathf.Max(LeftFA - 10, 0) * 1.5f;
        float dotP = Mathf.Clamp(Vector2.Dot(v3To2(ParentObject.forward).normalized, movementDir.normalized), 0, 1);

        // hip
        float alphaHip = 0;

        if (RightValue < 0.9999f)
        {
            alphaHip = Mathf.Clamp01(RightValue - 0.333f) * 1.5f * bendOnStep.y * dotP * Mathf.Clamp01(1 - sp) + Mathf.Clamp01(RightValue - 0.333f) * 1.5f * bendOnStep.x * dotP * Mathf.Clamp01(Speed / walkSpeed) * sp;
        }

        float adder = -(Mathf.Max(alphaHip - 10, 0) * 2.2f);
        LeftFA += Mathf.Clamp(adder, -80, 0);

        if (leftToeGrounded)
        {
            LeftRotateAngle = Mathf.Lerp(LeftRotateAngle, LeftFA, 6 * Time.deltaTime);
        }
        else
        {
            LeftRotateAngle = Mathf.Lerp(LeftRotateAngle, 0, 6 * Time.deltaTime);
        }
        Vector3 LDirection = SetY0(leftLegLastL.rotation * Vector3.right).normalized;
        leftToesQ *= Quaternion.AngleAxis(LeftRotateAngle, leftToe.InverseTransformDirection(LDirection));

        //RIGHT

        float RightFA = Vector3.SignedAngle(right.rotation * Vector3.forward, SetY0(right.rotation * Vector3.forward).normalized, right.rotation * Vector3.right);
        RightFA = -Mathf.Max(RightFA - 10, 0) * 1.5f;

        // hip
        alphaHip = 0;

        if (LeftValue < 0.9999f)
        {
            alphaHip = Mathf.Clamp01(LeftValue - 0.333f) * 1.5f * bendOnStep.y * dotP * Mathf.Clamp01(1 - sp) + Mathf.Clamp01(LeftValue - 0.333f) * 1.5f * bendOnStep.x * dotP * Mathf.Clamp01(Speed / walkSpeed) * sp;
        }
        adder = -(Mathf.Max(alphaHip - 10, 0) * 2.2f);
        RightFA += Mathf.Clamp(adder, -80, 0);

        if (rightToeGrounded)
        {
            RightRotateAngle = Mathf.Lerp(RightRotateAngle, RightFA, 6 * Time.deltaTime);
        }
        else
        {
            RightRotateAngle = Mathf.Lerp(RightRotateAngle, 0, 6 * Time.deltaTime);
        }
        Vector3 RDirection = SetY0(rightLegLastL.rotation * Vector3.right).normalized;
        rightToesQ *= Quaternion.AngleAxis(RightRotateAngle, rightToe.InverseTransformDirection(RDirection));
    }

    #endregion


    #region Helper_Functions

    bool betwenNumbers(float number, float min, float max)
    {
        bool betwen = number >= min;
        betwen &= number <= max;
        return betwen;
    }

    Vector3 RotateVectorAroundAxis(Vector3 vector, Vector3 axis, float degrees)
    {
        return Quaternion.AngleAxis(degrees, axis) * vector;
    }

    Vector3 GetClosestPointOnInfiniteLine(Vector3 point, Vector3 line_start, Vector3 line_end)
    {
        return line_start + Vector3.Project(point - line_start, line_end - line_start);
    }

    public Vector2 FindNearestPointOnLine(Vector2 origin, Vector2 direction, Vector2 point)
    {
        direction.Normalize();
        Vector2 lhs = point - origin;

        float dotP = Vector2.Dot(lhs, direction);
        return origin + direction * dotP;
    }

    private Vector3 v2T3(Vector2 vector)
    {
        return new Vector3(vector.x, 0, vector.y);
    }

    private Vector2 v3To2(Vector3 vector)
    {
        return new Vector2(vector.x, vector.z);
    }

    Vector3 SetY0(Vector3 vector)
    {
        return v2T3(v3To2(vector));
    }

    private Vector2 RotateVector(Vector2 vector, float angle, float angle1)
    {
        Quaternion rotation = Quaternion.Euler(angle1, 0f, angle);
        Vector2 rotatedVector = rotation * vector;
        return rotatedVector;
    }

    static Vector3 RotatePointAroundP(Vector3 point, Vector3 pivot, Quaternion rotation)
    {
        Vector3 direction = point - pivot;
        return pivot + rotation * direction;
    }

    private float PointDistanceToLine(line line, Vector2 point)
    {
        Vector2 lineDirection = (line.a - line.b).normalized;
        Vector2 AtoPoint = point - line.a;
        float dot = Vector2.Dot(AtoPoint, lineDirection);
        Vector2 P2 = line.a + lineDirection * dot;

        return Vector2.Distance(point, P2);
    }

    Vector3 RotateAroundV(Vector3 pos, Vector3 pivotPoint, float angle, Vector3 axis)
    {
        Quaternion rot = Quaternion.AngleAxis(angle, axis);
        pos = rot * (pos - pivotPoint) + pivotPoint;
        return pos;
    }

    private int LargestValueInList(List<Keyframe> keys)
    {
        int value = 0;
        float Cvalue = 0;
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i].value > Cvalue)
            {
                Cvalue = keys[i].value;
                value = i;
            }
        }

        return value;
    }

    int LargestValueInArray(float[] aray)
    {
        int value = 0;
        float Cvalue = 0;
        for (int i = 0; i < aray.Length; i++)
        {
            if (aray[i] > Cvalue)
            {
                Cvalue = aray[i];
                value = i;
            }
        }

        return value;
    }
    #endregion


    #region Debugger
    void DrawCube(Vector3 position, Vector3 size, Quaternion rotation, bool left)
    {
        Vector3 halfSize = size * 0.5f;

        if (left)
        {
            Vector3[] p =
        {
            rotation * new Vector3(-halfSize.x, -halfSize.y, -halfSize.z) + position,
            rotation * new Vector3(-halfSize.x, -halfSize.y, halfSize.z) + position,
            rotation * new Vector3(halfSize.x, -halfSize.y, halfSize.z) + position,
            rotation * new Vector3(halfSize.x, -halfSize.y, -halfSize.z) + position,
            rotation * new Vector3(-halfSize.x, halfSize.y, -halfSize.z) + position,
            rotation * new Vector3(-halfSize.x, halfSize.y, halfSize.z) + position,
            rotation * new Vector3(halfSize.x, halfSize.y, halfSize.z) + position,
            rotation * new Vector3(halfSize.x, halfSize.y, -halfSize.z) + position
        };
            leftBoxVertices = p;
        }
        else
        {
            Vector3[] p =
            {
            rotation * new Vector3(-halfSize.x, -halfSize.y, -halfSize.z) + position,
            rotation * new Vector3(-halfSize.x, -halfSize.y, halfSize.z) + position,
            rotation * new Vector3(halfSize.x, -halfSize.y, halfSize.z) + position,
            rotation * new Vector3(halfSize.x, -halfSize.y, -halfSize.z) + position,
            rotation * new Vector3(-halfSize.x, halfSize.y, -halfSize.z) + position,
            rotation * new Vector3(-halfSize.x, halfSize.y, halfSize.z) + position,
            rotation * new Vector3(halfSize.x, halfSize.y, halfSize.z) + position,
            rotation * new Vector3(halfSize.x, halfSize.y, -halfSize.z) + position
        };
            rightBoxVertices = p;
        }
    }

    void DrawLegBounds(Transform left, Transform right)
    {
        if (ShowLegBounds)
        {
            // Left Leg
            Vector3 dir;

            dir = leftFootDir;

            boxCastInfo cast = new boxCastInfo();
            cast.Center = left.position;
            cast.Size = leftLegBounds.size;
            cast.Direction = Quaternion.LookRotation(Vector3.down, dir) * Vector3.forward;
            cast.Orientation = Quaternion.LookRotation(Vector3.down, dir);
            cast.Center += cast.Orientation * Vector3.right * leftLegBounds.ShiftCenterX * leftLegBounds.size.x
            + cast.Orientation * Vector3.up * leftLegBounds.ShiftCenterY * leftLegBounds.size.y
            + cast.Orientation * Vector3.forward * leftLegBounds.ShiftCenterZ * leftLegBounds.size.z;

            DrawCube(cast.Center, new Vector3(cast.Size.x, cast.Size.z, cast.Size.y), cast.Orientation, true);

            // Right Leg

            dir = rightFootDir;

            cast = new boxCastInfo();
            cast.Center = right.position;
            cast.Size = rightLegBounds.size;
            cast.Direction = Quaternion.LookRotation(Vector3.down, dir) * Vector3.forward;
            cast.Orientation = Quaternion.LookRotation(Vector3.down, dir);
            cast.Center += cast.Orientation * Vector3.right * rightLegBounds.ShiftCenterX * rightLegBounds.size.x
            + cast.Orientation * Vector3.up * -rightLegBounds.ShiftCenterY * rightLegBounds.size.y
            + cast.Orientation * Vector3.forward * rightLegBounds.ShiftCenterZ * rightLegBounds.size.z;

            DrawCube(cast.Center, new Vector3(cast.Size.x, cast.Size.z, cast.Size.y), cast.Orientation, false);
        }
    }

    public static void DrawBoxCastBox(boxCastInfo info, float distance, Color color, float time = 0.1f)
    {
        Vector3 origin = info.Center;
        Vector3 halfExtents = info.Size / 2;
        Quaternion orientation = info.Orientation;
        Vector3 direction = info.Direction;

        direction.Normalize();
        Box bottomBox = new Box(origin, halfExtents, orientation);
        Box topBox = new Box(origin + (direction * distance), halfExtents, orientation);

        Debug.DrawLine(bottomBox.backBottomLeft, topBox.backBottomLeft, color);
        Debug.DrawLine(bottomBox.backBottomRight, topBox.backBottomRight, color);
        Debug.DrawLine(bottomBox.backTopLeft, topBox.backTopLeft, color);
        Debug.DrawLine(bottomBox.backTopRight, topBox.backTopRight, color);
        Debug.DrawLine(bottomBox.frontTopLeft, topBox.frontTopLeft, color);
        Debug.DrawLine(bottomBox.frontTopRight, topBox.frontTopRight, color);
        Debug.DrawLine(bottomBox.frontBottomLeft, topBox.frontBottomLeft, color);
        Debug.DrawLine(bottomBox.frontBottomRight, topBox.frontBottomRight, color);

        DrawBox(bottomBox, color, time);
        DrawBox(topBox, color, time);
    }

    public static void DrawBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Color color, float time)
    {
        DrawBox(new Box(origin, halfExtents, orientation), color, time);
    }

    public static void DrawBox(Box box, Color color, float time)
    {
        Debug.DrawLine(box.frontTopLeft, box.frontTopRight, color, time);
        Debug.DrawLine(box.frontTopRight, box.frontBottomRight, color, time);
        Debug.DrawLine(box.frontBottomRight, box.frontBottomLeft, color, time);
        Debug.DrawLine(box.frontBottomLeft, box.frontTopLeft, color, time);

        Debug.DrawLine(box.backTopLeft, box.backTopRight, color, time);
        Debug.DrawLine(box.backTopRight, box.backBottomRight, color, time);
        Debug.DrawLine(box.backBottomRight, box.backBottomLeft, color, time);
        Debug.DrawLine(box.backBottomLeft, box.backTopLeft, color, time);

        Debug.DrawLine(box.frontTopLeft, box.backTopLeft, color, time);
        Debug.DrawLine(box.frontTopRight, box.backTopRight, color, time);
        Debug.DrawLine(box.frontBottomRight, box.backBottomRight, color, time);
        Debug.DrawLine(box.frontBottomLeft, box.backBottomLeft, color, time);
    }

    public struct Box
    {
        public Vector3 localFrontTopLeft { get; private set; }
        public Vector3 localFrontTopRight { get; private set; }
        public Vector3 localFrontBottomLeft { get; private set; }
        public Vector3 localFrontBottomRight { get; private set; }
        public Vector3 localBackTopLeft { get { return -localFrontBottomRight; } }
        public Vector3 localBackTopRight { get { return -localFrontBottomLeft; } }
        public Vector3 localBackBottomLeft { get { return -localFrontTopRight; } }
        public Vector3 localBackBottomRight { get { return -localFrontTopLeft; } }

        public Vector3 frontTopLeft { get { return localFrontTopLeft + origin; } }
        public Vector3 frontTopRight { get { return localFrontTopRight + origin; } }
        public Vector3 frontBottomLeft { get { return localFrontBottomLeft + origin; } }
        public Vector3 frontBottomRight { get { return localFrontBottomRight + origin; } }
        public Vector3 backTopLeft { get { return localBackTopLeft + origin; } }
        public Vector3 backTopRight { get { return localBackTopRight + origin; } }
        public Vector3 backBottomLeft { get { return localBackBottomLeft + origin; } }
        public Vector3 backBottomRight { get { return localBackBottomRight + origin; } }

        public Vector3 origin { get; private set; }

        public Box(Vector3 origin, Vector3 halfExtents, Quaternion orientation) : this(origin, halfExtents)
        {
            Rotate(orientation);
        }
        public Box(Vector3 origin, Vector3 halfExtents)
        {
            this.localFrontTopLeft = new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
            this.localFrontTopRight = new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
            this.localFrontBottomLeft = new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);
            this.localFrontBottomRight = new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);

            this.origin = origin;
        }


        public void Rotate(Quaternion orientation)
        {
            localFrontTopLeft = RotatePointAroundP(localFrontTopLeft, Vector3.zero, orientation);
            localFrontTopRight = RotatePointAroundP(localFrontTopRight, Vector3.zero, orientation);
            localFrontBottomLeft = RotatePointAroundP(localFrontBottomLeft, Vector3.zero, orientation);
            localFrontBottomRight = RotatePointAroundP(localFrontBottomRight, Vector3.zero, orientation);
        }
    }

    private void OnDrawGizmos()
    {
        if (anim != null)
        {
            if (Application.isPlaying == false)
            {
                leftFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
                rightFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);

                CalculateBoxInformation(leftLeg.position, rightLeg.position);
            }
        }
        if (ShowLegBounds && leftBoxVertices != null && rightBoxVertices != null)
        {
            if (leftBoxVertices.Length == 8 && rightBoxVertices.Length == 8)
            {
                // left
                Gizmos.color = Color.red;
                Gizmos.DrawLine(leftBoxVertices[0], leftBoxVertices[1]);
                Gizmos.DrawLine(leftBoxVertices[1], leftBoxVertices[2]);
                Gizmos.DrawLine(leftBoxVertices[2], leftBoxVertices[3]);
                Gizmos.DrawLine(leftBoxVertices[3], leftBoxVertices[0]);

                Gizmos.DrawLine(leftBoxVertices[4], leftBoxVertices[5]);
                Gizmos.DrawLine(leftBoxVertices[5], leftBoxVertices[6]);
                Gizmos.DrawLine(leftBoxVertices[6], leftBoxVertices[7]);
                Gizmos.DrawLine(leftBoxVertices[7], leftBoxVertices[4]);

                Gizmos.DrawLine(leftBoxVertices[0], leftBoxVertices[4]);
                Gizmos.DrawLine(leftBoxVertices[1], leftBoxVertices[5]);
                Gizmos.DrawLine(leftBoxVertices[2], leftBoxVertices[6]);
                Gizmos.DrawLine(leftBoxVertices[3], leftBoxVertices[7]);

                // right
                Gizmos.color = Color.red;
                Gizmos.DrawLine(rightBoxVertices[0], rightBoxVertices[1]);
                Gizmos.DrawLine(rightBoxVertices[1], rightBoxVertices[2]);
                Gizmos.DrawLine(rightBoxVertices[2], rightBoxVertices[3]);
                Gizmos.DrawLine(rightBoxVertices[3], rightBoxVertices[0]);

                Gizmos.DrawLine(rightBoxVertices[4], rightBoxVertices[5]);
                Gizmos.DrawLine(rightBoxVertices[5], rightBoxVertices[6]);
                Gizmos.DrawLine(rightBoxVertices[6], rightBoxVertices[7]);
                Gizmos.DrawLine(rightBoxVertices[7], rightBoxVertices[4]);

                Gizmos.DrawLine(rightBoxVertices[0], rightBoxVertices[4]);
                Gizmos.DrawLine(rightBoxVertices[1], rightBoxVertices[5]);
                Gizmos.DrawLine(rightBoxVertices[2], rightBoxVertices[6]);
                Gizmos.DrawLine(rightBoxVertices[3], rightBoxVertices[7]);
            }
        }

        if (Application.isPlaying == false)
        {
            if (EnableDebugger)
            {
                if (ParentObject != null && LowestPointOfCharacter != null && anim != null)
                {
                    float DistN = (Mathf.Min(MoveSpeed * StepDistanceMultiplier, MaxStepDistance) * 2) / 3 + 0.1f;
                    DistN = Mathf.Max(DistN, MinStepDistance);
                    float RunWalkk = Mathf.Clamp01((MoveSpeed - walkSpeed) / (RunSpeed - walkSpeed));
                    Vector3 startPos = SetY0(Hips.position - ParentObject.forward * ((DistN / 2) + MinStepDistance - DistN * (ForwardMultiplier / 5)));
                    startPos += Vector3.up * (LowestPointOfCharacter.position.y + 0.05f);
                    Vector3 endPos = startPos + ParentObject.forward * (DistN - 0.0666f);

                    if (ShowStepDistance)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(startPos, endPos);
                        Gizmos.DrawLine(startPos - ParentObject.right * DistN / 8, startPos + ParentObject.right * DistN / 8);
                        Gizmos.DrawLine(endPos - ParentObject.right * DistN / 8, endPos + ParentObject.right * DistN / 8);
                    }

                    if (ShowFootPath)
                    {
                        Gizmos.color = Color.cyan;
                        Vector3 LastPoint = startPos;
                        int c = 0;
                        for (int i = 1; i <= 50; i++)
                        {
                            float raise = RaiseFootWalking.Evaluate((float)i / 50f) * RaiseFootWMultiplier;
                            raise = Mathf.Lerp(raise, RaiseFootRunning.Evaluate((float)i / 50f) * RaiseFootRMultiplier, RunWalkk);
                            Vector3 point = Vector3.Lerp(startPos, endPos, (float)i / 50f) + Vector3.up * raise;
                            if (i % 2 == 0)
                            {
                                c = 1;
                            }
                            if (c == 0)
                            {
                                Gizmos.DrawLine(LastPoint, point);
                            }
                            else
                            {
                                c--;
                            }

                            LastPoint = point;
                        }
                    }
                    if (ShowSmartStepRadius)
                    {
                        Quaternion RotL = anim.GetBoneTransform(HumanBodyBones.LeftFoot).rotation;
                        RotL = Quaternion.Euler(0, RotL.eulerAngles.y, 0);
                        Quaternion RotR = anim.GetBoneTransform(HumanBodyBones.RightFoot).rotation;
                        RotR = Quaternion.Euler(0, RotR.eulerAngles.y, 0);

                        Vector3 PL = anim.GetBoneTransform(HumanBodyBones.LeftFoot).position - FeetHeight * Vector3.up;
                        Vector3 PR = anim.GetBoneTransform(HumanBodyBones.RightFoot).position - FeetHeight * Vector3.up;

                        Vector3[] LDots = new Vector3[4];
                        LDots[0] = PL + RotL * Vector3.forward * (Scal / 2) / 7f + RotL * Vector3.right * (Scal / 2);
                        LDots[1] = PL + RotL * Vector3.forward * (Scal / 2) / 7f - RotL * Vector3.right * (Scal / 2);
                        LDots[2] = PL - RotL * Vector3.forward * (Scal / 2) / 7f + RotL * Vector3.right * (Scal / 2);
                        LDots[3] = PL - RotL * Vector3.forward * (Scal / 2) / 7f - RotL * Vector3.right * (Scal / 2);
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(LDots[0], LDots[1]);
                        Gizmos.DrawLine(LDots[2], LDots[3]);
                        Gizmos.DrawLine(LDots[0], LDots[2]);
                        Gizmos.DrawLine(LDots[3], LDots[1]);

                        Vector3[] RDots = new Vector3[4];
                        RDots[0] = PR + RotR * Vector3.forward * (Scal / 2) / 7f + RotR * Vector3.right * (Scal / 2);
                        RDots[1] = PR + RotR * Vector3.forward * (Scal / 2) / 7f - RotR * Vector3.right * (Scal / 2);
                        RDots[2] = PR - RotR * Vector3.forward * (Scal / 2) / 7f + RotR * Vector3.right * (Scal / 2);
                        RDots[3] = PR - RotR * Vector3.forward * (Scal / 2) / 7f - RotR * Vector3.right * (Scal / 2);
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(RDots[0], RDots[1]);
                        Gizmos.DrawLine(RDots[2], RDots[3]);
                        Gizmos.DrawLine(RDots[0], RDots[2]);
                        Gizmos.DrawLine(RDots[3], RDots[1]);
                    }
                    if (ShowFootPlacement)
                    {
                        Vector3 PL = anim.GetBoneTransform(HumanBodyBones.LeftFoot).position;
                        Vector3 PR = anim.GetBoneTransform(HumanBodyBones.RightFoot).position;

                        Vector2 leftPOffset = new Vector2(LeftLegPlacement.positionX, LeftLegPlacement.positionY);
                        Vector2 rightPOffset = new Vector2(RightLegPlacement.positionX, RightLegPlacement.positionY);

                        Vector3 LeftLocOffs = ParentObject.rotation * Vector3.forward * leftPOffset.y + ParentObject.rotation * Vector3.right * leftPOffset.x;
                        Vector3 RightLocOffs = ParentObject.rotation * Vector3.forward * rightPOffset.y + ParentObject.rotation * Vector3.right * -rightPOffset.x;

                        Vector3 leftPos = SetY0(Hips.position + LeftLocOffs) + Vector3.up * PL.y;
                        Vector3 rightPos = SetY0(Hips.position + RightLocOffs) + Vector3.up * PR.y;

                        Vector3 dir = Quaternion.Euler(0, LeftLegPlacement.YRotation, 0) * leftFootDir;

                        boxCastInfo cast = new boxCastInfo();
                        cast.Center = leftPos;
                        cast.Size = leftLegBounds.size;
                        cast.Size.y = 0;
                        cast.Direction = Quaternion.LookRotation(SetY0(dir).normalized, Vector3.down) * Vector3.forward;
                        cast.Orientation = Quaternion.LookRotation(SetY0(dir).normalized, Vector3.down);
                        cast.Center += cast.Orientation * Vector3.right * leftLegBounds.ShiftCenterX * leftLegBounds.size.x
                        + cast.Orientation * Vector3.up * leftLegBounds.ShiftCenterY * leftLegBounds.size.y
                        + cast.Orientation * Vector3.forward * leftLegBounds.ShiftCenterZ * leftLegBounds.size.z;

                        Box box = new Box(cast.Center, cast.Size / 2, cast.Orientation);

                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(box.frontTopLeft, box.frontTopRight);
                        Gizmos.DrawLine(box.frontTopRight, box.frontBottomRight);
                        Gizmos.DrawLine(box.frontBottomRight, box.frontBottomLeft);
                        Gizmos.DrawLine(box.frontBottomLeft, box.frontTopLeft);

                        Gizmos.DrawLine(box.backTopLeft, box.backTopRight);
                        Gizmos.DrawLine(box.backTopRight, box.backBottomRight);
                        Gizmos.DrawLine(box.backBottomRight, box.backBottomLeft);
                        Gizmos.DrawLine(box.backBottomLeft, box.backTopLeft);

                        Gizmos.DrawLine(box.frontTopLeft, box.backTopLeft);
                        Gizmos.DrawLine(box.frontTopRight, box.backTopRight);
                        Gizmos.DrawLine(box.frontBottomRight, box.backBottomRight);
                        Gizmos.DrawLine(box.frontBottomLeft, box.backBottomLeft);

                        dir = Quaternion.Euler(0, RightLegPlacement.YRotation, 0) * rightFootDir;

                        cast = new boxCastInfo();
                        cast.Center = rightPos;
                        cast.Size = rightLegBounds.size;
                        cast.Size.y = 0;
                        cast.Direction = Quaternion.LookRotation(SetY0(dir).normalized, Vector3.down) * Vector3.forward;
                        cast.Orientation = Quaternion.LookRotation(SetY0(dir).normalized, Vector3.down);
                        cast.Center += cast.Orientation * Vector3.right * rightLegBounds.ShiftCenterX * rightLegBounds.size.x
                        + cast.Orientation * Vector3.up * -rightLegBounds.ShiftCenterY * rightLegBounds.size.y
                        + cast.Orientation * Vector3.forward * rightLegBounds.ShiftCenterZ * rightLegBounds.size.z;

                        box = new Box(cast.Center, cast.Size / 2, cast.Orientation);

                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(box.frontTopLeft, box.frontTopRight);
                        Gizmos.DrawLine(box.frontTopRight, box.frontBottomRight);
                        Gizmos.DrawLine(box.frontBottomRight, box.frontBottomLeft);
                        Gizmos.DrawLine(box.frontBottomLeft, box.frontTopLeft);

                        Gizmos.DrawLine(box.backTopLeft, box.backTopRight);
                        Gizmos.DrawLine(box.backTopRight, box.backBottomRight);
                        Gizmos.DrawLine(box.backBottomRight, box.backBottomLeft);
                        Gizmos.DrawLine(box.backBottomLeft, box.backTopLeft);

                        Gizmos.DrawLine(box.frontTopLeft, box.backTopLeft);
                        Gizmos.DrawLine(box.frontTopRight, box.backTopRight);
                        Gizmos.DrawLine(box.frontBottomRight, box.backBottomRight);
                        Gizmos.DrawLine(box.frontBottomLeft, box.backBottomLeft);
                    }
                }

                else if (ShowWarnings)
                {
                    Debug.LogWarning("One or more instances of an object have not been set, debugger cannot be enabled.");
                }
            }
        }
        else
        {
            if (EnableDebugger && Hips != null)
            {
                if (ParentObject != null && LowestPointOfCharacter != null && anim != null)
                {
                    if (LeftLast)
                    {
                        Vector3 startPos = leftLegLast.position + 0.05f * Vector3.up;
                        Vector3 endPos = leftLeg.position + 0.05f * Vector3.up;
                        if (ShowStepDistance)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawLine(startPos, endPos);
                            Vector3 direc = (endPos - startPos).normalized;
                            Vector3 sideVector = v2T3(RotateVector(v3To2(direc), 90, 0)).normalized;
                            float Dist = Vector3.Distance(startPos, endPos);
                            Gizmos.DrawLine(startPos - sideVector * Dist / 8, startPos + sideVector * Dist / 8);
                            Gizmos.DrawLine(endPos - sideVector * Dist / 8, endPos + sideVector * Dist / 8);
                        }

                        if (ShowFootPath)
                        {
                            Gizmos.color = Color.cyan;
                            Vector3 LastPoint = startPos;
                            float RunWalkk = Mathf.Clamp01((Speed - walkSpeed) / (RunSpeed - walkSpeed));
                            int c = 0;
                            for (int i = 1; i <= 50; i++)
                            {
                                float raise = RaiseFootWalking.Evaluate((float)i / 50f) * RaiseFootWMultiplier + leftCurve.Evaluate((float)i / 50f);
                                raise = Mathf.Lerp(raise, RaiseFootRunning.Evaluate((float)i / 50f) * RaiseFootRMultiplier, RunWalkk);
                                Vector3 point = Vector3.Lerp(startPos, endPos, (float)i / 50f) + Vector3.up * raise;
                                if (i % 2 == 0)
                                {
                                    c = 1;
                                }
                                if (c == 0)
                                {
                                    Gizmos.DrawLine(LastPoint, point);
                                }
                                else
                                {
                                    c--;
                                }

                                LastPoint = point;
                            }
                        }
                    }
                    else
                    {
                        Vector3 startPos = rightLegLast.position + 0.05f * Vector3.up;
                        Vector3 endPos = rightLeg.position + 0.05f * Vector3.up;
                        if (ShowStepDistance)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawLine(startPos, endPos);
                            Vector3 direc = (endPos - startPos).normalized;
                            Vector3 sideVector = v2T3(RotateVector(v3To2(direc), 90, 0)).normalized;
                            float Dist = Vector3.Distance(startPos, endPos);
                            Gizmos.DrawLine(startPos - sideVector * Dist / 8, startPos + sideVector * Dist / 8);
                            Gizmos.DrawLine(endPos - sideVector * Dist / 8, endPos + sideVector * Dist / 8);
                        }

                        if (ShowFootPath)
                        {
                            Gizmos.color = Color.cyan;
                            Vector3 LastPoint = startPos;
                            float RunWalkk = Mathf.Clamp01((Speed - walkSpeed) / (RunSpeed - walkSpeed));
                            int c = 0;
                            for (int i = 1; i <= 50; i++)
                            {
                                float raise = RaiseFootWalking.Evaluate((float)i / 50f) * RaiseFootWMultiplier + rightCurve.Evaluate((float)i / 50f);
                                raise = Mathf.Lerp(raise, RaiseFootRunning.Evaluate((float)i / 50f) * RaiseFootRMultiplier, RunWalkk);
                                Vector3 point = Vector3.Lerp(startPos, endPos, (float)i / 50f) + Vector3.up * raise;
                                if (i % 2 == 0)
                                {
                                    c = 1;
                                }
                                if (c == 0)
                                {
                                    Gizmos.DrawLine(LastPoint, point);
                                }
                                else
                                {
                                    c--;
                                }

                                LastPoint = point;
                            }
                        }
                    }
                    if (ShowSmartStepRadius)
                    {
                        Quaternion RotL = leftLeg.rotation;
                        RotL = Quaternion.Euler(0, RotL.eulerAngles.y, 0);
                        Quaternion RotR = rightLeg.rotation;
                        RotR = Quaternion.Euler(0, RotR.eulerAngles.y, 0);

                        Vector3 PL = leftLeg.position;
                        Vector3 PR = rightLeg.position;

                        Vector3[] LDots = new Vector3[4];
                        LDots[0] = PL + RotL * Vector3.forward * (Scal / 2) / 7f + RotL * Vector3.right * (Scal / 2);
                        LDots[1] = PL + RotL * Vector3.forward * (Scal / 2) / 7f - RotL * Vector3.right * (Scal / 2);
                        LDots[2] = PL - RotL * Vector3.forward * (Scal / 2) / 7f + RotL * Vector3.right * (Scal / 2);
                        LDots[3] = PL - RotL * Vector3.forward * (Scal / 2) / 7f - RotL * Vector3.right * (Scal / 2);
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(LDots[0], LDots[1]);
                        Gizmos.DrawLine(LDots[2], LDots[3]);
                        Gizmos.DrawLine(LDots[0], LDots[2]);
                        Gizmos.DrawLine(LDots[3], LDots[1]);

                        Vector3[] RDots = new Vector3[4];
                        RDots[0] = PR + RotR * Vector3.forward * (Scal / 2) / 7f + RotR * Vector3.right * (Scal / 2);
                        RDots[1] = PR + RotR * Vector3.forward * (Scal / 2) / 7f - RotR * Vector3.right * (Scal / 2);
                        RDots[2] = PR - RotR * Vector3.forward * (Scal / 2) / 7f + RotR * Vector3.right * (Scal / 2);
                        RDots[3] = PR - RotR * Vector3.forward * (Scal / 2) / 7f - RotR * Vector3.right * (Scal / 2);
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(RDots[0], RDots[1]);
                        Gizmos.DrawLine(RDots[2], RDots[3]);
                        Gizmos.DrawLine(RDots[0], RDots[2]);
                        Gizmos.DrawLine(RDots[3], RDots[1]);
                    }

                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(leftLegLastL.position, 0.03f);
                    Gizmos.DrawSphere(rightLegLastL.position, 0.03f);
                }
                else if (ShowWarnings)
                {
                    Debug.LogWarning("One or more instances of an object have not been set, debugger cannot be enabled.");
                }
            }
        }
    }

    #endregion
}