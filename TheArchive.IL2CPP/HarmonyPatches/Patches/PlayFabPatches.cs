﻿using MelonLoader;
using PlayFab.ClientModels;
using SNetwork;
using System;
using System.Collections.Generic;
using System.IO;
using TheArchive.Core;
using TheArchive.Utilities;
using static TheArchive.Core.ArchivePatcher;

namespace TheArchive.HarmonyPatches.Patches
{
    [BindPatchToSetting(nameof(ArchiveSettings.EnableLocalProgressionPatches), "LocalProgression")]
    public class PlayFabPatches
    {
        [Obsolete]
        public static bool DisableAllPlayFabInteraction { get; set; } = true;

        public static bool IsLoggedIn { get; private set; } = false;

        public static Il2CppSystem.Collections.Generic.Dictionary<string, string> playerEntityData = new Il2CppSystem.Collections.Generic.Dictionary<string, string>();

        /*[ArchivePatch(typeof(PlayFabManager))]
        [ArchivePatch(nameof(PlayFabManager.LoggedIn), MethodType.Getter)]
        internal static class PlayFabManager_LoggedInGetterPatch
        {
            public static bool Prefix(ref bool __result)
            {
                if (DisableAllPlayFabInteraction)
                {
                    __result = IsLoggedIn;
                    return false;
                }
                __result = false;
                return true;
            }
        }

        [ArchivePatch(typeof(PlayFabManager))]
        [ArchivePatch(nameof(PlayFabManager.LocalPlayerTitleData), MethodType.Getter)]
        internal static class PlayFabManager_LocalPlayerTitleDataPatch
        {
            public static bool Prefix(ref Il2CppSystem.Collections.Generic.Dictionary<string, string> __result)
            {
                if(DisableAllPlayFabInteraction)
                {
                    __result = playerEntityData;
                    return false;
                }
                __result = null;
                return true;
            }
        }*/

        /*[ArchivePatch(typeof(PlayFabManager), "TryGetPlayerEntityFileValue")]
        internal static class PlayFabManager_TryGetPlayerEntityFileValuePatch
        {
            public static bool Prefix(string fileName, out string value, ref bool __result)
            {
                
                __result = playerEntityData.TryGetValue(fileName, out value);

                ArchiveLogger.Msg(ConsoleColor.Green, $"Getting {fileName} from playerEntityData.");

                return false;
            }
        }*/



        /*[ArchivePatch(typeof(PlayFabManager), "SetPlayerEntityFileValue")] // SetPlayerEntityFileValue(string fileName, string value)
        internal static class PlayFabManager_SetPlayerEntityFileValuePatch
        {
            public static bool Prefix(string fileName, string value)
            {
                ArchiveLogger.Msg(ConsoleColor.Green, $"Adding {fileName} to playerEntityData.");
                if (playerEntityData.ContainsKey(fileName))
                {
                    playerEntityData[fileName] = value;
                    return false;
                }

                playerEntityData.Add(fileName, value);
                return false;
            }
        }*/


        /*[ArchivePatch(typeof(PlayFabManager), "DoUploadPlayerEntityFile")]
        internal class PlayFabManager_DoUploadPlayerEntityFilePatch
        {
            public static bool Prefix(string fileName)
            {
                if (DisableAllPlayFabInteraction)
                {


                    // This is where the game usually uploads your Progression to the PlayFab servers.
                    // TODO: Save player progression to disk instead!
                    if (playerEntityData.TryGetValue(fileName, out string value))
                        LocalFiles.SaveToFile(fileName, value);

                    var eventInfo = typeof(PlayFabManager).GetType().GetEvent("OnFileUploadSuccess", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    var eventDelegate = (MulticastDelegate) typeof(PlayFabManager).GetField("OnFileUploadSuccess", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
                    if (eventDelegate != null)
                    {

                        foreach (var handler in eventDelegate.GetInvocationList())
                        {
                            ArchiveLogger.Msg(ConsoleColor.Red, $"OnFileUploadSuccess: calling {handler.Method.DeclaringType.Name}.{handler.Method.Name}()");
                            handler.Method.Invoke(handler.Target, new object[] { fileName });
                        }
                    }
                    else
                    {
                        ArchiveLogger.Msg(ConsoleColor.Red, $"OnFileUploadSuccess is null! (= no event handlers)");
                    }

                    ArchiveLogger.Msg(ConsoleColor.DarkYellow, "Canceled DoUploadPlayerEntityFile");
                    return false;
                }

                return true;
            }
        }*/

