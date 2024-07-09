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
            Settings.OnLoad();
            LoadEmbeddedAssetBundle();
        }

        private static void LoadEmbeddedAssetBundle()
        {
            MemoryStream memoryStream;
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("PhotoMod.Resources.photobundle.unity3d");
            memoryStream = new MemoryStream((int)stream.Length);
            stream.CopyTo(memoryStream);

            loadBundle = AssetBundle.LoadFromMemory(memoryStream.ToArray());
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

        public override void OnSceneWasUnloaded(int level, string name)
        {
            AA.ActivateTool(AA.ToolPoint.LeftHand, false);
            AA.Done();
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
                    AA.Done();

                    PhotoAnimation_isRunning = false;
                    yield break;
                }

                AA.ActivateTool(AA.ToolPoint.LeftHand, true);

                AA.SendBool(true, "FunnyBool");

                PhotoAnimation_isRunning = false;
                yield break;
            }

            else
            {
                AA.SendBool(false, "FunnyBool");

                while (AA.AnimatorIsActiveAndRunning() && AA.currentAnimator.IsInState(new string[] { "PutAway", "Idle", "BringUp" }) == true)
                {
                    yield return new WaitForEndOfFrame();
                }
                AA.Done();
                AA.ActivateTool(AA.ToolPoint.LeftHand, false);

                PhotoAnimation_isRunning = false;
                yield break;
            }
        }

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.IsReadyToEquip))]
        public class DelayToolSwitchWhileHoldingPicture
        {
            private static void Postfix(ref bool __result)
            {
                if (AA.AnimatorIsActiveAndRunning()) __result = false;
            }
        }

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.EquipItem))]
        public class HidePictureWhenEquippingVanillaTool
        {
            private static void Prefix()
            {
                if (AA.AnimatorIsActiveAndRunning() && !PhotoAnimation_isRunning) MelonCoroutines.Start(PhotoAnimation(false));
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
            if (InputManager.GetKeyDown(InputManager.m_CurrentContext, Settings.options.usePictureButton))
            {
                if (PhotoAnimation_isRunning) return;

                UpdatePhotoTexture();

                if (AA.AnimatorIsActiveAndRunning())
                {
                    MelonCoroutines.Start(PhotoAnimation(false));
                }
                else
                {
                    MelonCoroutines.Start(PhotoAnimation(true));
                }
            }

            if (InputManager.GetHolsterPressed(InputManager.m_CurrentContext))
            {
                MelonCoroutines.Start(PhotoAnimation(false));
            }
        }
    }
}