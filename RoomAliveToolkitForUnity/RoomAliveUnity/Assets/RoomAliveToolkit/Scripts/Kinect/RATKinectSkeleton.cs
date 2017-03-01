using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using UnityEngine;

namespace RoomAliveToolkit
{

    /// <summary>
    ///  The result of a body's attribute being detected.
    /// </summary>
    public enum DetectionResult
    {
        // Summary:
        //     Undetermined detection.
        Unknown = 0,
        //
        // Summary:
        //     Not detected.
        No = 1,
        //
        // Summary:
        //     Maybe detected.
        Maybe = 2,
        //
        // Summary:
        //     Is detected.
        Yes = 3,
    }

    /// <summary>
    /// Bone connections for building up a topology
    /// </summary>
    public class BoneConnection
    {
        public string name { get; private set; }
        public int ID;
        public JointType fromJoint { get; private set; }
        public JointType toJoint { get; private set; }

        static int count = 0;

        public BoneConnection(string name, JointType fromJoint, JointType toJoint)
        {
            this.ID = count++;
            this.name = name;
            this.fromJoint = fromJoint;
            this.toJoint = toJoint;
        }
    }

    /// <summary>
    /// Human topology according to kinect skeleton
    /// </summary>
    public static class HumanTopology
    {
        public static string[] JOINT_NAMES = new string[]
        {
                "SpineBase",
                "SpineMid",
                "Neck",
                "Head",
                "ShoulderLeft",
                "ElbowLeft",
                "WristLeft",
                "HandLeft",
                "ShoulderRight",
                "ElbowRight",
                "WristRight",
                "HandRight",
                "HipLeft",
                "KneeLeft",
                "AnkleLeft",
                "FootLeft",
                "HipRight",
                "KneeRight",
                "AnkleRight",
                "FootRight",
                "SpineShoulder",
                "HandTipLeft",
                "ThumbLeft",
                "HandTipRight",
                "ThumbRight"
        };

        private static float WRIST_RADIUS = 0.8f;
        private static float HAND_RADIUS = 0.7f;
        private static float HAND_TIP_RADIUS = 0.6f;
        private static float THUMB_RADIUS = 0.5f;
        private static float ANKLE_RADIUS = 0.9f;
        private static float FOOT_RADIUS = 0.8f;

        public static float[] JOINT_DEBUG_WIDTHS = new float[]
        {
                1,
                1,
                1,
                1.2f,
                1,
                1,
                WRIST_RADIUS,
                HAND_RADIUS,
                1,
                1,
                WRIST_RADIUS,
                HAND_RADIUS,
                0.9f,
                1,
                ANKLE_RADIUS,
                FOOT_RADIUS,
                0.9f,
                1,
                ANKLE_RADIUS,
                FOOT_RADIUS,
                1,
                HAND_TIP_RADIUS,
                THUMB_RADIUS,
                HAND_TIP_RADIUS,
                THUMB_RADIUS
        };

