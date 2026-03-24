// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Numerics;

namespace Prowl.Runtime.Rendering;

public class CameraData
{
    public Vector3 WorldSpaceCameraPos;
    public Vector3 CameraUp;
    public Vector3 CameraForward;

    public float NearClipPlane;
    public float FarClipPlane;
    public float Aspect;
    public uint PixelWidth;
    public uint PixelHeight;

    public Matrix4x4 ViewMatrix;
    public Matrix4x4 ProjectionMatrix;
    public Matrix4x4 ViewProjectionMatrix;
    public Matrix4x4 InverseViewMatrix;
    public Matrix4x4 PreviousViewProjectionMatrix;

    public Matrix4x4 OriginViewMatrix;
    public Matrix4x4 NonJitteredProjectionMatrix;

    public BoundingFrustum WorldFrustum;
    public LayerMask CullingMask;

    public CameraClearFlags ClearFlags;
    public DepthTextureMode DepthTextureMode;
    public bool HDR;

    public bool UseCameraRelativeRendering;

    public Camera Camera { get; private set; }

    public static CameraData Create(Camera camera, bool cameraRelative = true)
    {
        var data = new CameraData
        {
            Camera = camera,
            WorldSpaceCameraPos = camera.Transform.position,
            CameraUp = camera.Transform.up,
            CameraForward = camera.Transform.forward,

            NearClipPlane = camera.NearClipPlane,
            FarClipPlane = camera.FarClipPlane,
            Aspect = camera.Aspect,
            PixelWidth = camera.PixelWidth,
            PixelHeight = camera.PixelHeight,

            ViewMatrix = camera.ViewMatrix,
            ProjectionMatrix = Graphics.GetGPUProjectionMatrix(camera.ProjectionMatrix),
            InverseViewMatrix = camera.ViewMatrix.Invert(),
            PreviousViewProjectionMatrix = camera.PreviousViewProjectionMatrix,
            NonJitteredProjectionMatrix = camera.NonJitteredProjectionMatrix,

            WorldFrustum = new BoundingFrustum(camera.ViewMatrix * camera.ProjectionMatrix),
            CullingMask = camera.CullingMask,

            ClearFlags = camera.ClearFlags,
            DepthTextureMode = camera.DepthTextureMode,
            HDR = camera.HDR,

            UseCameraRelativeRendering = cameraRelative
        };

        data.ViewProjectionMatrix = data.ViewMatrix * data.ProjectionMatrix;

        if (cameraRelative)
        {
            data.OriginViewMatrix = camera.OriginViewMatrix;
        }

        return data;
    }
}
