using UnityEngine;
using System.Collections;
using System;

namespace RoomAliveToolkit
{
    /// <summary>
    /// An abstract class that provides the basic functionality for handling and drawing Kinect skeletons in the Unity Editor. 
    /// </summary>
    public abstract class RATSkeletonProvider : MonoBehaviour
    {
        //parameters for Skeleton Debug view (in Scene)
        protected Color skeletonColor = Color.red;
        protected Color faceColor = Color.yellow;
        protected float jointRadius = 0.025f;
        protected float eyeRadius = 0.015f;
        protected float noseRadius = 0.01f;
        protected float mouthRadius = 0.008f;
        protected int lineWidth = 2;

        public bool showSkeletons = true;

        public abstract RATKinectSkeleton GetKinectSkeleton(int n);
        public abstract Vector3 KinectToWorld(Vector3 pos);
        public abstract int GetMaxBodiesCount();

        private void drawEye(Vector3 pos,DetectionResult closed, Quaternion rotation)
        {
            Color color = faceColor;
            if (closed==DetectionResult.Unknown)
                color.a *= 0.4f;
            if (closed == DetectionResult.Maybe)
                color.a *= 0.8f;
            Gizmos.color = color;
            if (closed != DetectionResult.Yes)
                Gizmos.DrawSphere(pos, eyeRadius);
            else {
                Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.TRS(pos, rotation, new Vector3(0.9f, 0.2f, 0.9f));
                Gizmos.DrawSphere(Vector3.zero, eyeRadius);
                Gizmos.matrix = transform.localToWorldMatrix;
            }
            
        }

        protected virtual void OnDrawGizmos()
        {
            if(Application.isPlaying && showSkeletons)
            {
                
                int maxBodies = GetMaxBodiesCount();
                Gizmos.matrix = transform.localToWorldMatrix;
                for (int n = 0; n < maxBodies; n++)
                {
                    RATKinectSkeleton skeleton = GetKinectSkeleton(n);
                    if (skeleton != null && (skeleton.valid ))
                    {
                        //Draw body
                        for (int i = 0; i < skeleton.jointPositions3D.Length; i++)
                        {
                            RATKinectSkeleton.TrackingState state = skeleton.jointStates[i];
                            bool tracked = state != RATKinectSkeleton.TrackingState.NotTracked;
                            if(tracked)
                            {
                                Color cl = skeletonColor;
                                if (state != RATKinectSkeleton.TrackingState.Tracked)
                                    cl.a *= 0.3f;
                                Gizmos.color = cl;
                                Gizmos.DrawSphere(skeleton.jointPositions3D[i], jointRadius * HumanTopology.JOINT_DEBUG_WIDTHS[i]);
                            }
                        }
                        for (int i = 0; i < HumanTopology.BONE_CONNECTIONS.Length; i++)
                        {
                            BoneConnection bone = HumanTopology.BONE_CONNECTIONS[i];
                            JointType joint1 = bone.fromJoint;
                            JointType joint2 = bone.toJoint;
                            RATKinectSkeleton.TrackingState state1 = skeleton.jointStates[(int)joint1];
                            RATKinectSkeleton.TrackingState state2 = skeleton.jointStates[(int)joint2];
                            bool tracked = state1 != RATKinectSkeleton.TrackingState.NotTracked && state2 != RATKinectSkeleton.TrackingState.NotTracked;
                            if(tracked)
                            {
                                Color cl = skeletonColor;
                                if (state1 != RATKinectSkeleton.TrackingState.Tracked || state2 != RATKinectSkeleton.TrackingState.Tracked)
                                    cl.a *= 0.3f;
                                Gizmos.color = cl;
                                Gizmos.DrawLine(skeleton.jointPositions3D[(int)joint1], skeleton.jointPositions3D[(int)joint2]);
                            }
                        }

                        //Draw face
                        drawEye(skeleton.leftEyePos, skeleton.leftEyeClosed, skeleton.faceOrientation);
                        drawEye(skeleton.rightEyePos, skeleton.rightEyeClosed, skeleton.faceOrientation);
                        Gizmos.color = faceColor;
                        Gizmos.DrawLine(skeleton.leftEyePos, skeleton.rightEyePos);
                        Gizmos.DrawSphere(skeleton.nosePos, noseRadius);
                        Gizmos.DrawSphere(skeleton.mouthLeftPos, mouthRadius);
                        Gizmos.DrawSphere(skeleton.mouthRightPos, mouthRadius);
                    }
                }
                
                Gizmos.matrix = Matrix4x4.identity;
            }
        }

    }

}