        [ArchivePatch(typeof(PlayFabManager), "TryGetRundownTimerData")]
        internal static class PlayFabManager_TryGetRundownTimerDataPatch
        {
            public static bool Prefix(ref bool __result, out RundownTimerData data)
            {
                data = new RundownTimerData();
                data.ShowScrambledTimer = true;
                data.ShowCountdownTimer = true;
                DateTime theDate = DateTime.Today.AddDays(5);
                data.UTC_Target_Day = theDate.Day;
                data.UTC_Target_Hour = theDate.Hour;
                data.UTC_Target_Minute = theDate.Minute;
                data.UTC_Target_Month = theDate.Month;
                data.UTC_Target_Year = theDate.Year;

                __result = true;
                return false;
            }
        }

        public static void ReadAllFilesFromDisk()
        {
            playerEntityData.Clear();
            foreach (string file in Directory.EnumerateFiles(LocalFiles.FilesDirectoryPath, "*"))
            {
                ArchiveLogger.Msg(ConsoleColor.Green, $"Reading playerEntityData file: {file}");
                string contents = File.ReadAllText(file);
                string fileName = Path.GetFileName(file);

                playerEntityData.Add(fileName, contents);
            }
        }


        // Doesn't exist for RD#001
        //[ArchivePatch(typeof(PlayFabManager), "TryGetRundownTimerData")]

        [ArchivePatch(typeof(PlayFabManager), nameof(PlayFabManager.Setup))]
        internal class PlayFabManager_SetupPatch
        {
            public static void Prefix(PlayFabManager __instance)
            {
                ArchiveLogger.Msg(ConsoleColor.Yellow, "Setting up PlayFabManager ... ");

                ArchiveLogger.Msg(ConsoleColor.DarkRed, $"SNet.Core Type = {SNet.Core.GetType()}");
            }
        }

        public static StartupScreenData StartupScreenData { get; private set; } = null;

        [ArchivePatch(typeof(PlayFabManager), "TryGetStartupScreenData")]
        internal class PlayFabManager_TryGetStartupScreenDataPatch
        {
            public static bool Prefix(eStartupScreenKey key, out StartupScreenData data, ref bool __result)
            {
                if (StartupScreenData == null)
                {
                    StartupScreenData = new StartupScreenData();
                    StartupScreenData.AllowedToStartGame = true;

                    StartupScreenData.IntroText = Utils.GetStartupTextForRundown((int) ArchiveIL2CPPModule.CurrentRundownID);
                    StartupScreenData.ShowDiscordButton = true;
                    StartupScreenData.ShowBugReportButton = false;
                    StartupScreenData.ShowRoadmapButton = true;
                    //sud.ShowOvertoneButton = false;
                    StartupScreenData.ShowIntroText = true;
                }

                __result = true;
                data = StartupScreenData;
                return false;
            }
        }

        //AddToOrUpdateLocalPlayerTitleData(string key, string value, Action OnSuccess = null)
        //AddToOrUpdateLocalPlayerTitleData(Dictionary<string, string> keys, Action OnSuccess)
        // Lol method go brrr (doesn't work)

        /*[ArchivePatch(typeof(PlayFabManager), nameof(PlayFabManager.LoginWithSteam), MethodType.Normal)]
        internal class LoginWithSteamPatch_WhyTheFuckAreYouNotWorkingTETSTSTSTSTS
        {
            public static void Prefix(*//*PlayFabManager __instance*//*)
            {
                ArchiveLogger.Msg(ConsoleColor.DarkMagenta, $"{nameof(PlayFabManager.LoginWithSteam)} method called!");
            }
        }*/

