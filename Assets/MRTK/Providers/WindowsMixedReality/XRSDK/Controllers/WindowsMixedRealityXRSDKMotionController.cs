﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.WindowsMixedReality;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.XR;

namespace Microsoft.MixedReality.Toolkit.XRSDK.WindowsMixedReality
{
    /// <summary>
    /// XR SDK implementation of Windows Mixed Reality motion controllers.
    /// </summary>
    [MixedRealityController(
        SupportedControllerType.WindowsMixedReality,
        new[] { Handedness.Left, Handedness.Right },
        "StandardAssets/Textures/MotionController")]
    public class WindowsMixedRealityXRSDKMotionController : BaseWindowsMixedRealityXRSDKSource
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public WindowsMixedRealityXRSDKMotionController(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
            : base(trackingState, controllerHandedness, inputSource, interactions)
        {
            controllerDefinition = new WindowsMixedRealityControllerDefinition(inputSource, controllerHandedness);
        }

        private readonly WindowsMixedRealityControllerDefinition controllerDefinition;

        /// <inheritdoc />
        public override MixedRealityInteractionMapping[] DefaultInteractions => controllerDefinition?.DefaultInteractions;

        private static readonly ProfilerMarker UpdateButtonDataPerfMarker = new ProfilerMarker("[MRTK] WindowsMixedRealityXRSDKMotionController.UpdateButtonData");

        /// <inheritdoc />
        protected override void UpdateButtonData(MixedRealityInteractionMapping interactionMapping, InputDevice inputDevice)
        {
            using (UpdateButtonDataPerfMarker.Auto())
            {
                Debug.Assert(interactionMapping.AxisType == AxisType.Digital);

                InputFeatureUsage<bool> buttonUsage;

                // These mappings are flipped from the base class,
                // where thumbstick is primary and touchpad is secondary.
                switch (interactionMapping.InputType)
                {
                    case DeviceInputType.TouchpadTouch:
                        buttonUsage = CommonUsages.primary2DAxisTouch;
                        break;
                    case DeviceInputType.TouchpadPress:
                        buttonUsage = CommonUsages.primary2DAxisClick;
                        break;
                    case DeviceInputType.ThumbStickPress:
                        buttonUsage = CommonUsages.secondary2DAxisClick;
                        break;
                    default:
                        base.UpdateButtonData(interactionMapping, inputDevice);
                        return;
                }

                if (inputDevice.TryGetFeatureValue(buttonUsage, out bool buttonPressed))
                {
                    interactionMapping.BoolData = buttonPressed;
                }

                // If our value changed raise it.
                if (interactionMapping.Changed)
                {
                    // Raise input system event if it's enabled
                    if (interactionMapping.BoolData)
                    {
                        CoreServices.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                    }
                    else
                    {
                        CoreServices.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction);
                    }
                }
            }
        }

        private static readonly ProfilerMarker UpdateDualAxisDataPerfMarker = new ProfilerMarker("[MRTK] WindowsMixedRealityXRSDKMotionController.UpdateDualAxisData");

        /// <inheritdoc />
        protected override void UpdateDualAxisData(MixedRealityInteractionMapping interactionMapping, InputDevice inputDevice)
        {
            using (UpdateDualAxisDataPerfMarker.Auto())
            {
                Debug.Assert(interactionMapping.AxisType == AxisType.DualAxis);

                InputFeatureUsage<Vector2> axisUsage;

                // These mappings are flipped from the base class,
                // where thumbstick is primary and touchpad is secondary.
                switch (interactionMapping.InputType)
                {
                    case DeviceInputType.ThumbStick:
                        axisUsage = CommonUsages.secondary2DAxis;
                        break;
                    case DeviceInputType.Touchpad:
                        axisUsage = CommonUsages.primary2DAxis;
                        break;
                    default:
                        base.UpdateDualAxisData(interactionMapping, inputDevice);
                        return;
                }

                if (inputDevice.TryGetFeatureValue(axisUsage, out Vector2 axisData))
                {
                    // Update the interaction data source
                    interactionMapping.Vector2Data = axisData;
                }

                // If our value changed raise it.
                if (interactionMapping.Changed)
                {
                    // Raise input system event if it's enabled
                    CoreServices.InputSystem?.RaisePositionInputChanged(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, interactionMapping.Vector2Data);
                }
            }
        }
    }
}