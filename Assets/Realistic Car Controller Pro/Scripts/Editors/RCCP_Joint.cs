//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Stores properties, sets properties, and locks motions of the Configurable Joint.
/// </summary>
[System.Serializable]
public class RCCP_Joint {

    /// <summary>
    /// Connected rigidbody of the Configurable Joint.
    /// </summary>
    [Tooltip("Rigidbody that this joint is connected to.")]
    public Rigidbody connectedBody;     //  Connected body of the configurable joint.

    /// <summary>
    /// Local-space anchor position of the Configurable Joint.
    /// </summary>
    [Tooltip("Local-space anchor position of the joint on this body.")]
    public Vector3 anchor;      //  Anchor of the configurable joint.
    /// <summary>
    /// Primary axis direction of the Configurable Joint in local space.
    /// </summary>
    [Tooltip("Primary axis direction of the joint in local space.")]
    public Vector3 axis;        //  Axis of the configurable joint.

    //  Joint configurations.
    /// <summary>
    /// Angular motion mode around the X axis.
    /// </summary>
    internal ConfigurableJointMotion jointMotionAngularX;
    /// <summary>
    /// Angular motion mode around the Y axis.
    /// </summary>
    internal ConfigurableJointMotion jointMotionAngularY;
    /// <summary>
    /// Angular motion mode around the Z axis.
    /// </summary>
    internal ConfigurableJointMotion jointMotionAngularZ;

    /// <summary>
    /// Linear motion mode along the X axis.
    /// </summary>
    internal ConfigurableJointMotion jointMotionX;
    /// <summary>
    /// Linear motion mode along the Y axis.
    /// </summary>
    internal ConfigurableJointMotion jointMotionY;
    /// <summary>
    /// Linear motion mode along the Z axis.
    /// </summary>
    internal ConfigurableJointMotion jointMotionZ;

    //  Joint limitations.
    /// <summary>
    /// Linear movement limit of the joint.
    /// </summary>
    [Tooltip("Linear movement limit of the joint.")]
    public SoftJointLimit linearLimit;
    /// <summary>
    /// Lower angular rotation limit around the X axis.
    /// </summary>
    [Tooltip("Lower angular rotation limit around the X axis.")]
    public SoftJointLimit lowAngularXLimit;
    /// <summary>
    /// Upper angular rotation limit around the X axis.
    /// </summary>
    [Tooltip("Upper angular rotation limit around the X axis.")]
    public SoftJointLimit highAngularXLimit;
    /// <summary>
    /// Angular rotation limit around the Y axis.
    /// </summary>
    [Tooltip("Angular rotation limit around the Y axis.")]
    public SoftJointLimit angularYLimit;
    /// <summary>
    /// Angular rotation limit around the Z axis.
    /// </summary>
    [Tooltip("Angular rotation limit around the Z axis.")]
    public SoftJointLimit angularZLimit;

    //  Original position and rotation of the joint.
    /// <summary>
    /// Original local position of the joint before any detachment.
    /// </summary>
    [Tooltip("Original local position of the joint before any detachment.")]
    public Vector3 orgLocalPosition;
    /// <summary>
    /// Original local rotation of the joint before any detachment.
    /// </summary>
    [Tooltip("Original local rotation of the joint before any detachment.")]
    public Quaternion orgLocalRotation;
    /// <summary>
    /// Original parent transform of the joint before any detachment.
    /// </summary>
    [Tooltip("Original parent transform of the joint before any detachment.")]
    public Transform orgParent;

    /// <summary>
    /// Sets the target Configurable Joint properties to the stored one.
    /// </summary>
    /// <param name="targetJoint">The Configurable Joint to apply stored properties to.</param>
    public void SetProperties(ConfigurableJoint targetJoint) {

        targetJoint.transform.SetParent(orgParent);
        targetJoint.transform.localPosition = orgLocalPosition;
        targetJoint.transform.localRotation = orgLocalRotation;

        targetJoint.connectedBody = connectedBody;
        targetJoint.anchor = anchor;
        targetJoint.axis = axis;

        targetJoint.angularXMotion = jointMotionAngularX;
        targetJoint.angularYMotion = jointMotionAngularY;
        targetJoint.angularZMotion = jointMotionAngularZ;

        targetJoint.xMotion = jointMotionX;
        targetJoint.yMotion = jointMotionY;
        targetJoint.zMotion = jointMotionZ;

        targetJoint.linearLimit = linearLimit;
        targetJoint.lowAngularXLimit = lowAngularXLimit;
        targetJoint.highAngularXLimit = highAngularXLimit;
        targetJoint.angularYLimit = angularYLimit;
        targetJoint.angularZLimit = angularZLimit;

        targetJoint.projectionMode = JointProjectionMode.PositionAndRotation;
        targetJoint.projectionAngle = 0f;
        targetJoint.projectionDistance = 0f;

    }

    /// <summary>
    /// Gets default properties of the Configurable Joint.
    /// </summary>
    /// <param name="joint">The Configurable Joint to read properties from.</param>
    public void GetProperties(ConfigurableJoint joint) {

        connectedBody = joint.connectedBody;
        anchor = joint.anchor;
        axis = joint.axis;

        jointMotionAngularX = joint.angularXMotion;
        jointMotionAngularY = joint.angularYMotion;
        jointMotionAngularZ = joint.angularZMotion;

        jointMotionX = joint.xMotion;
        jointMotionY = joint.yMotion;
        jointMotionZ = joint.zMotion;

        linearLimit = joint.linearLimit;
        lowAngularXLimit = joint.lowAngularXLimit;
        highAngularXLimit = joint.highAngularXLimit;
        angularYLimit = joint.angularYLimit;
        angularZLimit = joint.angularZLimit;

        orgLocalPosition = joint.transform.localPosition;
        orgLocalRotation = joint.transform.localRotation;
        orgParent = joint.transform.parent;

    }

    /// <summary>
    /// Locks all motion axes of the Configurable Joint, preventing any movement or rotation.
    /// </summary>
    /// <param name="joint">The Configurable Joint to lock.</param>
    public static void LockPart(ConfigurableJoint joint) {

        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

    }

    /// <summary>
    /// Frees all motion axes of the Configurable Joint, allowing unrestricted movement and rotation.
    /// </summary>
    /// <param name="joint">The Configurable Joint to free.</param>
    public static void LoosePart(ConfigurableJoint joint) {

        joint.angularXMotion = ConfigurableJointMotion.Free;
        joint.angularYMotion = ConfigurableJointMotion.Free;
        joint.angularZMotion = ConfigurableJointMotion.Free;

        joint.xMotion = ConfigurableJointMotion.Free;
        joint.yMotion = ConfigurableJointMotion.Free;
        joint.zMotion = ConfigurableJointMotion.Free;

    }

}