        [ArchivePatch(typeof(PlayFabManager), nameof(PlayFabManager.OnGetAuthSessionTicketResponse))]
        internal class LoginWithSteamPatch_WhyTheFuckAreYouNotWorking
        {
            public static bool Prefix(/*PlayFabManager __instance*/)
            {
                ArchiveLogger.Msg(ConsoleColor.Yellow, "OnGetAuthSessionTicketResponse() was called!");

                // TODO: fake playfab login maybe?

                //(typeof(PlayFabManager).GetField("OnLoginSuccess", BindingFlags.Public | BindingFlags.Static).GetValue(null) as Action).Invoke();
                //return false;
                if (DisableAllPlayFabInteraction)
                {
                    ArchiveLogger.Msg(ConsoleColor.Yellow, "Reading Files ... ");
                    ReadAllFilesFromDisk();
                    // other stuff maybe ?
                    ArchiveLogger.Msg(ConsoleColor.Yellow, "Trying to fake a PlayFab login ...");
                    // m_globalTitleDataLoaded && this.m_playerDataLoaded && m_loggedIn


                    /*___m_globalTitleDataLoaded = true;
                    ___m_playerDataLoaded = true;
                    ___m_entityId = "steam_" + Steamworks.SteamUser.GetSteamID().m_SteamID;
                    ___m_entityType = "Player";*/
                    PlayFabManager.Current.m_globalTitleDataLoaded = true;
                    PlayFabManager.Current.m_playerDataLoaded = true;
                    PlayFabManager.Current.m_entityId = "steamplayer_" + new System.Random().Next(int.MinValue, int.MaxValue);
                    PlayFabManager.Current.m_entityType = "Player";
                    PlayFabManager.Current.m_entityToken = "bogus_token_ " + new System.Random().Next(int.MinValue, int.MaxValue);
                    PlayFabManager.Current.m_entityLoggedIn = true;


                    PlayFabManager.PlayFabId = "pId_gczasftzasftqasgsahgjachjhcajh";// + ___m_entityId;
                    
                    //PlayFabManager.PlayerEntityFilesLoaded = true;
                    PlayFabManager.LoggedInDateTime = new Il2CppSystem.DateTime();
                    PlayFabManager.LoggedInSeconds = Clock.Time;

                    //___m_loggedIn = true;
                    IsLoggedIn = true;

                    ArchiveLogger.Msg("Starting one second timer.");
                    MelonCoroutines.Start(Il2CppUtils.DoAfter(1f, () => {
                        ArchiveLogger.Msg("One second has elapsed. - calling event OnLoginSucess()!");
                        try
                        {
                            Utilities.Il2CppUtils.CallEvent<PlayFabManager>("OnLoginSuccess");
                            Utilities.Il2CppUtils.CallEvent<PlayFabManager>("OnTitleDataUpdated");
                        }
                        catch (Exception ex)
                        {
                            ArchiveLogger.Error(ex.Message);
                            ArchiveLogger.Error(ex.StackTrace);
                        }

                    }));
/*
                    ArchiveLogger.Msg("Starting two second timer.");
                    MelonCoroutines.Start(Utils.DoAfter(2f, () => {
                        ArchiveLogger.Msg("Two seconds have elapsed. - calling event OnAllPlayerEntityFilesLoaded()!");

                        try
                        {
                            Utilities.Utils.IL2CPP.CallEvent("OnTitleDataUpdated");
                        }
                        catch (Exception ex)
                        {
                            ArchiveLogger.Error(ex.Message);
                            ArchiveLogger.Error(ex.StackTrace);
                        }
                    }));
*/


                    //
                    return false;
                }

                return true;
            }
        }

        [ArchivePatch(typeof(PlayFabManager), "Awake")]
        internal class AwakePatch
        {
            public static void Prefix(PlayFabManager __instance)
            {
                ArchiveLogger.Msg("PlayFabManager has awoken!");
                //PlayFabManager.OnAllPlayerEntityFilesLoaded += __instance_OnAllPlayerEntityFilesLoaded;

            }
        }

        //GetEntityTokenAsync
        [ArchivePatch(typeof(PlayFabManager), nameof(PlayFabManager.GetEntityTokenAsync))]
        internal class PlayFabManager_GetEntityTokenAsyncPatch
        {
            public static bool Prefix(ref Il2CppSystem.Threading.Tasks.Task<string> __result)
            {
                if (DisableAllPlayFabInteraction)
                {
                    ArchiveLogger.Msg(ConsoleColor.DarkYellow, $"Something's calling GetEntityTokenAsync.");

                    __result = Il2CppSystem.Threading.Tasks.Task.FromResult<string>("bogus_token_hfdztfc6873e2witgf78rw_42069_768dftw3768ft76fte78fet76ft67");
                    return false;
                }
                return true;   
            }
        }

        //RefreshGlobalTitleDataForKeys
        [ArchivePatch(typeof(PlayFabManager), nameof(PlayFabManager.RefreshGlobalTitleDataForKeys))]
        internal class PlayFabManager_RefreshGlobalTitleDataForKeysPatch
        {
            public static bool Prefix(Il2CppSystem.Collections.Generic.List<string> keys, Il2CppSystem.Action OnSuccess)
            {
                if (DisableAllPlayFabInteraction)
                {
                    ArchiveLogger.Msg(ConsoleColor.DarkYellow, $"Canceled RefreshGlobalTitleDataForKeys: Keys:{keys?.Count}");

                    if (keys != null)
                    {
                        foreach (string key in keys)
                        {
                            ArchiveLogger.Msg(ConsoleColor.DarkYellow, $"RefreshGlobalTitleDataForKeys -> Key:{key}");

                        }
                    }

                    OnSuccess?.Invoke();

                    return false;
                }
                return true;
            }
        }

