using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]
public class CustomAvatarController : MonoBehaviour
{
    [Tooltip("Index of the player, tracked by this component. 0 means the 1st player, 1 - the 2nd one, etc.")]
    public int playerIndex = 0;

    [Tooltip("Whether the avatar is facing the player or not.")]
    public bool mirroredMovement = false;

    [Tooltip("Whether the avatar is allowed to move vertically.")]
    public bool verticalMovement = true;

    [Tooltip("Whether the avatar is allowed to move horizontally.")]
    public bool horizontalMovement = true;

    [Header("Custom Tracking Settings")]
    [Tooltip("If true, the avatar stays in its exact last pose and position when tracking is lost.")]
    public bool keepLastPositionOnLost = true;

    [Tooltip("If true, the avatar starts exactly where you placed it in the Editor, using relative movement.")]
    public bool startFromEditorPosition = true;

    // Internal tracking variables
    private Vector3 initialKinectUserPos = Vector3.zero;
    private bool hasCapturedInitialPos = false;

    // Standard Kinect variables
    private KinectManager kinectManager;
    private int bonesTotal;
    private Transform[] bones;
    private Quaternion[] initialRotations;
    private Quaternion[] localRotations;
    private bool isRigged = false;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 offsetNodePos;
    private Animator animatorComponent;

    void Awake()
    {
        // Get animator and initialize bones
        animatorComponent = GetComponent<Animator>();
        MapBones();
        
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        offsetNodePos = transform.position;
    }

    void Update()
    {
        if (kinectManager == null)
        {
            kinectManager = KinectManager.Instance;
        }

        if (kinectManager && kinectManager.IsInitialized())
        {
            if (kinectManager.IsUserDetected(playerIndex))
            {
                long userId = kinectManager.GetUserIdByIndex(playerIndex);
                MoveAvatar(userId);

                for (int i = 0; i < bonesTotal; i++)
                {
                    TransformBone(userId, i);
                }
            }
            else
            {
                // User lost - trigger our custom reset logic
                ResetToInitialPosition();
            }
        }
    }

    protected void MoveAvatar(long UserID)
    {
        if (!kinectManager || !isRigged) return;

        // Get the absolute physical position of the user from the Kinect
        Vector3 trans = kinectManager.GetUserPosition(UserID);

        // --- NEW LOGIC: RELATIVE MOVEMENT ---
        if (startFromEditorPosition)
        {
            if (!hasCapturedInitialPos)
            {
                // Capture the exact physical distance the moment they step into frame
                initialKinectUserPos = trans;
                hasCapturedInitialPos = true;
            }
            // Subtract that physical distance so the avatar stays at its Unity Editor origin
            trans -= initialKinectUserPos;
        }
        // ------------------------------------

        if (mirroredMovement)
        {
            trans.x = -trans.x;
            trans.z = -trans.z;
        }

        if (!verticalMovement)
        {
            trans.y = 0;
        }

        if (!horizontalMovement)
        {
            trans.x = 0;
            trans.z = 0;
        }

        // Apply the calculated movement to the Avatar's root
        transform.position = initialPosition + trans;
    }

    protected void TransformBone(long userId, int boneIndex)
    {
        if (!kinectManager || !isRigged || bones[boneIndex] == null) return;

        int joint = boneIndex;
        if (kinectManager.IsJointTracked(userId, joint))
        {
            Quaternion kinectRotation = kinectManager.GetJointOrientation(userId, joint, !mirroredMovement);
            
            // Apply rotation to the Mixamo skeleton
            Quaternion newRotation = kinectRotation * initialRotations[boneIndex];
            bones[boneIndex].rotation = Quaternion.Slerp(bones[boneIndex].rotation, newRotation, Time.deltaTime * 20f);
        }
    }

    public void ResetToInitialPosition()
    {
        // Reset the relative movement tracker so the next user gets a fresh start
        hasCapturedInitialPos = false;

        // --- NEW LOGIC: FREEZE POSITION ON DISCONNECT ---
        if (keepLastPositionOnLost)
        {
            // Abort the reset process immediately. The avatar will freeze exactly where it is.
            return; 
        }
        // ------------------------------------------------

        if (!isRigged) return;

        // Standard Reset logic (only runs if keepLastPositionOnLost is FALSE)
        transform.position = initialPosition;
        transform.rotation = initialRotation;

        for (int i = 0; i < bonesTotal; i++)
        {
            if (bones[i] != null)
            {
                bones[i].rotation = initialRotations[i];
            }
        }
    }

    // Maps the Kinect Joints to the Mixamo Humanoid Animator
    private void MapBones()
    {
        bonesTotal = 25; // Standard Kinect v2 joint count
        bones = new Transform[bonesTotal];
        initialRotations = new Quaternion[bonesTotal];
        localRotations = new Quaternion[bonesTotal];

        if (animatorComponent == null) return;

        // Map Animator bones to Kinect joint indices array
        bones[0] = animatorComponent.GetBoneTransform(HumanBodyBones.Hips);
        bones[1] = animatorComponent.GetBoneTransform(HumanBodyBones.Spine);
        bones[2] = animatorComponent.GetBoneTransform(HumanBodyBones.Neck);
        bones[3] = animatorComponent.GetBoneTransform(HumanBodyBones.Head);
        
        bones[4] = animatorComponent.GetBoneTransform(HumanBodyBones.LeftShoulder);
        bones[5] = animatorComponent.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        bones[6] = animatorComponent.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        bones[7] = animatorComponent.GetBoneTransform(HumanBodyBones.LeftHand);
        
        bones[8] = animatorComponent.GetBoneTransform(HumanBodyBones.RightShoulder);
        bones[9] = animatorComponent.GetBoneTransform(HumanBodyBones.RightUpperArm);
        bones[10] = animatorComponent.GetBoneTransform(HumanBodyBones.RightLowerArm);
        bones[11] = animatorComponent.GetBoneTransform(HumanBodyBones.RightHand);
        
        bones[12] = animatorComponent.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        bones[13] = animatorComponent.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        bones[14] = animatorComponent.GetBoneTransform(HumanBodyBones.LeftFoot);
        bones[15] = animatorComponent.GetBoneTransform(HumanBodyBones.LeftToes);
        
        bones[16] = animatorComponent.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        bones[17] = animatorComponent.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        bones[18] = animatorComponent.GetBoneTransform(HumanBodyBones.RightFoot);
        bones[19] = animatorComponent.GetBoneTransform(HumanBodyBones.RightToes);

        for (int i = 0; i < bonesTotal; i++)
        {
            if (bones[i] != null)
            {
                initialRotations[i] = bones[i].rotation;
                localRotations[i] = bones[i].localRotation;
            }
        }
        isRigged = true;
    }
}