using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace blacksmithname.src
{
    [HarmonyPatch]
    public class HarmonyPatches
    {
        public static FieldInfo BlockEntityAnvilworkItemStack = typeof(BlockEntityAnvil).GetField("workItemStack", BindingFlags.NonPublic | BindingFlags.Instance);
        public static IEnumerable<CodeInstruction> Transpiler_BlockEntityAnvil_CheckIfFinished(IEnumerable<CodeInstruction> instructions)
        {
            bool found = false;
            var codes = new List<CodeInstruction>(instructions);
            var decMethod = AccessTools.GetDeclaredMethods(typeof(IWorldAccessor))
            .Where(m => m.Name == "SpawnItemEntity" && m.GetParameters().Types().Contains(typeof(ItemStack)) && m.GetParameters().Types().Contains(typeof(Vec3d)) && m.GetParameters().Types().Contains(typeof(Vec3d)))
            .First();
            var proxyMethod = AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.AddSmithName));
            for (int i = 0; i < codes.Count; i++)
            {
                if (!found &&
                    codes[i].opcode == OpCodes.Ldarg_1 && codes[i + 1].opcode == OpCodes.Brfalse_S && codes[i + 2].opcode == OpCodes.Ldarg_1 && codes[i - 1].opcode == OpCodes.Stfld)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, BlockEntityAnvilworkItemStack);
                    yield return new CodeInstruction(OpCodes.Call, proxyMethod);
                    found = true;
                }
                yield return codes[i];
            }
        }
        
        public static void AddSmithName(IServerPlayer byPlayer, ItemStack itemStack, ItemStack workItemStack)
        {
            if (byPlayer != null)
            {
                if (itemStack.Item != null && ((itemStack.Item.Shape.Base.Path.StartsWith("item/tool") || itemStack.Item.Shape.Base.Path.StartsWith("item/parts")) 
                    || itemStack.Item.Shape.Base.Path.StartsWith("item/spytube")
                    || itemStack.Item.Code.Domain.Equals("xmelee")
                    || blacksmithname.config.itemCodesToAddAttribute.Any(it => itemStack.Item.Code.Path.StartsWith(it))))
                {
                    itemStack.Attributes.SetString(blacksmithname.smithnameAttributeName, byPlayer.PlayerName);
                }
            }
            else
            {
                if (workItemStack != null)
                    if (workItemStack?.Attributes?.HasAttribute(blacksmithname.smithnameAttributeName) ?? false)
                {
                    if (itemStack.Item != null && (itemStack.Item.Shape.Base.Path.StartsWith("item/tool")
                    || itemStack.Item.Shape.Base.Path.StartsWith("item/spytube")
                    || itemStack.Item.Code.Domain.Equals("xmelee")))
                    {
                        itemStack.Attributes.SetString(workItemStack.Attributes.GetString(blacksmithname.smithnameAttributeName), byPlayer.PlayerName);
                    }
                }
            }
        }
        public static void Postfix_BlockEntityAnvil_TryPut(Vintagestory.GameContent.BlockEntityAnvil __instance, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack ___workItemStack, ref bool __result)
        {
           if(__result)
            {
                if(___workItemStack != null)
                {
                    ___workItemStack.Attributes.SetString(blacksmithname.smithnameAttributeName, byPlayer.PlayerName);
                }
            }
        }

        public static void Prefix_BlockEntityAnvil_TryTake(Vintagestory.GameContent.BlockEntityAnvil __instance, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack ___workItemStack)
        {
            if (___workItemStack != null)
            {
                ___workItemStack.Attributes.RemoveAttribute(blacksmithname.smithnameAttributeName);
            }
            
        }

        public static void Postfix_GetHeldItemInfo(Vintagestory.API.Common.CollectibleObject __instance,
                                                                                      ItemSlot inSlot,
                                                                                      StringBuilder dsc,
                                                                                      IWorldAccessor world,
                                                                                      bool withDebugInfo)
        {
            ItemStack itemstack = inSlot.Itemstack;
            string smithName = itemstack.Attributes.GetString(blacksmithname.smithnameAttributeName);
            if (smithName != null && !(smithName.Length == 0))
            {
                dsc.Append(Lang.Get(blacksmithname.modID + ":smithed_by", "<font color=\"" + blacksmithname.config.nameColor + "\">" + smithName + "</font>")).Append("\n");
                return;
            }
            if (itemstack.Attributes.HasAttribute("tinkeredToolHead"))
            {
                var tree = itemstack.Attributes.GetItemstack("tinkeredToolHead");
                if (tree != null)
                {
                    smithName = tree.Attributes.GetString(blacksmithname.smithnameAttributeName, null);
                    if (smithName != null)
                    {
                        dsc.Append(Lang.Get(blacksmithname.modID + ":smithed_by", "<font color=\""  + blacksmithname.config.nameColor + "\">" + smithName + "</font>")).Append("\n");
                        return;
                    }
                }
            }
            return;
        }
        public static void Postfix_GetHeldItemInfo_Toolworks(Vintagestory.API.Common.CollectibleObject __instance,
                                                                                     ItemSlot inSlot,
                                                                                     StringBuilder dsc,
                                                                                     IWorldAccessor world,
                                                                                     bool withDebugInfo)
        {
            ItemStack itemstack = inSlot.Itemstack;
            var itemStackAttribute = itemstack.Attributes.GetItemstack("Head");
            if(itemStackAttribute != null) 
            {
                string smithName = itemStackAttribute.Attributes.GetString(blacksmithname.smithnameAttributeName);
                if (smithName != null && !(smithName.Length == 0))
                {
                    dsc.Append(Lang.Get(blacksmithname.modID + ":smithed_by", "<font color=\""  + blacksmithname.config.nameColor + "\">" + smithName + "</font>")).Append("\n");
                }
                return;
            }
           
        }

        public static void Postfix_CollectibleObject_OnCreatedByCrafting(Vintagestory.API.Common.CollectibleObject __instance, ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
        {
            
            if (!(outputSlot.Itemstack?.Item?.Shape?.Base?.Path?.StartsWith("item/tool")).HasValue ||
                !(outputSlot.Itemstack?.Item?.Shape?.Base?.Path?.StartsWith("item/tool")).Value
                && !(outputSlot.Itemstack.Item.Code.Domain.Equals("xmelee")))
            {
                return;
            }

            string twoAuthors = "";
            foreach (var it in allInputslots)
            {
                if (it.Itemstack != null && it.Itemstack.Attributes.GetString(blacksmithname.smithnameAttributeName) != null)
                {
                    if (it.Itemstack.Item.Code.Path.StartsWith("xweapongrip"))
                    {
                        if (twoAuthors != "" && !it.Itemstack.Attributes.GetString(blacksmithname.smithnameAttributeName).Equals(twoAuthors))
                        {
                            twoAuthors += " & " + it.Itemstack.Attributes.GetString(blacksmithname.smithnameAttributeName);
                        }               
                        outputSlot.Itemstack.Attributes.SetString(blacksmithname.smithnameAttributeName, twoAuthors);
                        return;
                    }
                    if(it.Itemstack.Item.Code.Path.StartsWith("xspearhead") || it.Itemstack.Item.Code.Path.StartsWith("xhalberdhead"))
                    {
                        outputSlot.Itemstack.Attributes.SetString(blacksmithname.smithnameAttributeName, it.Itemstack.Attributes.GetString(blacksmithname.smithnameAttributeName));
                        return;
                    }
                    if(it.Itemstack.Item.Code.Domain.StartsWith("xmelee"))
                    {
                        twoAuthors = it.Itemstack.Attributes.GetString(blacksmithname.smithnameAttributeName);
                        continue;
                    }
                    outputSlot.Itemstack.Attributes.SetString(blacksmithname.smithnameAttributeName, it.Itemstack.Attributes.GetString(blacksmithname.smithnameAttributeName));
                    return;
                }
            }
        }
    }

}