        public static BoneConnection[] BONE_CONNECTIONS = new BoneConnection[]
        {
            //torso
            new BoneConnection("Head", JointType.Head, JointType.Neck),
            new BoneConnection("BackTop", JointType.Neck, JointType.SpineShoulder),
            new BoneConnection("Back", JointType.SpineShoulder, JointType.SpineMid),
            new BoneConnection("BackBottom", JointType.SpineMid, JointType.SpineBase),
            new BoneConnection("ShoulderRight", JointType.SpineShoulder, JointType.ShoulderRight),
            new BoneConnection("ShoulderLeft", JointType.SpineShoulder, JointType.ShoulderLeft),
            new BoneConnection("LegBaseRight", JointType.SpineBase, JointType.HipRight),
            new BoneConnection("LegBaseLeft", JointType.SpineBase, JointType.HipLeft),

            //rigth arm
            new BoneConnection("UpperArmRight", JointType.ShoulderRight, JointType.ElbowRight),
            new BoneConnection("LowerArmRight", JointType.ElbowRight, JointType.WristRight),
            new BoneConnection("HandRight", JointType.WristRight, JointType.HandRight),
            new BoneConnection("FingersRight", JointType.HandRight, JointType.HandTipRight),
            new BoneConnection("ThumbRight", JointType.WristRight, JointType.ThumbRight),

            //left arm
            new BoneConnection("UpperArmLeft", JointType.ShoulderLeft, JointType.ElbowLeft),
            new BoneConnection("LowerArmLeft", JointType.ElbowLeft, JointType.WristLeft),
            new BoneConnection("HandLeft", JointType.WristLeft, JointType.HandLeft),
            new BoneConnection("FingersLeft", JointType.HandLeft, JointType.HandTipLeft),
            new BoneConnection("ThumbLeft", JointType.WristLeft, JointType.ThumbLeft),

            //right leg
            new BoneConnection("UpperLegRight", JointType.HipRight, JointType.KneeRight),
            new BoneConnection("LowerLegRight", JointType.KneeRight, JointType.AnkleRight),
            new BoneConnection("FootRight", JointType.AnkleRight, JointType.FootRight),

            //left leg
            new BoneConnection("UpperLegLeft", JointType.HipLeft, JointType.KneeLeft),
            new BoneConnection("LowerLegLeft", JointType.KneeLeft, JointType.AnkleLeft),
            new BoneConnection("FootLeft", JointType.AnkleLeft, JointType.FootLeft)
        };
    }

    /// <summary>
    /// Data container for a tracked kinect skeleton with human topology
    /// </summary>
    [Serializable]
    public class RATKinectSkeleton : ISerializable
    {

        public static string GetJointName(JointType jointType)
        {
            return GetJointName((int)jointType);
        }

        public static string GetJointName(int jointIndex)
        {
            if (jointIndex < 0 || jointIndex >= 25)
                return "<unknown>";
            return HumanTopology.JOINT_NAMES[jointIndex];
        }
        [NonSerializedAttribute]
        public const int JOINT_COUNT = 25;
        [NonSerializedAttribute]
        public const int FACE_POSITION_COUNT = 5;

        public Vector3 headPos
        {
            get
            {
                return jointPositions3D[(int)JointType.Head];
            }
        }

        public bool headTracked
        {
            get
            {
                return jointStates[(int)JointType.Head] == TrackingState.Tracked;
            }
        }

        public Vector3 leftEyePos {
            get
            {
                return facePositions3D[(int)FaceType.EyeLeft];
            }
        }

        public Vector3 rightEyePos
        {
            get
            {
                return facePositions3D[(int)FaceType.EyeRight];
            }
        }

        public Vector3 nosePos
        {
            get
            {
                return facePositions3D[(int)FaceType.Nose];
            }
        }

        public Vector3 mouthLeftPos
        {
            get
            {
                return facePositions3D[(int)FaceType.MouthLeft];
            }
        }

        public Vector3 mouthRightPos
        {
            get
            {
                return facePositions3D[(int)FaceType.MouthRight];
            }
        }

        public bool leftEyeValid
        {
            get
            {
                return posValid(leftEyePos);
            }
        }

        public bool rightEyeValid
        {
            get
            {
                return posValid(rightEyePos);
            }
        }

        private bool posValid(Vector3 pos)
        {
            return pos.magnitude < 100000;
        }

        /// <summary>
        /// Helper struct to enable serialization of Unity Vector3.
        /// </summary>
        [Serializable]
        public struct SerializableVector3
        {
            public float x;
            public float y;
            public float z;

            public SerializableVector3(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public SerializableVector3(Vector3 v)
            {
                x = v.x;
                y = v.y;
                z = v.z;
            }

            public static implicit operator Vector3(SerializableVector3 v)
            {
                return new Vector3(v.x, v.y, v.z);
            }

            public static implicit operator SerializableVector3(Vector3 v)
            {
                return new SerializableVector3(v);
            }
        }

