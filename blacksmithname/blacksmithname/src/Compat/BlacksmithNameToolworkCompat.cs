using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace blacksmithname.src.Compat
{
    public class BlacksmithNameToolworkCompat: ModSystem
    {
        public static Harmony harmonyInstance;
        public const string harmonyID = "blacksmithnametoolnamecompat.Patches";
        public override bool ShouldLoad(ICoreAPI api)
        {
            if (base.ShouldLoad(api))
            {
                if (api.Side == EnumAppSide.Client || !api.ModLoader.IsModEnabled("toolsmith"))
                {
                    return false;
                }
                return true;
            }
            return false;
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            harmonyInstance = new Harmony(harmonyID);
            if (api.ModLoader.IsModEnabled("toolworks"))
            {
                harmonyInstance.Patch(typeof(Toolworks.ItemCompositeTool).GetMethod("GetHeldItemInfo"),
                    postfix: new HarmonyMethod(typeof(harmPatches).GetMethod("Postfix_GetHeldItemInfo_Toolworks")));
                harmonyInstance.Patch(typeof(Toolworks.ItemBoundTool).GetMethod("GetHeldItemInfo"),
                    postfix: new HarmonyMethod(typeof(harmPatches).GetMethod("Postfix_GetHeldItemInfo_Toolworks")));
                harmonyInstance.Patch(typeof(Toolworks.ItemHaftedTool).GetMethod("GetHeldItemInfo"),
                    postfix: new HarmonyMethod(typeof(harmPatches).GetMethod("Postfix_GetHeldItemInfo_Toolworks")));
            }
        }
        public override void Dispose()
        {
            harmonyInstance?.UnpatchAll(harmonyID);
        }
    }
}
