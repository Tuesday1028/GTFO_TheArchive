﻿using GameData;
using LevelGeneration;
using System;
using TheArchive.Core;
using TheArchive.Core.Attributes;
using TheArchive.Utilities;
using UnityEngine.CrashReportHandler;

[assembly: ModDefaultFeatureGroupName("TheArchive")]
namespace TheArchive
{
    [ArchiveModule(ArchiveMod.GUID, ArchiveMod.MOD_NAME, ArchiveMod.VERSION_STRING)]
    public class ArchiveIL2CPPModule : IArchiveModule
    {
        internal static ArchiveIL2CPPModule instance;

        public static event Action<eGameStateName> OnGameStateChanged;

        public bool ApplyHarmonyPatches => false;
        public bool UsesLegacyPatches => false;

        public ArchiveLegacyPatcher Patcher { get; set; }

        public string ModuleGroup => ArchiveMod.ARCHIVE_CORE_FEATURE_GROUP;

        static ArchiveIL2CPPModule()
        {
            typeof(EnemyDataBlock).RegisterSelf();
            typeof(GameDataBlockBase<>).RegisterSelf();
            typeof(GameDataBlockWrapper<>).RegisterSelf();
            typeof(eGameStateName).RegisterSelf();
            typeof(LG_Area).RegisterSelf();
            typeof(Il2CppSystem.Collections.Generic.List<>).RegisterForIdentifier("GenericList");
        }

        public void Init()
        {
            instance = this;

            CrashReportHandler.SetUserMetadata("Modded", "true");
            CrashReportHandler.enableCaptureExceptions = false;

            ArchiveMod.GameStateChanged += (eGameStateName_state) => OnGameStateChanged?.Invoke((eGameStateName) eGameStateName_state);
        }

        public void OnSceneWasLoaded(int buildIndex, string sceneName)
        {

        }

        public void OnLateUpdate()
        {

        }

        public void OnExit()
        {

        }

    }
}
