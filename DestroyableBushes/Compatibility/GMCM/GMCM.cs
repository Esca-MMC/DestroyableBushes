﻿using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Collections.Generic;

namespace DestroyableBushes
{
    /// <summary>Handles this mod's option menu for Generic Mod Config Menu (GMCM).</summary>
    public static class GMCM
    {
        /// <summary>This mod's SMAPI monitor.</summary>
        private static IMonitor Monitor => ModEntry.Instance.Monitor;
        /// <summary>This mod's SMAPI helper.</summary>
        private static IModHelper Helper => ModEntry.Instance.Helper;
        /// <summary>This mod's SMAPI manifest.</summary>
        private static IManifest ModManifest => ModEntry.Instance.ModManifest;
        /// <summary>This mod's "config.json" class instance.</summary>
        private static ModConfig Config
        {
            get => ModEntry.Config;
            set => ModEntry.Config = value;
        }

        /// <summary>True if the method <see cref="Enable"/> has already run once.</summary>
        /// <remarks>This does NOT indicate whether GMCM is installed, nor whether this mod's menu is actually enabled.</remarks>
        private static bool InitializedGMCM { get; set; } = false;

        /// <summary>A SMAPI event that initializes this mod's GMCM option menu if possible.</summary>
        /// <remarks>This SMAPI event type was used to avoid issues with GMCM readiness timing. Another event may be more appropriate with later GMCM versions.</remarks>
        public static void Enable(object sender, RenderedActiveMenuEventArgs e)
        {
            if (InitializedGMCM)
                return; //do nothing
            InitializedGMCM = true; //prevent this method running again after this

            try
            {
                var api = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu"); //attempt to get GMCM's API instance

                if (api == null)
                    return;

                //create this mod's menu
                api.Register
                (
                    mod: ModManifest,
                    reset: () => Config = new ModConfig(),
                    save: () => Helper.WriteConfig(Config),
                    titleScreenOnly: false
                );

                //register an option for each config setting
                api.AddTextOption
                (
                    mod: ModManifest,
                    getValue: () => Config.WhenBushesRegrow ?? "",
                    setValue: (string val) => Config.WhenBushesRegrow = val, //note: validated/converted within the config code
                    name: () => Helper.Translation.Get("WhenBushesRegrow.Name"),
                    tooltip: () => Helper.Translation.Get("WhenBushesRegrow.Desc")
                );

                api.AddTextOption
                (
                    mod: ModManifest,
                    getValue: () => GMCMLocationList,
                    setValue: (string val) => GMCMLocationList = val,
                    name: () => Helper.Translation.Get("DestroyableBushLocations.Name"),
                    tooltip: () => Helper.Translation.Get("DestroyableBushLocations.Desc")
                );

                api.AddSectionTitle
                (
                    mod: ModManifest,
                    text: () => Helper.Translation.Get("DestroyableBushTypes.Name"),
                    tooltip: () => Helper.Translation.Get("DestroyableBushTypes.Desc")
                );

                api.AddBoolOption
                (
                    mod: ModManifest,
                    getValue: () => Config.DestroyableBushTypes.SmallBushes,
                    setValue: (bool val) => Config.DestroyableBushTypes.SmallBushes = val,
                    name: () => Helper.Translation.Get("DestroyableBushTypes.SmallBushes.Name"),
                    tooltip: () => Helper.Translation.Get("DestroyableBushTypes.SmallBushes.Desc")
                );

                api.AddBoolOption
                (
                    mod: ModManifest,
                    getValue: () => Config.DestroyableBushTypes.MediumBushes,
                    setValue: (bool val) => Config.DestroyableBushTypes.MediumBushes = val,
                    name: () => Helper.Translation.Get("DestroyableBushTypes.MediumBushes.Name"),
                    tooltip: () => Helper.Translation.Get("DestroyableBushTypes.MediumBushes.Desc")
                );

                api.AddBoolOption
                (
                    mod: ModManifest,
                    getValue: () => Config.DestroyableBushTypes.LargeBushes,
                    setValue: (bool val) => Config.DestroyableBushTypes.LargeBushes = val,
                    name: () => Helper.Translation.Get("DestroyableBushTypes.LargeBushes.Name"),
                    tooltip: () => Helper.Translation.Get("DestroyableBushTypes.LargeBushes.Desc")
                );

                api.AddSectionTitle
                (
                    mod: ModManifest,
                    text: () => Helper.Translation.Get("AmountOfWoodDropped.Name"),
                    tooltip: () => Helper.Translation.Get("AmountOfWoodDropped.Desc")
                );

                api.AddNumberOption
                (
                    mod: ModManifest,
                    getValue: () => Config.AmountOfWoodDropped.SmallBushes,
                    setValue: (int val) => Config.AmountOfWoodDropped.SmallBushes = val,
                    name: () => Helper.Translation.Get("AmountOfWoodDropped.SmallBushes.Name"),
                    tooltip: () => Helper.Translation.Get("AmountOfWoodDropped.SmallBushes.Desc"),
                    min: 0
                );

                api.AddNumberOption
                (
                    mod: ModManifest,
                    getValue: () => Config.AmountOfWoodDropped.MediumBushes,
                    setValue: (int val) => Config.AmountOfWoodDropped.MediumBushes = val,
                    name: () => Helper.Translation.Get("AmountOfWoodDropped.MediumBushes.Name"),
                    tooltip: () => Helper.Translation.Get("AmountOfWoodDropped.MediumBushes.Desc"),
                    min: 0
                );

                api.AddNumberOption
                (
                    mod: ModManifest,
                    getValue: () => Config.AmountOfWoodDropped.LargeBushes,
                    setValue: (int val) => Config.AmountOfWoodDropped.LargeBushes = val,
                    name: () => Helper.Translation.Get("AmountOfWoodDropped.LargeBushes.Name"),
                    tooltip: () => Helper.Translation.Get("AmountOfWoodDropped.LargeBushes.Desc"),
                    min: 0
                );

                api.AddNumberOption
                (
                    mod: ModManifest,
                    getValue: () => Config.AmountOfWoodDropped.GreenTeaBushes,
                    setValue: (int val) => Config.AmountOfWoodDropped.GreenTeaBushes = val,
                    name: () => Helper.Translation.Get("AmountOfWoodDropped.GreenTeaBushes.Name"),
                    tooltip: () => Helper.Translation.Get("AmountOfWoodDropped.GreenTeaBushes.Desc"),
                    min: 0
                );
            }
            catch (Exception ex)
            {
                Monitor.Log($"An error happened while loading this mod's GMCM options menu. Its menu might be missing or fail to work. The auto-generated error message has been added to the log.", LogLevel.Warn);
                Monitor.Log($"----------", LogLevel.Trace);
                Monitor.Log($"{ex.ToString()}", LogLevel.Trace);
            }
        }

        /// <summary>Converts <see cref="ModConfig.DestroyableBushLocations"/> between a string and a list.</summary>
        private static string GMCMLocationList
        {
            get
            {
                string result = "";
                int total = Config.DestroyableBushLocations?.Count ?? 0; //get the number of listed locations (or 0 if null)

                if (total > 0) //if any locations are listed
                {
                    result = Config.DestroyableBushLocations[0]; //start with the first location name

                    for (int x = 1; x < total; x++) //for each location after the first
                    {
                        result += ", " + Config.DestroyableBushLocations[x]; //add a comma, space, and the next location name
                    }
                }

                return result;
            }

            set
            {
                List<string> locationList = new List<string>(); //create an empty list

                if (!string.IsNullOrWhiteSpace(value)) //if the provided string is NOT null or otherwise blank
                {
                    string[] locations = value.Split(','); //get an array of location names that were separated by commas

                    foreach (string location in locations) //for each location name
                    {
                        locationList.Add(location.Trim()); //add the (whitespace-trimmed) location name to the list
                    }
                }

                Config.DestroyableBushLocations = locationList; //set the created list
            }
        }
    }
}