        [ArchivePatch(typeof(PlayFabManager), "AddToOrUpdateLocalPlayerTitleData", new Type[] { typeof(string), typeof(string), typeof(Il2CppSystem.Action) })]
        internal class PlayFabManager_AddToOrUpdateLocalPlayerTitleDataPatch
        {
            public static bool Prefix(string key, string value, Il2CppSystem.Action OnSuccess)
            {
                if (DisableAllPlayFabInteraction)
                {
                    ArchiveLogger.Msg(ConsoleColor.DarkYellow, $"Canceled AddToOrUpdateLocalPlayerTitleData: Key:{key} - Value:{value}");
                    // other stuff maybe ?
                    OnSuccess?.Invoke();

                    return false;
                }

                return true;  
            }
        }

        //AddToOrUpdateLocalPlayerTitleData(Dictionary<string, string> keys, Action OnSuccess)
        [ArchivePatch(typeof(PlayFabManager), "AddToOrUpdateLocalPlayerTitleData", new Type[] { typeof(Il2CppSystem.Collections.Generic.Dictionary<string, string>), typeof(Il2CppSystem.Action) })]
        internal class PlayFabManager_AddToOrUpdateLocalPlayerTitleDataOverloadPatch
        {
            public static bool Prefix(Il2CppSystem.Collections.Generic.Dictionary<string, string> keys, Il2CppSystem.Action OnSuccess)
            {
                if (DisableAllPlayFabInteraction)
                {
                    ArchiveLogger.Msg(ConsoleColor.DarkYellow, $"Canceled AddToOrUpdateLocalPlayerTitleData(OverloadMethod): Count:{keys?.Count}");

                    if (keys != null)
                        foreach (Il2CppSystem.Collections.Generic.KeyValuePair<string, string> kvp in keys)
                        {
                            ArchiveLogger.Msg(ConsoleColor.DarkYellow, $"AddToOrUpdateLocalPlayerTitleData(OverloadMethod): Key:{kvp?.Key} - Value:{kvp?.Value}");
                        }

                    // other stuff maybe ?
                    OnSuccess?.Invoke();

                    return false;
                }

                return true;
            }
        }

        [ArchivePatch(typeof(PlayFabManager), "CloudGiveAlwaysInInventory")]
        internal class PlayFabManager_CloudGiveAlwaysInInventoryPatch
        {
            public static bool Prefix(Il2CppSystem.Action onSucess)
            {
                if (DisableAllPlayFabInteraction)
                {
                    ArchiveLogger.Msg(ConsoleColor.DarkYellow, "Canceled CloudGiveAlwaysInInventory");
                    // other stuff maybe ?
                    onSucess?.Invoke();

                    return false;
                }

                return true;
            }
        }

        [ArchivePatch(typeof(PlayFabManager), "CloudGiveItemToLocalPlayer")]
        internal class PlayFabManager_CloudGiveItemToLocalPlayerPatch
        {
            public static bool Prefix(string ItemId, Il2CppSystem.Action onSucess)
            {
                if (DisableAllPlayFabInteraction)
                {
                    ArchiveLogger.Msg(ConsoleColor.DarkYellow, $"Canceled CloudGiveItemToLocalPlayer - ItemId:{ItemId}");
                    // other stuff maybe ?
                    onSucess?.Invoke();

                    return false;
                }

                return true;
            }
        }

        [ArchivePatch(typeof(PlayFabManager), "JSONTest")]
        internal class PlayFabManager_JSONTestPatch
        {
            public static bool Prefix()
            {
                ArchiveLogger.Msg(ConsoleColor.DarkYellow, "Canceled JSONTest - why is this being run in the first place?");
                return false;
            }
        }

