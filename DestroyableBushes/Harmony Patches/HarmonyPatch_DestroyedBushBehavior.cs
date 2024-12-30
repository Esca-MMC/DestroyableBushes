using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;

namespace DestroyableBushes
{
    public static class HarmonyPatch_DestroyedBushBehavior
    {
        /// <summary>Applies this Harmony patch to the game through the provided instance.</summary>
        /// <param name="harmony">This mod's Harmony instance.</param>
        public static void ApplyPatch(Harmony harmony)
        {
            try
            {
                ModEntry.Instance.Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_DestroyedBushBehavior)}\": postfixing SDV method \"Bush.performToolAction(Tool, int, Vector2)\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(Bush), nameof(Bush.performToolAction), new[] { typeof(Tool), typeof(int), typeof(Vector2) }),
                    postfix: new HarmonyMethod(typeof(HarmonyPatch_DestroyedBushBehavior), nameof(performToolAction_Postfix))
                );
            }
            catch (Exception ex)
            {
                ModEntry.Instance.Monitor.LogOnce($"\"{nameof(HarmonyPatch_DestroyedBushBehavior)}\" encountered an error while applying patches. Bushes might not drop wood or regrow when destroyed. Full error message:\n{ex.ToString()}", LogLevel.Error);
            }
        }

        /// <summary>If this bush was destroyed, add it to this mod's "destroyed bushes" list. Also provide foraging experience and drop an amount of wood, based on this mod's config settings.</summary>
        /// <param name="t">The <see cref="Tool"/> used on this bush.</param>
        /// <param name="tileLocation">The tile on which the tool is being used.</param>
        /// <param name="__instance">The <see cref="Bush"/> on which a tool is being used.</param>
        /// <param name="__result">True if this bush was destroyed."/></param>
        public static void performToolAction_Postfix(Tool t, Vector2 tileLocation, Bush __instance, bool __result)
        {
            try
            {
                if (__result) //if this bush was destroyed
                {
                    int amountOfWood; //the amount of wood this bush should drop
                    int amountOfXP; //the amount of foraging experience this bush should give
                    bool shouldRegrow; //whether this bush should eventually be respawned

                    switch (__instance.size.Value) //based on the bush's size, set the amount of wood
                    {
                        case Bush.smallBush:
                            amountOfWood = ModEntry.Config?.AmountOfWoodDropped?.SmallBushes ?? 0;
                            amountOfXP = ModEntry.Config?.AmountOfExperienceGained?.SmallBushes ?? 0;
                            shouldRegrow = true;
                            break;
                        case Bush.mediumBush:
                            amountOfWood = ModEntry.Config?.AmountOfWoodDropped?.MediumBushes ?? 0;
                            amountOfXP = ModEntry.Config?.AmountOfExperienceGained?.MediumBushes ?? 0;
                            shouldRegrow = true;
                            break;
                        case Bush.largeBush:
                            amountOfWood = ModEntry.Config?.AmountOfWoodDropped?.LargeBushes ?? 0;
                            amountOfXP = ModEntry.Config?.AmountOfExperienceGained?.LargeBushes ?? 0;
                            shouldRegrow = true;
                            break;
                        case Bush.walnutBush:
                            amountOfWood = ModEntry.Config?.AmountOfWoodDropped?.WalnutBushes ?? 0;
                            amountOfXP = ModEntry.Config?.AmountOfExperienceGained?.WalnutBushes ?? 0;
                            shouldRegrow = true;
                            break;
                        case Bush.greenTeaBush:
                            amountOfWood = ModEntry.Config?.AmountOfWoodDropped?.GreenTeaBushes ?? 0;
                            amountOfXP = ModEntry.Config?.AmountOfExperienceGained?.GreenTeaBushes ?? 0;
                            shouldRegrow = false;
                            break;
                        default:
                            amountOfWood = 0;
                            amountOfXP = 0;
                            shouldRegrow = false;
                            break;
                    }

                    if (shouldRegrow) //if this bush should eventually be respawned
                    {
                        int? safeOffset = null; //the bush's tilesheetOffset (or null if it should be ignored)
                        if (__instance.size.Value != Bush.walnutBush) //if this is NOT a walnut bush
                        {
                            if (__instance.size.Value != Bush.mediumBush || __instance.townBush.Value) //and this is NOT a berry bush (i.e. a medium non-town bush)
                            {
                                safeOffset = __instance.tileSheetOffset.Value; //get the bush's offset
                            }
                        }
                        else //if this is a walnut bush
                        {
                            safeOffset = 0; //force it to respawn without a walnut
                        }

                        ModData.DestroyedBush destroyed = new ModData.DestroyedBush(__instance.Location?.Name, __instance.Tile, __instance.size.Value, __instance.townBush.Value, safeOffset); //create a record of this bush

                        if (Context.IsMainPlayer) //if this code is run by the main player
                            ModEntry.Data.DestroyedBushes.Add(destroyed); //add the record to the list of destroyed bushes
                        else //if this code is run by a multiplayer farmhand
                            ModEntry.Instance.Helper.Multiplayer.SendMessage //send the record to the main player
                            (
                                message: destroyed,
                                messageType: "DestroyedBush",
                                modIDs: new[] { ModEntry.Instance.ModManifest.UniqueID },
                                playerIDs: new[] { Game1.serverHost.Value.UniqueMultiplayerID }
                            );
                    }

                    Farmer farmer = t?.getLastFarmerToUse(); //try to get the player who destroyed this bush

                    if (amountOfWood > 0) //if this bush should drop any wood
                    {
                        if (farmer?.professions.Contains(Farmer.forester) == true) //if the player destroying this bush has the "Forester" profession
                        {
                            double multipliedWood = 1.25 * amountOfWood; //increase wood by 25%
                            amountOfWood = (int)Math.Floor(multipliedWood); //update the amount of wood (round down)
                            if (multipliedWood > amountOfWood) //if the multiplied wood had a decimal
                            {
                                multipliedWood -= amountOfWood; //get the decimal amount of wood
                                if (Game1.random.NextDouble() < multipliedWood) //use the decimal as a random chance
                                {
                                    amountOfWood++; //add 1 wood
                                }
                            }
                        }

                        //if the player has read the book "Woody's Secret", add a 5% chance to double the wood output (based on trees; see Tree.performToolAction)
                        if (farmer?.stats.Get("Book_Woodcutting") != 0 && Game1.random.NextDouble() < 0.05)
                        {
                            amountOfWood *= 2;
                        }

                        //drop the amount of wood at this bush's location
                        Game1.createRadialDebris(__instance.Location, Debris.woodDebris, (int)tileLocation.X, (int)tileLocation.Y, amountOfWood, true, -1, false, null);
                    }

                    if (amountOfXP > 0) //if this bush should give any experience
                    {
                        farmer?.gainExperience(Farmer.foragingSkill, amountOfXP); //gain foraging skill xp
                    }
                }
            }
            catch (Exception ex)
            {
                ModEntry.Instance.Monitor.LogOnce($"Harmony patch \"{nameof(performToolAction_Postfix)}\" has encountered an error:\n{ex.ToString()}", LogLevel.Error);
            }
        }
    }
}
