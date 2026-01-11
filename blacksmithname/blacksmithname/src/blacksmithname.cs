using System.Linq;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;


namespace blacksmithname.src
{
    public class blacksmithname: ModSystem
    {
        public static Harmony harmonyInstance;
        public static Config config;
        public const string modID = "blacksmithname";
        public const string harmonyID = modID + ".Patches";
        public const string channelName = modID + "Channel";
        public const string smithnameAttributeName = "smithname";
        internal static IServerNetworkChannel serverChannel;
        internal static IClientNetworkChannel clientChannel;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            harmonyInstance = new Harmony(harmonyID);
            if (!GlobalConstants.IgnoredStackAttributes.Contains(smithnameAttributeName))
            {
                GlobalConstants.IgnoredStackAttributes =
                    GlobalConstants.IgnoredStackAttributes.Append(smithnameAttributeName).ToArray();
            }

            harmonyInstance.Patch(typeof(Vintagestory.GameContent.BlockEntityAnvil).GetMethod("TryPut", BindingFlags.NonPublic | BindingFlags.Instance),
                postfix: new HarmonyMethod(
                AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.Postfix_BlockEntityAnvil_TryPut))));

            harmonyInstance.Patch(typeof(Vintagestory.GameContent.BlockEntityAnvil).GetMethod("TryTake", BindingFlags
                .NonPublic | BindingFlags.Instance),
                prefix: new HarmonyMethod(
                AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.Prefix_BlockEntityAnvil_TryTake))));           
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            harmonyInstance.Patch(typeof(Vintagestory.GameContent.BlockEntityAnvil).GetMethod("CheckIfFinished"),
                transpiler: AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.Transpiler_BlockEntityAnvil_CheckIfFinished)));
            harmonyInstance.Patch(typeof(Vintagestory.API.Common.CollectibleObject).GetMethod("OnCreatedByCrafting"),
                postfix: AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.Postfix_CollectibleObject_OnCreatedByCrafting)));
            LoadConfig(api);
            serverChannel = api.Network.RegisterChannel(channelName);
            serverChannel.RegisterMessageType(typeof(SyncBlacksmithNamePacket));
            api.Event.PlayerNowPlaying += (IServerPlayer byPlayer) =>
            {
                serverChannel.SendPacket(new SyncBlacksmithNamePacket()
                {
                    NameColor = config.nameColor
                }, byPlayer);                           
            };
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            //COLLECTIBLE
            harmonyInstance.Patch(typeof(Vintagestory.API.Common.CollectibleObject).GetMethod("GetHeldItemInfo"),
                postfix: new HarmonyMethod(
                AccessTools.Method(typeof(HarmonyPatches), nameof(HarmonyPatches.Postfix_GetHeldItemInfo))
            ));
            if (config == null)
            {
                LoadConfig(api);
            }
            clientChannel = api.Network.RegisterChannel(channelName);
            clientChannel.RegisterMessageType(typeof(SyncBlacksmithNamePacket));
            clientChannel.SetMessageHandler<SyncBlacksmithNamePacket>((packet) =>
            {
                config.nameColor = packet.NameColor;
            });
        }
        private void LoadConfig(ICoreAPI api)
        {
            config = api.LoadModConfig<Config>(modID + ".json") ?? new Config();
            api.StoreModConfig(config, modID + ".json");
            config = api.LoadModConfig<Config>(this.Mod.Info.ModID + ".json");
            if (config == null)
            {
                config = new Config();
                api.StoreModConfig<Config>(config, this.Mod.Info.ModID + ".json");
                return;
            }
            api.StoreModConfig<Config>(config, this.Mod.Info.ModID + ".json");       
        }
        public override void Dispose()
        {
            harmonyInstance?.UnpatchAll(harmonyID);
            harmonyInstance = null;
        }
    }
}