        // Will never be called because it's private and it's caller is patched
        /*[ArchivePatch(typeof(PlayFabManager), "RefreshGlobalTitleData")]
        internal class RefreshGlobalTitleDataPatch
        {
            public static bool Prefix(Action OnSuccess)
            {
                if (DisableAllPlayFabInteraction)
                {
                    ArchiveLogger.Msg(ConsoleColor.DarkYellow, "Canceled RefreshGlobalTitleData");
                    // other stuff maybe ?
                    OnSuccess?.Invoke();

                    return false;
                }

                return true;
            }

            public static void Postfix(PlayFabManager __instance, Dictionary<string, string> ___m_globalTitleData)
            {
                ArchiveLogger.Error("RefreshGlobalTitleData:START");
                foreach (KeyValuePair<string, string> kvp in ___m_globalTitleData)
                {
                    ArchiveLogger.Msg($"{kvp.Key} : {kvp.Value}");
                }
                ArchiveLogger.Error("RefreshGlobalTitleData:END");

            }
        }*/

        [ArchivePatch(typeof(PlayFabManager), "RefreshItemCatalog")]
        internal class PlayFabManager_RefreshItemCatalogPatch
        {
            public static bool Prefix(PlayFabManager.delUpdateItemCatalogDone OnSuccess, string catalogVersion)
            {
                if (DisableAllPlayFabInteraction)
                {
                    ArchiveLogger.Msg(ConsoleColor.DarkYellow, $"Canceled RefreshItemCatalog - catalogVersion:{catalogVersion}");
                    // other stuff maybe ?

                    OnSuccess?.Invoke(default);

                    return false;
                }

                return true;
            }
        }

        [ArchivePatch(typeof(PlayFabManager), "RefreshLocalPlayerInventory")]
        internal class RefreshLocalPlayerInventoryPatch
        {
            public static bool Prefix(PlayFabManager __instance, PlayFabManager.delUpdatePlayerInventoryDone OnSuccess)
            {
                if (DisableAllPlayFabInteraction)
                {
                    ArchiveLogger.Msg(ConsoleColor.DarkYellow, "Canceled RefreshLocalPlayerInventory");
                    // other stuff maybe ?
                    return false;
                }

                //OnSuccess += test;
                return true;
            }
            public static void test(List<ItemInstance> instance)
            {
                ArchiveLogger.Error("RefreshLocalPlayerInventory:List<ItemInstance>:START");
                foreach (ItemInstance ii in instance)
                {
                    ArchiveLogger.Msg(ConsoleColor.DarkMagenta, $"{ii}");
                }
                ArchiveLogger.Error("RefreshLocalPlayerInventory:List<ItemInstance>:END");
            }
        }


        [ArchivePatch(typeof(PlayFabManager), "RefreshLocalPlayerTitleData")]
        internal class RefreshLocalPlayerTitleDataPatch
        {
            public static bool Prefix(Il2CppSystem.Action OnSuccess, PlayFabManager __instance/*, ref Dictionary<string, string> ___m_localPlayerData*/)
            {
                if (DisableAllPlayFabInteraction)
                {
                    ArchiveLogger.Msg(ConsoleColor.DarkYellow, "Canceled RefreshLocalPlayerTitleData");
                    // other stuff maybe ?
                    //GearManager.SetupGearInOfflineMode();

                    // Set values
                    //___m_localPlayerData = playerEntityData;

                    /*__instance.m_localPlayerData.Clear();
                    foreach(Il2CppSystem.Collections.Generic.KeyValuePair<string,string> kvp in playerEntityData)
                    {
                        __instance.m_localPlayerData.Add(kvp.Key, kvp.Value);
                    }*/

                    OnSuccess?.Invoke();

                    return false;
                }

                return true;
            }

        }

        [ArchivePatch(typeof(PlayFabManager), "RefreshGlobalTitleData")]
        internal class PlayFabManager_RefreshStartupScreenTitelDataPatch
        {
            public static bool Prefix(Il2CppSystem.Action OnSuccess)
            {
                if (DisableAllPlayFabInteraction)
                {
                    ArchiveLogger.Msg(ConsoleColor.DarkYellow, "Canceled RefreshGlobalTitleData");
                    // other stuff maybe ?

                    OnSuccess?.Invoke();

                    return false;
                }

                return true;
            }
        }

        [ArchivePatch(typeof(PlayFabManager), "RefreshStoreItems")]
        internal class PlayFabManager_RefreshStoreItemsPatch
        {
            public static bool Prefix(string storeID, PlayFabManager.delUpdateStoreItemsDone OnSuccess)
            {
                if (DisableAllPlayFabInteraction)
                {
                    ArchiveLogger.Msg(ConsoleColor.DarkYellow, $"Canceled RefreshStoreItems - storeID:{storeID}");
                    // other stuff maybe ?
                    OnSuccess?.Invoke(default);

                    return false;
                }

                return true;
            }
        }

    }
}
