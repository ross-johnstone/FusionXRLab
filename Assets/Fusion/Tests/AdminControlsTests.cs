#if UNITY_EDITOR
using UnityEngine;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

public class XRRotationControlTests
{
    private GameObject testObject;
    private XRRotationControl rotationControl;
    private Transform xrRig;
    private Transform vrEnvironment;
    private Transform networkScene;

    [SetUp]
    public void Setup()
    {
        // Create test objects
        testObject = new GameObject("XRRotationControl");
        rotationControl = testObject.AddComponent<XRRotationControl>();

        // Create and setup required transforms
        xrRig = new GameObject("XR Rig").transform;
        vrEnvironment = new GameObject("VR Environment").transform;
        networkScene = new GameObject("Network Scene").transform;

        // Assign references
        rotationControl.xrRig = xrRig;
        rotationControl.vrEnvironment = vrEnvironment;
        rotationControl.networkScene = networkScene;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(testObject);
        Object.DestroyImmediate(xrRig.gameObject);
        Object.DestroyImmediate(vrEnvironment.gameObject);
        Object.DestroyImmediate(networkScene.gameObject);
    }

    [Test]
    public void RotateXRRig_ValidRotation_RotatesCorrectly()
    {
        // Arrange
        Vector3 initialPosition = xrRig.position;
        Quaternion initialRotation = xrRig.rotation;
        float rotationAmount = 45f;

        // Act
        rotationControl.GetType().GetMethod("RotateXRRig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(rotationControl, new object[] { rotationAmount });

        // Assert
        Assert.AreEqual(Quaternion.Euler(0, rotationAmount, 0), xrRig.rotation);
        Assert.AreEqual(initialPosition, xrRig.position);
    }

    [Test]
    public void RotateVREnvironment_ValidRotation_RotatesCorrectly()
    {
        // Arrange
        Vector3 initialPosition = vrEnvironment.position;
        Quaternion initialRotation = vrEnvironment.rotation;
        float rotationAmount = 45f;

        // Act
        rotationControl.GetType().GetMethod("RotateVREnvironment", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(rotationControl, new object[] { rotationAmount });

        // Assert
        Assert.AreEqual(Quaternion.Euler(0, rotationAmount, 0), vrEnvironment.rotation);
        Assert.AreEqual(initialPosition, vrEnvironment.position);
    }

    [Test]
    public void RotateXRRig_NullReference_NoRotation()
    {
        // Arrange
        rotationControl.xrRig = null;
        float rotationAmount = 45f;

        // Act & Assert
        Assert.DoesNotThrow(() => {
            rotationControl.GetType().GetMethod("RotateXRRig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(rotationControl, new object[] { rotationAmount });
        });
    }

    [Test]
    public void RotateVREnvironment_NullReference_NoRotation()
    {
        // Arrange
        rotationControl.vrEnvironment = null;
        float rotationAmount = 45f;

        // Act & Assert
        Assert.DoesNotThrow(() => {
            rotationControl.GetType().GetMethod("RotateVREnvironment", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(rotationControl, new object[] { rotationAmount });
        });
    }
}
#endif 