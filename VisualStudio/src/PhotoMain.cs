namespace PhotoMod
{

    internal class PhotoMod : MelonMod
    {
        public static readonly string mainAssetName = "photobundle";
        public static AssetBundle loadBundle;
        public static bool PhotoAnimation_isRunning;
        public static readonly Shader vanillaSkinShader = Shader.Find("Shader Forge/TLD_StandardSkinned");

        public override void OnInitializeMelon()
        {
            loadBundle = AssetBundle.LoadFromFile("Mods/photobundle.unity3d");
        }

        public override void OnSceneWasInitialized(int level, string name)
        {
            if (Utility.IsMainMenu(name))
            {
                AA.DestroyOnMainMenu();
            }

            if (Utility.IsScenePlayable(name))
            {
                if (!AA.animatorRegistered)
                {
                    AA.Register(loadBundle, mainAssetName);
                    AA.AppendTool(loadBundle, "Polaroid", null, AA.ToolPoint.LeftHand);                
                }
            }
        }
        public static IEnumerator PhotoAnimation(bool start)
        {
            PhotoAnimation_isRunning = true;

            if (start)
            {
                GameManager.GetPlayerManagerComponent().UnequipItemInHands();

                while (!GameManager.GetPlayerManagerComponent().IsReadyToEquip())
                {
                    yield return new WaitForEndOfFrame();
                }

                MelonCoroutines.Start(AA.Activate());
                while (AA.CR_Activate_isRunning)
                {
                    yield return new WaitForEndOfFrame();
                }

                if (AA.activationPrevented)
                {
                    PhotoAnimation_isRunning = false;
                    AA.Done();
                    yield break;
                }

                AA.ActivateTool(AA.ToolPoint.LeftHand, true);

                AA.SendBool(true, "FunnyBool");

                yield break;
            }

            else
            {
                AA.SendBool(false, "FunnyBool");

                while (AA.currentAnimator.IsInState(new string[] { "PutAway", "Idle", "BringUp" }))
                {
                    yield return new WaitForEndOfFrame();
                }

                AA.Done();
                AA.ActivateTool(AA.ToolPoint.LeftHand, false);

                yield break;
            }
        }

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.IsReadyToEquip))]
        public class DelayToolSwitchWhileHoldingBook
        {
            private static void Postfix(ref bool __result)
            {
                if (AA.currentAnimator?.enabled == true) __result = false;
            }
        }

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.EquipItem))]
        public class HideBookWhenEquippingVanillaTool
        {
            private static void Prefix()
            {
                if (AA.currentAnimator?.enabled == true) MelonCoroutines.Start(PhotoAnimation(false));
            }
        }

        public static void UpdatePhotoTexture()
        {
            string texPath = null;
            texPath = "Mods/PhotoMod/Polaroid.png";

            if (File.Exists(texPath))
            {
                Texture2D polaroidTex = new Texture2D(2, 2);
                ImageConversion.LoadImage(polaroidTex, File.ReadAllBytes(texPath));
                AA.toolLeft.GetComponent<SkinnedMeshRenderer>().sharedMaterial.mainTexture = polaroidTex;
                AA.toolLeft.GetComponent<SkinnedMeshRenderer>().sharedMaterial.shader = vanillaSkinShader;
            }
        }

        public override void OnUpdate()
        {
            if (InputManager.GetKeyDown(InputManager.m_CurrentContext, KeyCode.T))
            {
                UpdatePhotoTexture();

                if (AA.animatorRegistered && AA.currentAnimator?.enabled == true)
                {
                    MelonCoroutines.Start(PhotoAnimation(false));
                }
                else
                {
                    MelonCoroutines.Start(PhotoAnimation(true));
                }
            }
        }

        public override void OnSceneWasUnloaded(int level, string name)
        {
            AA.Done();
            AA.ActivateTool(AA.ToolPoint.LeftHand, false);
        }
    }
}