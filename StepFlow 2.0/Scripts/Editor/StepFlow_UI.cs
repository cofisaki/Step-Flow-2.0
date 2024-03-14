using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StepFlow))]
public class StepFlow_UI : Editor
{
    private string[] TabsD = { "Parameters", "Debugger" };
    private string[] Tabs = {"Movement", "Collision", "Animation", "Upper Body"};
    private int tabIndex;
    private int tabIndexD;
    private static bool showFoldout = false;


    public override void OnInspectorGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 50;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;
        EditorGUILayout.LabelField("Step Flow", style, GUILayout.Height(60));

        //GUILayout.Box(tex, GUILayout.Width(100));
        EditorGUILayout.BeginHorizontal();
        tabIndexD = GUILayout.Toolbar(tabIndexD, TabsD);
        EditorGUILayout.EndHorizontal();

        if (tabIndexD == 0)
        {
            EditorGUILayout.BeginHorizontal();
            tabIndex = GUILayout.Toolbar(tabIndex, Tabs);
            EditorGUILayout.EndHorizontal();
        }

        if (tabIndexD == 0)
        {
            switch (tabIndex)
            {
                case 0:
                    Mov();
                    break;
                case 1:
                    Col();
                    break;
                case 2:
                    Anim();
                    break;
                case 3:
                    Upper();
                    break;
                default:
                    Def();
                    break;
            }
        }
        else
        {
            MovDebugger();
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }

    }

    private void Mov()
    {
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;
        StepFlow stepFlow = (StepFlow)target;
        SerializedObject serializedStepFlow = new SerializedObject(stepFlow);

        EditorGUILayout.Space(5);
        stepFlow.ShowWarnings = EditorGUILayout.Toggle("Show Script Warnings", stepFlow.ShowWarnings);
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Main Properties", EditorStyles.boldLabel);
        SerializedProperty parentObjectProperty = serializedStepFlow.FindProperty("ParentObject");
        EditorGUILayout.PropertyField(parentObjectProperty, new GUIContent("Parent Object"), true);
        parentObjectProperty = serializedStepFlow.FindProperty("LowestPointOfCharacter");
        EditorGUILayout.PropertyField(parentObjectProperty, new GUIContent("Lowest Point Of Character"), true);
        parentObjectProperty = serializedStepFlow.FindProperty("Hips");
        EditorGUILayout.PropertyField(parentObjectProperty, new GUIContent("Hips"), true);
        parentObjectProperty = serializedStepFlow.FindProperty("mask");
        EditorGUILayout.PropertyField(parentObjectProperty, new GUIContent("Layer Mask"), true);

        EditorGUILayout.Space(9);

        EditorGUILayout.LabelField("Smoothing Parameters", EditorStyles.boldLabel);
        stepFlow.DirectionLerpSpeed = EditorGUILayout.FloatField("Direction Lerp Speed", stepFlow.DirectionLerpSpeed);
        stepFlow.SpeedLerpSpeed = EditorGUILayout.FloatField("Speed Lerp", stepFlow.SpeedLerpSpeed);
        stepFlow.MaximumStepTime = EditorGUILayout.FloatField("Maximum Step Time", stepFlow.MaximumStepTime);

        EditorGUILayout.Space(9);

        EditorGUILayout.LabelField("Stepping Parameters", EditorStyles.boldLabel);
        stepFlow.StepDistanceMultiplier = EditorGUILayout.FloatField("Step Distance Multiplier", stepFlow.StepDistanceMultiplier);
        stepFlow.MinStepDistance = EditorGUILayout.FloatField("Min Step Distance", stepFlow.MinStepDistance);
        stepFlow.MaxStepDistance = EditorGUILayout.FloatField("Max Step Distance", stepFlow.MaxStepDistance);

        stepFlow.ForwardMultiplier = EditorGUILayout.FloatField("Forward Multiplier", stepFlow.ForwardMultiplier);
        stepFlow.AdaptiveForwardMultiplier = EditorGUILayout.Toggle("Adaptive Forward Multiplier", stepFlow.AdaptiveForwardMultiplier);
        if (stepFlow.AdaptiveForwardMultiplier)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.Space(8);
            stepFlow.AMaxAngle = EditorGUILayout.FloatField("Maximum Angle", stepFlow.AMaxAngle);
            EditorGUILayout.Space(5);
            stepFlow.UDMultiplier = EditorGUILayout.Vector2Field("", stepFlow.UDMultiplier);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Uphill", style);
            EditorGUILayout.LabelField("Downhill", style);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8);
            EditorGUI.indentLevel--;
        }

        stepFlow.maxStepHeight = EditorGUILayout.FloatField("Max Step Height", stepFlow.maxStepHeight);
        stepFlow.SmartStep = EditorGUILayout.Toggle("Smart Step", stepFlow.SmartStep);
        if (stepFlow.SmartStep)
        {
            EditorGUI.indentLevel++;
            stepFlow.Qual = EditorGUILayout.IntField("Quality", stepFlow.Qual);
            stepFlow.Scal = EditorGUILayout.FloatField("Check Area Size", stepFlow.Scal);
            stepFlow.DistanceWeight = EditorGUILayout.FloatField("Distance Weight", stepFlow.DistanceWeight);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(9);

        EditorGUILayout.LabelField("Resting Parameters", EditorStyles.boldLabel);
        stepFlow.RotationError = EditorGUILayout.FloatField("RotationError", stepFlow.RotationError);
        stepFlow.settleFeetSpeed = EditorGUILayout.FloatField("Settle Position Speed", stepFlow.settleFeetSpeed);
        stepFlow.MaxFeetAngle = EditorGUILayout.FloatField("Max Steepnes Angle", stepFlow.MaxFeetAngle);

        serializedStepFlow.ApplyModifiedProperties();
    }

    private void Col()
    {
        StepFlow stepFlow = (StepFlow)target;
        SerializedObject serializedStepFlow = new SerializedObject(stepFlow);
        SerializedProperty parentObjectProperty;

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Placement and Bounds", EditorStyles.boldLabel);
        stepFlow.ShowLegBounds = EditorGUILayout.Toggle("Show Leg Bounds", stepFlow.ShowLegBounds);

        EditorGUI.indentLevel++;
        if (stepFlow.ShowLegBounds)
        {
            showFoldout = EditorGUILayout.Foldout(showFoldout, "Feet Parameters");
            if (showFoldout)
            {
                EditorGUI.indentLevel++;
                parentObjectProperty = serializedStepFlow.FindProperty("leftLegBounds");
                EditorGUILayout.PropertyField(parentObjectProperty, new GUIContent("Left Leg Bounds"), true);

                parentObjectProperty = serializedStepFlow.FindProperty("rightLegBounds");
                EditorGUILayout.PropertyField(parentObjectProperty, new GUIContent("Right Leg Bounds"), true);

                parentObjectProperty = serializedStepFlow.FindProperty("LeftLegPlacement");
                EditorGUILayout.PropertyField(parentObjectProperty, new GUIContent("Left Leg Placement"), true);

                parentObjectProperty = serializedStepFlow.FindProperty("RightLegPlacement");
                EditorGUILayout.PropertyField(parentObjectProperty, new GUIContent("Right Leg Placement"), true);

                EditorGUI.indentLevel--;
            }
        }
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(9);

        EditorGUILayout.LabelField("Colision Parameters", EditorStyles.boldLabel);
        stepFlow.PredictQ = EditorGUILayout.IntField("Step Distance Prediction Quality", stepFlow.PredictQ);
        stepFlow.AvoidObstacles = EditorGUILayout.Toggle("Avoid Obstacles", stepFlow.AvoidObstacles);
        EditorGUI.indentLevel++;
        if (stepFlow.AvoidObstacles)
        {
            stepFlow.ColisionQuality = EditorGUILayout.IntField("Collision Quality", stepFlow.ColisionQuality);
            stepFlow.SmoothSteppingOver = EditorGUILayout.Toggle("Smooth Stepping Over", stepFlow.SmoothSteppingOver);
            if (stepFlow.SmoothSteppingOver)
            {
                stepFlow.SmoothingQuality = EditorGUILayout.IntField("Smoothing Quality", stepFlow.SmoothingQuality);
            }
        }
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(9);
        EditorGUILayout.LabelField("Height Adjustments", EditorStyles.boldLabel);
        stepFlow.FeetHeight = EditorGUILayout.FloatField("Feet Height", stepFlow.FeetHeight);
        stepFlow.UpdateHips = EditorGUILayout.Toggle("Update Hips Height", stepFlow.UpdateHips);
        if (stepFlow.UpdateHips)
        {
            stepFlow.HipsHeight = EditorGUILayout.Vector2Field("Hips Height", stepFlow.HipsHeight);
            stepFlow.HipsMovementSpeed = EditorGUILayout.FloatField("Hips Lerp Speed", stepFlow.HipsMovementSpeed);
            stepFlow.HipsUpDownMultiplier = EditorGUILayout.FloatField("Hips Bobbing Multiplier", stepFlow.HipsUpDownMultiplier);
        }
        serializedStepFlow.ApplyModifiedProperties();
    }

    private void Anim()
    {
        StepFlow stepFlow = (StepFlow)target;
        SerializedObject serializedStepFlow = new SerializedObject(stepFlow);

        EditorGUILayout.Space(5);

        SerializedProperty parentObjectProperty = serializedStepFlow.FindProperty("anim");
        EditorGUILayout.PropertyField(parentObjectProperty, new GUIContent("Animator"), true);

        EditorGUILayout.LabelField("State Speeds", EditorStyles.boldLabel);
        stepFlow.walkSpeed = EditorGUILayout.FloatField("Walk Speed", stepFlow.walkSpeed);
        stepFlow.RunSpeed = EditorGUILayout.FloatField("Run Speed", stepFlow.RunSpeed);

        EditorGUILayout.Space(9);

        EditorGUILayout.LabelField("Hips And Heel Control", EditorStyles.boldLabel);
        stepFlow.animateHips = EditorGUILayout.Toggle("Animate Hips", stepFlow.animateHips);
        if (stepFlow.animateHips)
        {
            EditorGUI.indentLevel++;
            stepFlow.HipsRotMultiplier = EditorGUILayout.Vector2Field("Hips Rotation Multiplier", stepFlow.HipsRotMultiplier);
            EditorGUI.indentLevel--;
        }
        stepFlow.bendToes = EditorGUILayout.Toggle("Bend Toes", stepFlow.bendToes);
        if (stepFlow.bendToes)
        {
            EditorGUI.indentLevel++;
            stepFlow.bendOnStep = EditorGUILayout.Vector2Field("Bending Multiplier", stepFlow.bendOnStep);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(9);

        EditorGUILayout.LabelField("Mid Air Foot Control", EditorStyles.boldLabel);
        stepFlow.RotationForWalking = EditorGUILayout.CurveField("Rotation For Walking", stepFlow.RotationForWalking);
        stepFlow.RotationForRunning = EditorGUILayout.CurveField("Rotation For Running", stepFlow.RotationForRunning);
        stepFlow.RaiseFootWalking = EditorGUILayout.CurveField("Raise Foot Walking", stepFlow.RaiseFootWalking);
        stepFlow.RaiseFootRunning = EditorGUILayout.CurveField("Raise Foot Running", stepFlow.RaiseFootRunning);
        EditorGUILayout.Space(5);

        stepFlow.RotationWMultiplier = EditorGUILayout.FloatField("Rotation For Walking Multiplier", stepFlow.RotationWMultiplier);
        stepFlow.RotationRMultiplier = EditorGUILayout.FloatField("Rotation For Running Multiplier", stepFlow.RotationRMultiplier);
        stepFlow.RaiseFootWMultiplier = EditorGUILayout.FloatField("Raise Foot Walking Multiplier", stepFlow.RaiseFootWMultiplier);
        stepFlow.RaiseFootRMultiplier = EditorGUILayout.FloatField("Raise Foot Running Multiplier", stepFlow.RaiseFootRMultiplier);


        EditorGUILayout.Space(9);
        EditorGUILayout.LabelField("WeightParameters", EditorStyles.boldLabel);
        parentObjectProperty = serializedStepFlow.FindProperty("positionWeight");
        EditorGUILayout.PropertyField(parentObjectProperty, new GUIContent("Position Weight"), true);

        parentObjectProperty = serializedStepFlow.FindProperty("rotationWeight");
        EditorGUILayout.PropertyField(parentObjectProperty, new GUIContent("Rotation Weight"), true);

        parentObjectProperty = serializedStepFlow.FindProperty("HintWeight");
        EditorGUILayout.PropertyField(parentObjectProperty, new GUIContent("Hint Weight"), true);

        serializedStepFlow.ApplyModifiedProperties();
    }

    private void Upper()
    {
        StepFlow stepFlow = (StepFlow)target;
        SerializedObject serializedStepFlow = new SerializedObject(stepFlow);

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Spine Parameters", EditorStyles.boldLabel);
        stepFlow.animateSpine = EditorGUILayout.Toggle("Animate Spine", stepFlow.animateSpine);
        if (stepFlow.animateSpine)
        {
            EditorGUI.indentLevel++;
            stepFlow.SpineMultiplier = EditorGUILayout.Vector2Field("Spine Bend Multiplier", stepFlow.SpineMultiplier);
            stepFlow.LeanMultiplier = EditorGUILayout.Vector2Field("Lean Multiplier", stepFlow.LeanMultiplier);
            stepFlow.Spine_Rotate_Amplitude = EditorGUILayout.Vector2Field("Spine Y Rotation", stepFlow.Spine_Rotate_Amplitude);

            stepFlow.AlignHead = EditorGUILayout.Toggle("Align Head", stepFlow.AlignHead);
            if (stepFlow.AlignHead)
            {
                SerializedProperty parentObjectProperty = serializedStepFlow.FindProperty("HeadPercentage");
                EditorGUILayout.PropertyField(parentObjectProperty, new GUIContent("Head Align Percentage"), true);
            }
        }
        EditorGUILayout.Space(9);
        EditorGUILayout.LabelField("Arm Parameters", EditorStyles.boldLabel);

        stepFlow.SwayArms = EditorGUILayout.Toggle("Move Arms", stepFlow.SwayArms);
        if (stepFlow.SwayArms)
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 15;
            style.alignment = TextAnchor.MiddleCenter;


            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Walking", style);
            EditorGUILayout.LabelField("Running", style);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
            Rect rect;
            rect = EditorGUILayout.GetControlRect(false, 1);

            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));

            EditorGUILayout.Space(9);
            EditorGUILayout.LabelField("Shoulder Values", style);
            EditorGUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();

            stepFlow.ShoulderValues.x = EditorGUILayout.FloatField(stepFlow.ShoulderValues.x);
            stepFlow.ShoulderValues.y = EditorGUILayout.FloatField(stepFlow.ShoulderValues.y);
            stepFlow.ShoulderValues.z = EditorGUILayout.FloatField(stepFlow.ShoulderValues.z);
            stepFlow.ShoulderValues.w = EditorGUILayout.FloatField(stepFlow.ShoulderValues.w);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(9);
            EditorGUILayout.LabelField("Arm Values", style);
            EditorGUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();

            stepFlow.ArmValues.x = EditorGUILayout.FloatField(stepFlow.ArmValues.x);
            stepFlow.ArmValues.y = EditorGUILayout.FloatField(stepFlow.ArmValues.y);
            stepFlow.ArmValues.z = EditorGUILayout.FloatField(stepFlow.ArmValues.z);
            stepFlow.ArmValues.w = EditorGUILayout.FloatField(stepFlow.ArmValues.w);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(9);
            EditorGUILayout.LabelField("Forearm Values", style);
            EditorGUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();

            stepFlow.ForearmValues.x = EditorGUILayout.FloatField(stepFlow.ForearmValues.x);
            stepFlow.ForearmValues.y = EditorGUILayout.FloatField(stepFlow.ForearmValues.y);
            stepFlow.ForearmValues.z = EditorGUILayout.FloatField(stepFlow.ForearmValues.z);
            stepFlow.ForearmValues.w = EditorGUILayout.FloatField(stepFlow.ForearmValues.w);

            EditorGUILayout.EndHorizontal();


            EditorGUILayout.Space(9);
            EditorGUILayout.LabelField("Bend Inward", style);
            EditorGUILayout.Space(3);
            EditorGUILayout.BeginHorizontal();

            stepFlow.BendInward.x = EditorGUILayout.FloatField(stepFlow.BendInward.x);
            stepFlow.BendInward.y = EditorGUILayout.FloatField(stepFlow.BendInward.y);
            stepFlow.BendInward.z = EditorGUILayout.FloatField(stepFlow.BendInward.z);
            stepFlow.BendInward.w = EditorGUILayout.FloatField(stepFlow.BendInward.w);

            EditorGUILayout.EndHorizontal();
        }

        serializedStepFlow.ApplyModifiedProperties();
    }

    private void Def()
    {
        DrawDefaultInspector();
    }

    private void MovDebugger()
    {
        StepFlow stepFlow = (StepFlow)target;
        SerializedObject serializedStepFlow = new SerializedObject(stepFlow);

        EditorGUILayout.Space(5);
        stepFlow.EnableDebugger = EditorGUILayout.Toggle("Enable Debugger", stepFlow.EnableDebugger);

        if (stepFlow.EnableDebugger)
        {
            EditorGUI.indentLevel++;
            stepFlow.ShowStepDistance = EditorGUILayout.Toggle("Show Step Distance", stepFlow.ShowStepDistance);
            stepFlow.ShowFootPath = EditorGUILayout.Toggle("Show Foot Path", stepFlow.ShowFootPath);

            if (stepFlow.ShowStepDistance || stepFlow.ShowFootPath)
            {
                EditorGUI.indentLevel++;
                stepFlow.MoveSpeed = EditorGUILayout.FloatField("Move Speed", stepFlow.MoveSpeed);
                EditorGUI.indentLevel--;
            }

            stepFlow.ShowSmartStepRadius = EditorGUILayout.Toggle("Show Smart Step Radius", stepFlow.ShowSmartStepRadius);
            stepFlow.ShowFootPlacement = EditorGUILayout.Toggle("Show Foot Placement", stepFlow.ShowFootPlacement);
        }
    }
}
