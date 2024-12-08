using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterProfile", menuName = "CustomTools/CharacterProfile", order = 1)]
public class PlayableCharacterProfile : ScriptableObject
{
    [Header("Grounded movement")]
    [Tooltip("Grounded acceleration speed (gradual force)")]
    public float walkAccelerationSpeed =  10f;
    [Tooltip("Maximum grounded speed")]
    public float maxSpeedWalk = 8f;
    [Tooltip("Lesser acceleration speed when aimed (gradual force) MINIMUM  OF 6.5F")]
    public float aimedAccelerationSpeed  = 7f; //6.5 for some reason is the lowest possible before it stops moving completely
    [Tooltip("Maximum slowed speed for aimed-in state")] 
    public float maxSpeedAim = 3f;
    [Tooltip("Jump strength (keep around the high numbers, needed for the smooth acceleration otherwise won't elevate)")]
    public float jumpForce = 400f;
    [Tooltip("Character rotation speed for matching input direction (generally wanted high)")]
    public float rotationSpeed = 1000f;
    [Tooltip("The braking speed/drag for the player when no input is detected (gradual force)")]
    public float decelerationSpeed = 20f;
    [Tooltip("Maximum acceptable angle for slope")]
    public float maxSlopeAngleAscent = 45f;
    [Tooltip("Minimum angle for slope recognition")]
    public float minSlopeAngle = 20f;
    
    public float maxSlopeAngleDescent = 60f;
    [Tooltip("The braking speed/drag for the player when changing directions (gradual force)")]
    public float groundedDrag = 8f;
    [Tooltip("Drag for braking movement when floating")]
    public float zeroGravDrag = 3f;
    
    [Header("Aerial movement")]
    [Tooltip("Aerial acceleration speed when relatively close to ground (gradual force)")]
    public float jumpForwardAccelerationSpeed = 5f;
    //dont  have??
    public float maxSpeedJump = 3f;
    [Tooltip("Air dive acceleration speed (gradual force)")]
    public float diveAccelerationSpeed = 5f;
    [Tooltip("Terminal velocity for air dive")]
    public float terminalVelocity = 20f;
    [Tooltip("Lateral acceleration speed when diving (gradual force)")]
    public float lateralAirDiveSpeed = 3f;
    [Tooltip("Max lateral speed when diving")]
    public float maxLateralSpeed =  5f;
    [Tooltip("Character rotation speed when diving")]
    public float diveRotationSpeed = 50f;
    [Tooltip("Required object distance until character dives (world unit)")]
    public float diveLength = 15f;
    [Tooltip("Required wait time until character dives (seconds)")]
    public float diveWaitTime = 1f;

    [Header("Gravity meter values")]
    public float maxGravityMeter = 100f;
    public float regenRate = 5f;
    public float zeroGravRate  = 2f;
    public float shiftDiveRate = 5f;
    public float groundedRate =  3f;
    public float regenCooldownLength = 2f;

    [Header("Camera variables")] 
    [Tooltip("Camera's default distance from the player")]
    public float defaultDistance = 4f;
    [Tooltip("Camera's zoomed-in distance from the player")]
    public float aimedDistance = 3f;

    [Header("Player materials")] 
    [Tooltip("The player's default body material")]
    public Material defaultSkin;
    [Tooltip("A variant body material to show the player shifted state")]
    public Material shiftedSkin;
}
