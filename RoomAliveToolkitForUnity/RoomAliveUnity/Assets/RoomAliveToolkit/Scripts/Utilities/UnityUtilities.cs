using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RoomAliveToolkit
{
    public static class UnityUtilities
    {

        #region Matrix Extensions
        /// <summary>
        /// Extract translation from transform matrix.
        /// </summary>
        /// <param name="matrix">Transform matrix.</param>
        /// <returns>
        /// Translation offset.
        /// </returns>
        public static Vector3 ExtractTranslation(this Matrix4x4 matrix)
        {
            return matrix.GetColumn(3);
        }

        /// <summary>
        /// Extract rotation quaternion from transform matrix.
        /// </summary>
        /// <param name="matrix">Transform matrix. </param>
        /// <returns>
        /// Quaternion representation of rotation transform.
        /// </returns>
        public static Quaternion ExtractRotation(this Matrix4x4 matrix)
        {
            return Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
        }

        /// <summary>
        /// Extract scale from transform matrix.
        /// </summary>
        /// <param name="matrix">Transform matrix. </param>
        /// <returns>
        /// Scale vector.
        /// </returns>
        public static Vector3 ExtractScale(this Matrix4x4 matrix)
        {
            Vector3 scale;
            scale.x = matrix.GetColumn(0).magnitude;
            scale.y = matrix.GetColumn(1).magnitude;
            scale.z = matrix.GetColumn(2).magnitude;
            return scale;
        }



        /// <summary>
        /// Flips the right handed matrix to left handed matrix by inverting X coordinate.
        /// </summary>
        /// <param name="inputRHMatrix">Right hand coordinate matrix</param>
        /// <returns>Left hand coordinate matrix</returns>
        public static Matrix4x4 ConvertRHtoLH(Matrix4x4 inputRHMatrix)
        {
            Matrix4x4 flipRHtoLH = Matrix4x4.identity;
            flipRHtoLH[0, 0] = -1;
            return flipRHtoLH * inputRHMatrix * flipRHtoLH;
        }


        #endregion Matrix Extensions

    

    }

    #region ReadOnly Attribute
    [AttributeUsage(AttributeTargets.Field, Inherited = true)]
    public class ReadOnlyAttribute : PropertyAttribute
    {

    }
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]

#if UNITY_EDITOR
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property,
                                                GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position,
                                   SerializedProperty property,
                                   GUIContent label)
        {
            GUI.enabled = false;

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    EditorGUI.LabelField(position, label.text, property.intValue.ToString());
                    break;
                case SerializedPropertyType.Boolean:
                    EditorGUI.LabelField(position, label.text, property.boolValue.ToString());
                    break;
                case SerializedPropertyType.Float:
                    EditorGUI.LabelField(position, label.text, property.floatValue.ToString("0.00000"));
                    break;
                case SerializedPropertyType.String:
                    EditorGUI.LabelField(position, label.text, property.stringValue);
                    break;
                default:
                    EditorGUI.PropertyField(position, property, label, true);
                    break;
            }
            GUI.enabled = true;
        }
    }
#endif
    #endregion ReadOnly Attribute

    // Notes:
    // RoomAliveToolkit.Utilities.Matrix is row major order
    // UnityEngine.Matrix4x4 is column major order

    public class RAT2Unity
    {
        public static Matrix4x4 Convert(RoomAliveToolkit.Matrix vMat)
        {
            // argumement checking
            if (vMat.Rows != 4 || vMat.Cols != 4)
                throw new System.ArgumentException("vMat not 4x4");

            Matrix4x4 uMat = new Matrix4x4();
            for (int i = 0; i < 16; i++)
            {
                uMat[i] = (float)(vMat[i]);
            }
            // Todo: ensure this works inplace
            uMat = uMat.transpose;
            return uMat;
        }

        public static Matrix4x4 Convert_3x3To4x4(RoomAliveToolkit.Matrix vMat)
        {
            Matrix4x4 retVal = new Matrix4x4();

            retVal[0, 0] = (float)vMat[0, 0];
            retVal[0, 1] = (float)vMat[0, 1];
            retVal[0, 2] = (float)vMat[0, 2];

            retVal[1, 0] = (float)vMat[1, 0];
            retVal[1, 1] = (float)vMat[1, 1];
            retVal[1, 2] = (float)vMat[1, 2];

            retVal[2, 0] = (float)vMat[2, 0];
            retVal[2, 1] = (float)vMat[2, 1];
            retVal[2, 2] = (float)vMat[2, 2];

            retVal = retVal.transpose;
            return retVal;
        }

     
    }

    public class Unity2RAT
    {
        public static RoomAliveToolkit.Matrix Convert(Matrix4x4 uMat)
        {
            RoomAliveToolkit.Matrix vMat = new RoomAliveToolkit.Matrix(4, 4);
            for (int i = 0; i < 16; i++)
            {
                vMat[i] = (double)(uMat[i]);
            }
            // Todo: ensure this works inplace
            vMat.Transpose(vMat);
            return vMat;
        }
        public static RoomAliveToolkit.Matrix Convert_4x4To3x3(Matrix4x4 uMat)
        {
            RoomAliveToolkit.Matrix vMat = new Matrix(3, 3);

            vMat[0, 0] = (double)uMat[0, 0];
            vMat[0, 1] = (double)uMat[0, 1];
            vMat[0, 2] = (double)uMat[0, 2];

            vMat[1, 0] = (double)uMat[1, 0];
            vMat[1, 1] = (double)uMat[1, 1];
            vMat[1, 2] = (double)uMat[1, 2];

            vMat[2, 0] = (double)uMat[2, 0];
            vMat[2, 1] = (double)uMat[2, 1];
            vMat[2, 2] = (double)uMat[2, 2];

            vMat.Transpose();
            return vMat;
        }
    }
}
