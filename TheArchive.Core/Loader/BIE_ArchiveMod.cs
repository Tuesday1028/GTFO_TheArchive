﻿#if BepInEx
using BepInEx;
using BepInEx.Unity.IL2CPP;
using System;
using UnityEngine;

namespace TheArchive.Loader
{
    [BepInPlugin(ArchiveMod.GUID, ArchiveMod.MOD_NAME, ArchiveMod.VERSION_STRING)]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ArchiveMod.MTFO_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class BIE_ArchiveMod : BasePlugin
    {
        public static MonoBehaviour MainComponent { get; private set; }

        public override void Load()
        {
            var harmony = new HarmonyLib.Harmony(ArchiveMod.GUID);
            ArchiveMod.OnApplicationStart(LoaderWrapper.WrapLogger(Log), harmony);

            Application.add_quitting((Il2CppSystem.Action) (() => ArchiveMod.OnApplicationQuit()));

            MainComponent = AddComponent<TheArchive_BIE_Controller>();
        }

        public override bool Unload()
        {
            ArchiveMod.OnApplicationQuit();

            return base.Unload();
        }

        public class TheArchive_BIE_Controller : UnityEngine.MonoBehaviour
        {
            public TheArchive_BIE_Controller(IntPtr ptr) : base(ptr) { }

            public void Awake()
            {
                DontDestroyOnLoad(this);
                hideFlags = UnityEngine.HideFlags.HideAndDontSave;
            }

            public void Update()
            {
                ArchiveMod.OnUpdate();
            }

            public void LateUpdate()
            {
                ArchiveMod.OnLateUpdate();
            }
        }
    }
}
#endif