        /// <summary>
        /// Helper struct to enable serialization of Unity Quaternion.
        /// </summary>
        [Serializable]
        public struct SerializableQuaternion
        {
            public float w;
            public float x;
            public float y;
            public float z;

            public SerializableQuaternion(float w, float x, float y, float z)
            {
                this.w = w;
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public SerializableQuaternion(Quaternion q)
            {
                w = q.w;
                x = q.x;
                y = q.y;
                z = q.z;
            }

            public static implicit operator Quaternion(SerializableQuaternion q)
            {
                return new Quaternion(q.w, q.x, q.y, q.z);
            }

            public static implicit operator SerializableQuaternion(Quaternion q)
            {
                return new SerializableQuaternion(q);
            }
        }

        public bool valid = false;
        public ulong ID;
        [NonSerializedAttribute]
        public Vector3[] jointPositions3D;
        [NonSerializedAttribute]
        public Vector3[] facePositions3D;
        public Vector3 faceOrientationYPR;
        public Quaternion faceOrientation;
        //[NonSerializedAttribute]
        //public Vector3[] JointVelocities3D;
        //[NonSerializedAttribute]
        //public Vector3[] JointAccelerations3D;
        //public Quaternion[] JointOrientations3D;
        public TrackingState[] jointStates;
        public DetectionResult glasses;
        public DetectionResult happy;
        public DetectionResult engaged;
        public DetectionResult lookingAway;
        public DetectionResult leftEyeClosed;
        public DetectionResult rightEyeClosed;
        public DetectionResult mouthOpen;
        public DetectionResult mouthMoved;
        public byte handLeftConfidence;
        public byte handRightConfidence;
        public byte handLeftState;
        public byte handRightState;

        [NonSerializedAttribute]
        private int mergeCount = 0;

        public RATKinectSkeleton()
        {
            this.jointPositions3D = new Vector3[JOINT_COUNT];
            this.jointStates = new TrackingState[JOINT_COUNT];
            this.facePositions3D = new Vector3[FACE_POSITION_COUNT];
        }

        private Vector3 Vec3(SerializableVector3 v)
        {
            return new Vector3(v.x,v.y,v.z);
        }

