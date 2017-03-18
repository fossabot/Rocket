﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Rocket.API;
using Rocket.API.Assets;
using Rocket.API.Collections;
using Rocket.API.Plugins;
using Rocket.API.Providers;
using Rocket.API.Serialisation;
using Rocket.Core.Extensions;
using UnityEngine;
using Environment = Rocket.API.Environment;
using Logger = Rocket.API.Logging.Logger;
using Object = UnityEngine.Object;

namespace Rocket.Plugins.Native
{
    public class RocketPluginBase<T> : RocketPluginBase, IRocketPlugin<T> where T : class, IRocketPluginConfiguration
    {
        public IAsset<T> Configuration { get; private set; }
        public void Initialize()
        {
            base.Initialize(false);

            string configurationFile = Path.Combine(WorkingDirectory,string.Format(API.Environment.PluginConfigurationFileTemplate, Name));
            string url = null;
            if (File.Exists(configurationFile))
                url = File.ReadAllLines(configurationFile).First().Trim();

            Uri uri;
            if (url != null && Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                Configuration = new WebXMLFileAsset<T>(uri, null, (IAsset<T> config)=> { LoadPlugin(); });
            }
            else
            {
                Configuration = new XMLFileAsset<T>(configurationFile);
                LoadPlugin();
            }
        }
        public override void LoadPlugin()
        {
            base.LoadPlugin();
            Configuration.Load();
        }
    }
    
    public class RocketPluginBase : MonoBehaviour, IRocketPlugin
    {
        public string WorkingDirectory { get; internal set; }

        public event RocketPluginUnloading OnPluginUnloading;
        public event RocketPluginUnloaded OnPluginUnloaded;

        public event RocketPluginLoading OnPluginLoading;
        public event RocketPluginLoaded OnPluginLoaded;

        public static event RocketPluginUnloading OnPluginsUnloading;
        public static event RocketPluginUnloaded OnPluginsUnloaded;

        public static event RocketPluginLoading OnPluginsLoading;
        public static event RocketPluginLoaded OnPluginsLoaded;

        public IRocketPluginProvider PluginManager { get; protected set; }
        public IAsset<TranslationList> Translations { get ; private set; }
        public PluginState State { get; private set; } = PluginState.Unloaded;
        public string Name { get; protected set; }


        public bool IsDependencyLoaded(string plugin)
        {
            return PluginManager.GetPlugin(plugin) != null;
        }

        public delegate void ExecuteDependencyCodeDelegate(IRocketPlugin plugin);

        public void ExecuteDependencyCode(string plugin, ExecuteDependencyCodeDelegate a)
        {
            IRocketPlugin p = PluginManager.GetPlugin(plugin);
            if (p != null)
                a(p);
        }

        public Assembly Assembly { get { return GetType().Assembly; } }

        public virtual TranslationList DefaultTranslations
        {
            get
            {
                return new TranslationList();
            }
        }

        public virtual void Initialize(bool loadPlugin = true)
        {
            WorkingDirectory = PluginManager.GetPluginDirectory(Name);
            if (!System.IO.Directory.Exists(WorkingDirectory))
                System.IO.Directory.CreateDirectory(WorkingDirectory);

            if (DefaultTranslations != null | DefaultTranslations.Count() != 0)
            {
                Translations = new XMLFileAsset<TranslationList>(Path.Combine(WorkingDirectory, String.Format(Environment.PluginTranslationFileTemplate, Name, Environment.LanguageCode)), new Type[] { typeof(TranslationList), typeof(PropertyListEntry) }, DefaultTranslations);
                Translations.AddUnknownEntries(DefaultTranslations);
            }
            if(loadPlugin)
                LoadPlugin();
        }

        public string Translate(string translationKey, params object[] placeholder)
        {
            return Translations.Instance.Translate(translationKey, placeholder);
        }

        public void ReloadPlugin()
        {
            UnloadPlugin();
            LoadPlugin();
        }

        public virtual void LoadPlugin()
        {
            Logger.Info("\n[loading] " + name);
            Translations.Load();

            try
            {
                Load();
            }
            catch (Exception ex)
            {
                Logger.Fatal("Failed to load " + Name+ ", unloading now...", ex);
                try
                {
                    UnloadPlugin(PluginState.Failure);
                    return;
                }
                catch (Exception ex1)
                {
                    Logger.Fatal("Failed to unload " + Name ,ex1);
                }
            }

            bool doCancelLoading = false;
            bool cancelLoading = false;
            if (OnPluginLoading != null)
            {
                foreach (var handler in OnPluginLoading.GetInvocationList().Cast<RocketPluginLoading>())
                {
                    try
                    {
                        handler(this, ref cancelLoading);
                        if (cancelLoading) doCancelLoading = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Fatal(ex);
                    }
                }
            }

            if (OnPluginsLoading != null)
            {
                foreach (var handler in OnPluginsLoading.GetInvocationList().Cast<RocketPluginLoading>())
                {
                    try
                    {
                        handler(this, ref cancelLoading);
                        if (cancelLoading) doCancelLoading = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Fatal(ex);
                    }
                }
            }

            if (doCancelLoading)
            {
                try
                {
                    UnloadPlugin(PluginState.Cancelled);
                    return;
                }
                catch (Exception ex1)
                {
                    Logger.Fatal("Failed to unload " + Name, ex1);
                }
            }

            State = PluginState.Loaded;
            OnPluginLoaded.TryInvoke(this);
            OnPluginsLoaded.TryInvoke(this);
        }

        public virtual void UnloadPlugin(PluginState state = PluginState.Unloaded)
        {
            Logger.Info("\n[unloading] " + Name);
            OnPluginUnloading.TryInvoke(this);
            OnPluginsUnloading.TryInvoke(this);
            Unload();
            State = state;
            OnPluginUnloaded.TryInvoke(this);
            OnPluginsUnloaded.TryInvoke(this);
        }

        private void OnDisable()
        {
            UnloadPlugin();
        }

        protected virtual void Load()
        {
        }

        protected virtual void Unload()
        {
        }

        public void DestroyPlugin()
        {
            Destroy(this);
        }
    }
}