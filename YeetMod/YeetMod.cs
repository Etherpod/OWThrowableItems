using HarmonyLib;
using OWML.ModHelper;
using System.Reflection;
using UnityEngine;

namespace YeetMod
{
    public class YeetMod : ModBehaviour
    {
        private static ScreenPrompt yeetPrompt;
        // not const so theyre editable in UE
        private static float
            doublePressTimeLimit = 0.5f,
            itemDropTimeLimit = 0.25f,
            yeetSpeedIncreaseRate = 20,
            yeetSpeedLimit = 50;
        private float lastButtonPressTime = float.NegativeInfinity;
        private bool isDoublePressing;


        private void Start()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            yeetPrompt = new(InputLibrary.interact, "<CMD> " + "<color=orange>(x2) (Hold) </color> " + "Throw Item");

            LoadManager.OnCompleteSceneLoad += (_, loadScene) =>
            {
                if (loadScene is OWScene.SolarSystem or OWScene.EyeOfTheUniverse) ModHelper.Events.Unity.FireOnNextUpdate(() => Locator.GetPromptManager().AddScreenPrompt(yeetPrompt, PromptPosition.LowerLeft));
            };
        }

        private void Update()
        {
            if (Locator.GetPromptManager() == null) return;

            if (OWInput.IsInputMode(InputMode.Character) && Locator.GetToolModeSwapper().GetToolMode() == ToolMode.Item)
            {
                yeetPrompt.SetVisibility(true);
                yeetPrompt.SetDisplayState(
                    CheckForObstructed() ? ScreenPrompt.DisplayState.GrayedOut :
                    isDoublePressing ? ScreenPrompt.DisplayState.Attention :
                    ScreenPrompt.DisplayState.Normal
                );
            }
            else
            {
                // reset everything so we dont accidentally count it if we arent holding an item
                yeetPrompt.SetVisibility(false);
                lastButtonPressTime = float.NegativeInfinity;
                isDoublePressing = false;
            }

            if (OWInput.IsNewlyPressed(InputLibrary.interact, InputMode.Character))
            {
                if (Time.time - lastButtonPressTime <= doublePressTimeLimit) isDoublePressing = true;
                lastButtonPressTime = Time.time;
            }
            else if (OWInput.IsNewlyReleased(InputLibrary.interact, InputMode.Character) && isDoublePressing)
            {
                isDoublePressing = false;
                Yeet(Time.time - lastButtonPressTime);
            }
        }

        private bool CheckForObstructed()
        {
            var ray = new Ray(Locator.GetPlayerCamera().transform.position - 0.5f * Locator.GetPlayerCamera().transform.forward, Locator.GetPlayerCamera().transform.forward);
            return Physics.SphereCast(ray, 0.5f, 2.5f, OWLayerMask.physicalMask);
        }

        private static void Yeet(float heldButtonTime)
        {
            //this is directly tied to the check result and we set display state first so this works fine as a return condition to prevent dropping items through walls
            if (yeetPrompt.IsDisplayState(ScreenPrompt.DisplayState.GrayedOut)) return;

            var itemTool = Locator.GetToolModeSwapper().GetItemCarryTool();
            if (itemTool.GetHeldItem().IsAnimationPlaying()) return;

            var yeetSpeed = heldButtonTime <= itemDropTimeLimit ? 0 : Mathf.Clamp(heldButtonTime * yeetSpeedIncreaseRate, 0, yeetSpeedLimit);
            var playerCameraTransform = Locator.GetPlayerCamera().transform;
            var socket = ItemYeetSocket.Create(itemTool.GetHeldItem(), playerCameraTransform.position + playerCameraTransform.forward * 2, playerCameraTransform.rotation, yeetSpeed);
            itemTool.DropItemInstantly(null, socket.transform);

            Locator.GetToolModeSwapper().UnequipTool();
        }
    }
}
