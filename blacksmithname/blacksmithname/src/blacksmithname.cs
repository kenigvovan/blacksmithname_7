using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;


namespace blacksmithname.src
{
    public class blacksmithname: ModSystem
    {
        public static ICoreServerAPI api;
        public static Harmony harmonyInstance;
        public const string harmonyID = "blacksmithname.Patches";

        public static string getID()
        {
            return "blacksmithname";
        }
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            harmonyInstance = new Harmony(harmonyID);
            GlobalConstants.IgnoredStackAttributes = GlobalConstants.IgnoredStackAttributes.Append("smithname").ToArray();

            harmonyInstance.Patch(typeof(Vintagestory.GameContent.BlockEntityAnvil).GetMethod("TryPut", BindingFlags.NonPublic | BindingFlags.Instance),
                postfix: new HarmonyMethod(typeof(harmPatches).GetMethod("Postfix_BlockEntityAnvil_TryPut")));

            harmonyInstance.Patch(typeof(Vintagestory.GameContent.BlockEntityAnvil).GetMethod("TryTake", BindingFlags
                .NonPublic | BindingFlags.Instance),
                prefix: new HarmonyMethod(typeof(harmPatches).GetMethod("Prefix_BlockEntityAnvil_TryTake")));
            
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            harmonyInstance = new Harmony(harmonyID);
            //ANVIL
            harmonyInstance.Patch(typeof(Vintagestory.GameContent.BlockEntityAnvil).GetMethod("CheckIfFinished"),
                transpiler: new HarmonyMethod(typeof(harmPatches).GetMethod("Transpiler_BlockEntityAnvil_CheckIfFinished")));




            //COLLECTIBLE
            harmonyInstance.Patch(typeof(Vintagestory.API.Common.CollectibleObject).GetMethod("OnCreatedByCrafting"),
                postfix: new HarmonyMethod(typeof(harmPatches).GetMethod("Postfix_CollectibleObject_OnCreatedByCrafting")));

            /*harmonyInstance.Patch(typeof(Vintagestory.GameContent.ItemWearable).GetMethod("OnCreatedByCrafting"),
                postfix: new HarmonyMethod(typeof(harmPatches).GetMethod("Postfix_ItemWearable_OnCreatedByCrafting")));*/
        }
        public override void Dispose()
        {
            harmonyInstance.UnpatchAll();
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            harmonyInstance = new Harmony(harmonyID);

            //COLLECTIBLE
            harmonyInstance.Patch(typeof(Vintagestory.API.Common.CollectibleObject).GetMethod("GetHeldItemInfo"),
                postfix: new HarmonyMethod(typeof(harmPatches).GetMethod("Postfix_GetHeldItemInfo")));
            
        }
    }
}
