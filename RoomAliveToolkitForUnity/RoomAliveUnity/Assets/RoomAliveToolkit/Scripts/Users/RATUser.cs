using UnityEngine;

namespace RoomAliveToolkit
{
    /// <summary>
    /// Behavior which defines a user in the RoomAlive scene. The user's position in the scene is usually updated from Kinect data. 
    /// Usually, the user will also have an acompanying RATUserViewCamera for rendering user specific views. 
    /// </summary>
    [AddComponentMenu("RoomAliveToolkit/RATUser")]
    public class RATUser : MonoBehaviour
    { 
        public RATSkeletonProvider skeletonProvider;
        public int skeletonID = 0; //each Kinect has 6 possible skeleton slots, this number indicates which spot one would request (not quite the same as a User ID


        public bool updateFromKinect = true;
        public GameObject lookAt = null; //always in world coordinates (not local to Kinect data)


        public void Start()
        {
            if (skeletonProvider == null)
            {
                Debug.Log("User is missing a skeleton provider!");
                return;
            }
        }

        public RATKinectSkeleton GetSkeleton()
        {
            if (skeletonProvider == null)
                return null;
            else
            {
                return skeletonProvider.GetKinectSkeleton(skeletonID);
            }
                
        }

        public bool IsReady()
        {
            RATKinectSkeleton skel = GetSkeleton();
            return skel != null && skel.valid;
        }

        public Vector3 getHeadPosition()
        {
            if (IsReady())
            {
                Vector3 pos = GetSkeleton().jointPositions3D[(int)JointType.Head]; // this is reported in the coordinate system of the skeleton provider
                return skeletonProvider.transform.localToWorldMatrix.MultiplyPoint(pos); //this moves it to world coordinates
            }
            else
                return Vector3.zero;
        }

        public void Update()
        {

            if (skeletonProvider != null)
            {
                RATKinectSkeleton skeleton = GetSkeleton();

                if (skeleton!=null && skeleton.valid)
                {
                    if (updateFromKinect)
                    {

                        transform.position = getHeadPosition(); 
                        if (lookAt != null)
                        {
                            transform.LookAt(lookAt.transform.position);
                        }
                        //else transform.localRotation = Quaternion.identity;
                    }

                }
            }

        }
    }
}
