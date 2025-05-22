#if UNITY_EDITOR
using UnityEngine;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;

public class AdminControlsTests
{
    private GameObject testObject;
    private AdminControls adminControls;
    private Transform rootTransform;
    private Transform xrOrigin;
    private Transform cameraOffset;

    [SetUp]
    public void Setup()
    {
        // Create test objects
        testObject = new GameObject("AdminControls");
        adminControls = testObject.AddComponent<AdminControls>();

        // Create and setup required transforms
        rootTransform = new GameObject("RootTransform").transform;
        xrOrigin = new GameObject("XR Origin").transform;
        cameraOffset = new GameObject("Camera Offset").transform;

        // Setup hierarchy
        cameraOffset.SetParent(xrOrigin);
        xrOrigin.SetParent(rootTransform);

        // Assign references
        adminControls.rootTransform = rootTransform;
        adminControls.xrOrigin = xrOrigin;
        adminControls.cameraOffset = cameraOffset;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(testObject);
        Object.DestroyImmediate(rootTransform.gameObject);
    }

    [Test]
    public void EnableControls_AllButtonsPressed_ControlsEnabled()
    {
        // Arrange
        adminControls.adminControlsEnabled = false;
        var forceEnabledField = adminControls.GetType().GetField("forceEnabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        forceEnabledField.SetValue(adminControls, false);

        // Act
        adminControls.SendMessage("EnableControls");

        // Assert
        Assert.IsTrue(adminControls.adminControlsEnabled);
        Assert.IsTrue((bool)forceEnabledField.GetValue(adminControls));
    }

    [Test]
    public void DisableControls_RightGripPressed_ControlsDisabled()
    {
        // Arrange
        adminControls.adminControlsEnabled = true;
        var forceEnabledField = adminControls.GetType().GetField("forceEnabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        forceEnabledField.SetValue(adminControls, true);

        // Act
        adminControls.SendMessage("DisableControls");

        // Assert
        Assert.IsFalse(adminControls.adminControlsEnabled);
        Assert.IsFalse((bool)forceEnabledField.GetValue(adminControls));
    }

    [Test]
    public void SaveCurrentRotation_RootTransformExists_RotationSaved()
    {
        // Arrange
        Quaternion testRotation = Quaternion.Euler(0, 45, 0);
        rootTransform.rotation = testRotation;

        // Act
        adminControls.SendMessage("SaveCurrentRotation");

        // Assert
        Assert.AreEqual(testRotation, adminControls.GetType().GetField("savedRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(adminControls));
    }

    [Test]
    public void RestoreSavedRotation_SavedRotationExists_RotationRestored()
    {
        // Arrange
        Quaternion testRotation = Quaternion.Euler(0, 45, 0);
        adminControls.GetType().GetField("savedRotation", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(adminControls, testRotation);

        // Act
        adminControls.SendMessage("RestoreSavedRotation");

        // Assert
        Assert.AreEqual(testRotation, rootTransform.rotation);
    }

    [Test]
    public void ValidateSceneComponents_AllComponentsPresent_ReturnsTrue()
    {
        // Act
        bool result = (bool)adminControls.GetType().GetMethod("ValidateSceneComponents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(adminControls, null);

        // Assert
        Assert.IsTrue(result);
    }

    [Test]
    public void ValidateSceneComponents_MissingRootTransform_ReturnsFalse()
    {
        // Arrange
        adminControls.rootTransform = null;

        // Act
        bool result = (bool)adminControls.GetType().GetMethod("ValidateSceneComponents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(adminControls, null);

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public void ValidateSceneComponents_MissingXROrigin_ReturnsFalse()
    {
        // Arrange
        adminControls.xrOrigin = null;

        // Act
        bool result = (bool)adminControls.GetType().GetMethod("ValidateSceneComponents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(adminControls, null);

        // Assert
        Assert.IsFalse(result);
    }

    [Test]
    public void RotateSceneAroundPointThumbstick_ValidRotation_RotatesCorrectly()
    {
        // Arrange
        Vector3 initialPosition = rootTransform.position;
        Quaternion initialRotation = rootTransform.rotation;
        float rotationAmount = 45f;

        // Act
        adminControls.GetType().GetMethod("RotateSceneAroundPointThumbstick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(adminControls, new object[] { rootTransform.position, rotationAmount });

        // Assert
        Assert.AreEqual(Quaternion.Euler(0, rotationAmount, 0), rootTransform.rotation);
        Assert.AreEqual(initialPosition, xrOrigin.position);
        Assert.AreEqual(initialRotation, xrOrigin.rotation);
    }
}
#endif 