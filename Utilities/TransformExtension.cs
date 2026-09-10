//Shady
using UnityEngine;

public static class TransformExtension
{
    /// <summary>
    /// Activates/Deactivates the GameObject of this Transform, depending on the given true/false value.
    /// </summary>
    public static void SetActive(this Transform target, bool value) => target.gameObject.SetActive(value);

    #region Global Position Methods

    /// <summary>
    // Reset Global position to (0,0,0)
    /// </summary>
    public static void ResetGlobalPos(this Transform target) => target.position = Vector3.zero;

    /// <summary>
    /// Change Global Position on x axis only without changing y and z.
    /// </summary>
    public static void SetGlobalPosX(this Transform target, float x) => target.position = new Vector3(x, target.position.y, target.position.z);
    
    /// <summary>
    /// Change Global Position on y axis only without changing x and z.
    /// </summary>
    public static void SetGlobalPosY(this Transform target, float y) => target.position = new Vector3(target.position.x, y, target.position.z);

    /// <summary>
    /// Change Global Position on z axis only without changing x and y.
    /// </summary>
    public static void SetGlobalPosZ(this Transform target, float z) => target.position = new Vector3(target.position.x, target.position.y, z);

    #endregion
    
    #region Local Position Methods

    /// <summary>
    /// Reset Local position to (0,0,0)
    /// </summary>
    public static void ResetLocalPos(this Transform target) => target.localPosition = Vector3.zero;

    /// <summary>
    /// Change Local Position on x axis only without changing y and z.
    /// </summary>
    public static void SetLocalPosX(this Transform target, float x) => target.localPosition = new Vector3(x, target.localPosition.y, target.localPosition.z);
    
    /// <summary>
    /// Change Local Position on y axis only without changing x and z.
    /// </summary>
    public static void SetLocalPosY(this Transform target, float y) => target.localPosition = new Vector3(target.localPosition.x, y, target.localPosition.z);

    /// <summary>
    /// Change Local Position on z axis only without changing x and y.
    /// </summary>
    public static void SetLocalPosZ(this Transform target, float z) => target.localPosition = new Vector3(target.localPosition.x, target.localPosition.y, z);

    /// <summary>
    /// Change Local Position on x axis only and set y and z to 0;
    /// </summary>
    public static void SetOnlyLocalPosX(this Transform Target, float PosX)  => Target.localPosition = Vector3.right * PosX;

    /// <summary>
    /// Change Local Position on y axis only and set x and z to 0;
    /// </summary>
    public static void SetOnlyLocalPosY(this Transform Target, float PosY)  => Target.localPosition = Vector3.up * PosY;

    /// <summary>
    /// Change Local Position on z axis only and set x and y to 0;
    /// </summary>
    public static void SetOnlyLocalPosZ(this Transform Target, float PosZ)  => Target.localPosition = Vector3.forward * PosZ;

    #endregion

    #region Global Rotation Methods

    /// <summary>
    /// Reset Global Euler Angles to (0,0,0)
    /// </summary>
    public static void ResetGlobalRot(this Transform target) => target.eulerAngles = Vector3.zero;

    /// <summary>
    /// Change Global Euler Angles on x axis only without changing y and z.
    /// </summary>
    public static void SetGlobalEulerX(this Transform target, float x) => target.eulerAngles = new Vector3(x, target.eulerAngles.y, target.eulerAngles.z);
    
    /// <summary>
    /// Change Global Euler Angles on y axis only without changing x and z.
    /// </summary>
    public static void SetGlobalEulerY(this Transform target, float y) => target.eulerAngles = new Vector3(target.eulerAngles.x, y, target.eulerAngles.z);

    /// <summary>
    /// Change Global Euler Angles on z axis only without changing x and y.
    /// </summary>
    public static void SetGlobalEulerZ(this Transform target, float z) => target.eulerAngles = new Vector3(target.eulerAngles.x, target.eulerAngles.y, z);

    /// <summary>
    /// Change Global Euler Angles on x axis only and set y and z to 0;
    /// </summary>
    public static void SetOnlyGlobalEulerX(this Transform target, float x)  => target.eulerAngles = Vector3.right * x;
    
    /// <summary>
    /// Change Global Euler Angles on y axis only and set x and z to 0;
    /// </summary>
    public static void SetOnlyGlobalEulerY(this Transform target, float y)  => target.eulerAngles = Vector3.up * y;

    /// <summary>
    /// Change Global Euler Angles on z axis only and set x and y to 0;
    /// </summary>
    public static void SetOnlyGlobalEulerZ(this Transform target, float z)  => target.eulerAngles = Vector3.forward * z;

    #endregion

    #region Local Rotation Methods
    
    /// <summary>
    /// Reset Local Rotation to (0,0,0)
    /// </summary>
    public static void ResetLocalRot(this Transform target) => target.localEulerAngles = Vector3.zero;

    /// <summary>
    /// Change Local Euler Angles on x axis only without changing y and z.
    /// </summary>
    public static void SetLocalEulerX(this Transform target, float x) => target.localEulerAngles = new Vector3(x, target.localEulerAngles.y, target.localEulerAngles.z);
    
    /// <summary>
    /// Change Local Euler Angles on y axis only without changing x and z.
    /// </summary>
    public static void SetLocalEulerY(this Transform target, float y) => target.localEulerAngles = new Vector3(target.localEulerAngles.x, y, target.localEulerAngles.z);

    /// <summary>
    /// Change Local Euler Angles on z axis only without changing x and y.
    /// </summary>
    public static void SetLocalEulerZ(this Transform target, float z) => target.localEulerAngles = new Vector3(target.localEulerAngles.x, target.localEulerAngles.y, z);

    /// <summary>
    /// Change Local Euler Angles on x axis only and set y and z to 0;
    /// </summary>
    public static void SetOnlyLocalEulerX(this Transform target, float x)  => target.localEulerAngles = Vector3.right * x;
    
    /// <summary>
    /// Change Local Euler Angles on y axis only and set x and z to 0;
    /// </summary>
    public static void SetOnlyLocalEulerY(this Transform target, float y)  => target.localEulerAngles = Vector3.up * y;

    /// <summary>
    /// Change Local Euler Angles on z axis only and set x and y to 0;
    /// </summary>
    public static void SetOnlyLocalEulerZ(this Transform target, float z)  => target.localEulerAngles = Vector3.forward * z;

    #endregion

    #region Local Scale Methods
    
    /// <summary>
    /// Reset Local Scale of to (1,1,1)
    /// </summary>
    public static void ResetScale(this Transform target) => target.localScale = Vector3.one;

    /// Scale Methods
    /// <summary>
    /// Change Local Scale with respective values of x, y, z
    /// </summary>
    public static void SetScale(this Transform target, float x, float y, float z) => target.localScale = new Vector3(x, y, z); 

    /// <summary>
    /// Change Local Scale on x axis only without changing y and z.
    /// </summary>
    public static void SetLocalScaleX(this Transform target, float x) => target.localScale = new Vector3(x, target.localScale.y, target.localScale.z);

    /// <summary>
    /// Change Local Scale on y axis only without changing x and z.
    /// </summary>
    public static void SetLocalScaleY(this Transform target, float y) => target.localScale = new Vector3(target.localScale.x, y, target.localScale.z);

    /// <summary>
    /// Change Local Scale on z axis only without changing x and y.
    /// </summary>
    public static void SetLocalScaleZ(this Transform target, float z) => target.localScale = new Vector3(target.localScale.x, target.localScale.y, z);

    #endregion

}//class end