        protected RATKinectSkeleton(SerializationInfo info, StreamingContext context) : this()
        {
            try{
                valid = info.GetBoolean("valid");
                ID = info.GetUInt64("ID");
                handLeftConfidence = info.GetByte("HandLeftConfidence");
                handRightConfidence = info.GetByte("HandRightConfidence");
                handLeftState = info.GetByte("HandLeftState");
                handRightState = info.GetByte("HandRightState");

                // Deserialize joint positions and orientations
                SerializableVector3[] jointPositions3D = (SerializableVector3[])info.GetValue("JointPositions3D", typeof(SerializableVector3[]));

                for (int jointIndex = 0; jointIndex < JOINT_COUNT; ++jointIndex)
                {
                    this.jointPositions3D[jointIndex] = (Vector3)jointPositions3D[jointIndex];
                }

                this.jointStates = (TrackingState[])info.GetValue("JointStates", typeof(RATKinectSkeleton.TrackingState[]));

                SerializableVector3[] FacePositions3D = (SerializableVector3[])info.GetValue("FacePositions3D", typeof(SerializableVector3[]));
                for (int faceIndex = 0; faceIndex < FACE_POSITION_COUNT; ++faceIndex)
                {
                    this.facePositions3D[faceIndex] = FacePositions3D.Length > faceIndex ? (Vector3)FacePositions3D[faceIndex] : Vector3.zero;
                }

                this.faceOrientationYPR = Vec3((SerializableVector3)info.GetValue("FaceOrientationYPR", typeof(SerializableVector3)));
                faceOrientation = Quaternion.Euler(faceOrientationYPR);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            try { 
                info.AddValue("valid", valid);
                info.AddValue("ID", ID);
                info.AddValue("HandLeftConfidence", handLeftConfidence);
                info.AddValue("HandRightConfidence", handRightConfidence);
                info.AddValue("HandLeftState", handLeftState);
                info.AddValue("HandRightState", handRightState);

                // Serialize joint positions and orientations
                SerializableVector3[] JointPositions3D = new SerializableVector3[JOINT_COUNT];

                for (int jointIndex = 0; jointIndex < JOINT_COUNT; ++jointIndex)
                {
                    JointPositions3D[jointIndex] = (SerializableVector3)this.jointPositions3D[jointIndex];
                }
                info.AddValue("JointPositions3D", JointPositions3D);
                info.AddValue("JointStates", jointStates);

                SerializableVector3[] FacePositions3D = new SerializableVector3[FACE_POSITION_COUNT];
                for (int faceIndex = 0; faceIndex < FACE_POSITION_COUNT; ++faceIndex)
                {
                    FacePositions3D[faceIndex] = this.facePositions3D.Length > faceIndex ? (SerializableVector3)this.facePositions3D[faceIndex] : (SerializableVector3)Vector3.zero;
                }
                info.AddValue("FacePositions3D", FacePositions3D);

                info.AddValue("FaceOrientationYPR", new SerializableVector3(faceOrientationYPR));
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        public void MergeSkeleton(RATKinectSkeleton srcData,Matrix4x4 transform)
        {
            if (!srcData.valid)
                return;

            //TODO utilize jointStates
            if (mergeCount==0)
            {
                //first skeleton, set data
                for (int i = 0; i < JOINT_COUNT; i++)
                {
                    jointPositions3D[i] = transform.MultiplyPoint(srcData.jointPositions3D[i]);
                    jointStates[i] = srcData.jointStates[i];
                }
                for (int i = 0; i < FACE_POSITION_COUNT; i++)
                {
                    facePositions3D[i] = transform.MultiplyPoint(srcData.facePositions3D[i]);
                }
                faceOrientationYPR = srcData.faceOrientationYPR;
                faceOrientation = srcData.faceOrientation;
                glasses = srcData.glasses;
                happy = srcData.happy;
                engaged = srcData.engaged;
                lookingAway = srcData.lookingAway;
                leftEyeClosed = srcData.leftEyeClosed;
                rightEyeClosed = srcData.rightEyeClosed;
                mouthOpen = srcData.mouthOpen;
                mouthMoved = srcData.mouthMoved;
            }
            else
            {
                for (int i = 0; i < JOINT_COUNT; i++)
                {
                    jointPositions3D[i] += transform.MultiplyPoint(srcData.jointPositions3D[i]);
                    if (jointStates[i] > srcData.jointStates[i])
                        jointStates[i] = srcData.jointStates[i];
                }
                for (int i = 0; i < FACE_POSITION_COUNT; i++)
                {
                    facePositions3D[i] += transform.MultiplyPoint(srcData.facePositions3D[i]);
                }
                faceOrientationYPR = srcData.faceOrientationYPR;
                faceOrientation = srcData.faceOrientation;
                glasses = srcData.glasses;
                happy = srcData.happy;
                engaged = srcData.engaged;
                lookingAway = srcData.lookingAway;
                leftEyeClosed = srcData.leftEyeClosed;
                rightEyeClosed = srcData.rightEyeClosed;
                mouthOpen = srcData.mouthOpen;
                mouthMoved = srcData.mouthMoved;
            }
            valid = true;
            mergeCount++;
        }

        public bool FinishMerging()
        {
            valid = mergeCount>0;
            if (mergeCount == 0)
                return false;
            for (int i = 0; i < JOINT_COUNT; i++)
            {
                jointPositions3D[i] /= mergeCount;
            }
            for (int i = 0; i < FACE_POSITION_COUNT; i++)
            {
                facePositions3D[i] /= mergeCount;
            }
            mergeCount = 0;
            return true;
        }

        public void CopyFrom(RATKinectSkeleton templateSkel,Matrix4x4 transform)
        {
            mergeCount = 0;
            MergeSkeleton(templateSkel, transform);
            FinishMerging();
        }


        /// <summary>
        /// The confidence level of a body's tracked attribute.
        /// </summary>
        public enum TrackingConfidence
        {
            // Summary:
            //     Low confidence.
            Low = 0,
            //
            // Summary:
            //     High confidence.
            High = 1,
        }

        /// <summary>
        /// The state of a hand of a body.
        /// </summary>  
        public enum HandState
        {
            // Summary:
            //     Undetermined hand state.
            Unknown = 0,
            //
            // Summary:
            //     Hand not tracked.
            NotTracked = 1,
            //
            // Summary:
            //     Open hand.
            Open = 2,
            //
            // Summary:
            //     Closed hand.
            Closed = 3,
            //
            // Summary:
            //     Lasso (pointer) hand.
            Lasso = 4,
        }

        /// <summary>
        /// The state of tracking a body or body's attribute. Copied from Microsoft.Kinect
        /// </summary> 
        public enum TrackingState
        {
            // Summary:
            //     Not tracked.
            NotTracked = 0,
            //
            // Summary:
            //     Inferred.
            Inferred = 1,
            //
            // Summary:
            //     Tracked.
            Tracked = 2,
        }

    }


    /// <summary>
    /// The types of joints of a Body. Copied from Microsoft.Kinect
    /// </summary> 
    public enum JointType
    {
        // Summary:
        //     Base of the spine.
        SpineBase = 0,
        //
        // Summary:
        //     Middle of the spine.
        SpineMid = 1,
        //
        // Summary:
        //     Neck.
        Neck = 2,
        //
        // Summary:
        //     Head.
        Head = 3,
        //
        // Summary:
        //     Left shoulder.
        ShoulderLeft = 4,
        //
        // Summary:
        //     Left elbow.
        ElbowLeft = 5,
        //
        // Summary:
        //     Left wrist.
        WristLeft = 6,
        //
        // Summary:
        //     Left hand.
        HandLeft = 7,
        //
        // Summary:
        //     Right shoulder.
        ShoulderRight = 8,
        //
        // Summary:
        //     Right elbow.
        ElbowRight = 9,
        //
        // Summary:
        //     Right wrist.
        WristRight = 10,
        //
        // Summary:
        //     Right hand.
        HandRight = 11,
        //
        // Summary:
        //     Left hip.
        HipLeft = 12,
        //
        // Summary:
        //     Left knee.
        KneeLeft = 13,
        //
        // Summary:
        //     Left ankle.
        AnkleLeft = 14,
        //
        // Summary:
        //     Left foot.
        FootLeft = 15,
        //
        // Summary:
        //     Right hip.
        HipRight = 16,
        //
        // Summary:
        //     Right knee.
        KneeRight = 17,
        //
        // Summary:
        //     Right ankle.
        AnkleRight = 18,
        //
        // Summary:
        //     Right foot.
        FootRight = 19,
        //
        // Summary:
        //     Between the shoulders on the spine.
        SpineShoulder = 20,
        //
        // Summary:
        //     Tip of the left hand.
        HandTipLeft = 21,
        //
        // Summary:
        //     Left thumb.
        ThumbLeft = 22,
        //
        // Summary:
        //     Tip of the right hand.
        HandTipRight = 23,
        //
        // Summary:
        //     Right thumb.
        ThumbRight = 24,
        //
        // Summary:
        //     The number of JointType values.
        Count = 25,
    }

    /// <summary>
    /// The types of face features of a Body
    /// </summary>
    public enum FaceType
    {
        EyeLeft = 0,
        EyeRight = 1,
        Nose = 2,
        MouthLeft = 3,
        MouthRight = 4,
    }

}
