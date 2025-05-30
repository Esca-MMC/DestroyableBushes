﻿using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace DestroyableBushes
{
    /// <summary>A Harmony patch that makes bushes destroyable based on config.json file settings.</summary>
    public static class HarmonyPatch_BushesAreDestroyable
    {
        /// <summary>Applies this Harmony patch to the game through the provided instance.</summary>
        /// <param name="harmony">This mod's Harmony instance.</param>
        public static void ApplyPatch(Harmony harmony)
        {
            try
            {
                ModEntry.Instance.Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_BushesAreDestroyable)}\": transpiling SDV method \"Bush.performToolAction(Tool, int, Vector2)\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(Bush), nameof(Bush.performToolAction)),
                    transpiler: new HarmonyMethod(typeof(HarmonyPatch_BushesAreDestroyable), nameof(performToolAction_Transpiler))
                );

                ModEntry.Instance.Monitor.Log($"Applying Harmony patch \"{nameof(HarmonyPatch_BushesAreDestroyable)}\": postfixing SDV method \"Bush.isDestroyable()\".", LogLevel.Trace);
                harmony.Patch(
                    original: AccessTools.Method(typeof(Bush), nameof(Bush.isDestroyable)),
                    postfix: new HarmonyMethod(typeof(HarmonyPatch_BushesAreDestroyable), nameof(isDestroyable_Postfix))
                );
            }
            catch (Exception ex)
            {
                ModEntry.Instance.Monitor.LogOnce($"\"{nameof(HarmonyPatch_BushesAreDestroyable)}\" encountered an error while applying patches. Bushes might be indestructible. Full error message:\n{ex.ToString()}", LogLevel.Error);
            }
        }


        /*
            Old C#:
                if (size == 4)
                    return false;
                [...]
                if ((int)axe.upgradeLevel >= 1 || (int)this.size == 3)
                [...]
                this.health -= (((int)this.size == 3) ? 0.5f : ((float)(int)axe.upgradeLevel / 5f));
             
            New C#:
                if (0 == 4)
                    return false;
                [...]
                if ((int)axe.upgradeLevel >= getUpgradeLevelRequirement() || (int)this.size == 3)
                [...]
                this.health -= (((int)this.size == 3) ? 0.5f : modifyAxeDamage(((float)(int)axe.upgradeLevel / 5f)));
             
            Old IL:
                IL_000b: ldarg.0
	            IL_000c: ldfld class Netcode.NetInt StardewValley.TerrainFeatures.Bush::size
	            IL_0011: call !0 class Netcode.NetFieldBase`2int32, class Netcode.NetInt::op_Implicit(class Netcode.NetFieldBase`2!0, !1)
	            IL_0016: ldc.i4.4
	            IL_0017: bne.un.s IL_001b
                [...]
                IL_009e: ldloc.2
		        IL_009f: ldfld class Netcode.NetInt StardewValley.Tool::upgradeLevel
		        IL_00a4: call int32 Netcode.NetInt::op_Implicit(class Netcode.NetInt)
		        IL_00a9: ldc.i4.1
		        IL_00aa: bge.s IL_00bd
                [...]
                IL_00d2: ldloc.2
		        IL_00d3: ldfld class Netcode.NetInt StardewValley.Tool::upgradeLevel
		        IL_00d8: call int32 Netcode.NetInt::op_Implicit(class Netcode.NetInt)
		        IL_00dd: conv.r4
		        IL_00de: ldc.r4 5
		        IL_00e3: div
		        IL_00e4: br.s IL_00eb
             
            New IL:
                IL_000b: ldarg.0
	            IL_000c: ldfld class Netcode.NetInt StardewValley.TerrainFeatures.Bush::size
	            IL_0011: call !0 class Netcode.NetFieldBase`2int32, class Netcode.NetInt::op_Implicit(class Netcode.NetFieldBase`2!0, !1)
                    (?): pop
                    (?): ldc.i4.0
	            IL_0016: ldc.i4.4
	            IL_0017: bne.un.s IL_001b
                [...]
                IL_009e: ldloc.2
		        IL_009f: ldfld class Netcode.NetInt StardewValley.Tool::upgradeLevel
		        IL_00a4: call int32 Netcode.NetInt::op_Implicit(class Netcode.NetInt)
		        IL_00a9: call int32 DestroyableBushes.HarmonyPatch_BushesAreDestroyable::getUpgradeLevelRequirement()
		        IL_00aa: bge.s IL_00bd
                [...]
                IL_00d2: ldloc.2
		        IL_00d3: ldfld class Netcode.NetInt StardewValley.Tool::upgradeLevel
		        IL_00d8: call int32 Netcode.NetInt::op_Implicit(class Netcode.NetInt)
		        IL_00dd: conv.r4
		        IL_00de: ldc.r4 5
		        IL_00e3: div
                    (?): call float DestroyableBushes.HarmonyPatch_BushesAreDestroyable::modifyAxeDamage(float)
		        IL_00e4: br.s IL_00eb
        */

        /// <summary>Allows walnut bushes to be destroyed, modifies the axe upgrades required to destroy non-tea bushes, and modifies the number of hits required to destroy them.</summary>
        /// <param name="instructions">The original method's CIL code.</param>
        private static IEnumerable<CodeInstruction> performToolAction_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                List<CodeInstruction> patched = new List<CodeInstruction>(instructions); //make a copy of the instructions to modify

                var sizeField = AccessTools.Field(typeof(Bush), "size"); //get the field info for Bush.size

                for (int x = patched.Count - 3; x >= 0; x--) //for each instruction, looping backward, skipping the last 2
                {
                    //if these instructions push Bush.size's value and then the integer 4 onto the stack
                    if (patched[x].opcode == OpCodes.Ldfld && patched[x].operand?.Equals(sizeField) == true
                     && (patched[x + 1].opcode == OpCodes.Call || patched[x + 1].opcode == OpCodes.Callvirt)
                     && patched[x + 2].opcode == OpCodes.Ldc_I4_4)
                    {
                        //before the integer 4 is pushed onto the stack, remove the Bush.size value, then replace it with integer 0
                        patched.InsertRange(x + 2,
                        [
                            new CodeInstruction(OpCodes.Pop),
                            new CodeInstruction(OpCodes.Ldc_I4_0)
                        ]);

                        ModEntry.Instance.Monitor.VerboseLog($"Transpiler modified \"Bush.size == 4\" to \"0 == 4\" near instruction #{x}.");
                    }
                }

                var upgradeField = AccessTools.Field(typeof(Tool), "upgradeLevel"); //get the field info for Tool.upgradeLevel (NOT "Tool.UpgradeLevel", "Axe.upgradeLevel", etc)

                var requirementMethod = AccessTools.Method(typeof(HarmonyPatch_BushesAreDestroyable), nameof(getUpgradeLevelRequirement)); //get the method to use when replacing the upgrade requirement

                for (int x = patched.Count - 3; x >= 0; x--) //for each instruction, looping backward, skipping the last 2
                {
                    //if these instructions push axe.upgradeLevel's value and then the integer 1 onto the stack
                    if (patched[x].opcode == OpCodes.Ldfld && patched[x].operand?.Equals(upgradeField) == true
                     && (patched[x + 1].opcode == OpCodes.Call || patched[x + 1].opcode == OpCodes.Callvirt)
                     && patched[x + 2].opcode == OpCodes.Ldc_I4_1)
                    {
                        var replacement = new CodeInstruction(OpCodes.Call, requirementMethod); //create an instruction to call the config-based requirement check
                        replacement = replacement.WithLabels(patched[x + 2].labels); //copy the labels from the original "push integer 1" instruction, if any
                        patched[x + 2] = replacement; //replace the original instruction with the new one

                        ModEntry.Instance.Monitor.VerboseLog($"Transpiler replaced \"axe.upgradeLevel >= 1\" with a configurable value at line {x}.");
                    }
                }

                var damageMethod = AccessTools.Method(typeof(HarmonyPatch_BushesAreDestroyable), nameof(modifyAxeDamage)); //get the method to use when modifying 

                for (int x = patched.Count - 5; x >= 0; x--) //for each instruction, looping backward, skipping the last 4
                {
                    //if these instructions push axe.upgradeLevel's value onto the stack, convert it to a float, and divide it by 5
                    if (patched[x].opcode == OpCodes.Ldfld && patched[x].operand?.Equals(upgradeField) == true
                     && (patched[x + 1].opcode == OpCodes.Call || patched[x + 1].opcode == OpCodes.Callvirt)
                     && patched[x + 2].opcode == OpCodes.Conv_R4
                     && patched[x + 3].opcode == OpCodes.Ldc_R4
                     && patched[x + 4].opcode == OpCodes.Div)
                    {
                        //after the original value is calculated, add a method call to conditionally modify it
                        patched.InsertRange(x + 5,
                        [
                            new CodeInstruction(OpCodes.Ldarg_0), //load this Bush instance onto the stack
                            new CodeInstruction(OpCodes.Call, damageMethod) //call the damage method
                        ]);

                        ModEntry.Instance.Monitor.VerboseLog($"Transpiler replaced \"axe.upgradeLevel / 5f\" with a conditional value at line {x}.");
                    }
                }

                return patched; //return the patched instructions
            }
            catch (Exception ex)
            {
                ModEntry.Instance.Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_BushesAreDestroyable)}\" has encountered an error. Transpiler \"{nameof(performToolAction_Transpiler)}\" will not be applied. Bushes may be indestructible. Full error message:\n{ex.ToString()}", LogLevel.Error);
                return instructions; //return the original instructions
            }
        }

        /// <summary>Gets the number of upgrades required for an axe to cut down bushes, based on user configuration settings.</summary>
        /// <returns>The number of upgrades required. 0 is the default axe, 1 is the copper axe, etc.</returns>
        private static int getUpgradeLevelRequirement()
        {
            return ModEntry.Config?.AxeUpgradesRequired ?? 0; //use the config value if available, or default to 0
        }

        /// <summary>Modifies the damage value used by axes when hitting non-tea bushes.</summary>
        /// <param name="oldDamage">The original damage value produced by the game.</param>
        /// <param name="bush">The bush being hit.</param>
        /// <returns>The modified damage value to use.</returns>
        /// <remarks>As of SDV 1.6.8, bushes normally start with 0 health, take 0.2 damage per axe upgrade when hit, and are destroyed at -1 health. Tea bushes always take 0.5 damage instead.</remarks>
        private static float modifyAxeDamage(float oldDamage, Bush bush)
        {
            float newDamage = Math.Max(oldDamage, 0.125f); //deal at least 0.125 damage (i.e. destroy typical bushes in 8 hits or less)

            newDamage *= ModEntry.Config?.AxeDamageMultiplier ?? 1; //multiply damage based on the player's config, or 1 if unavailable

            //get the relevant value from "bush type durability"
            float bushTypeDurability = 1;
            if (ModEntry.Config?.BushTypeDurability != null)
            {
                switch (bush.size.Value)
                {
                    case Bush.smallBush:
                        bushTypeDurability = ModEntry.Config.BushTypeDurability.SmallBushes;
                        break;
                    case Bush.mediumBush:
                        bushTypeDurability = ModEntry.Config.BushTypeDurability.MediumBushes;
                        break;
                    case Bush.largeBush:
                        bushTypeDurability = ModEntry.Config.BushTypeDurability.LargeBushes;
                        break;
                    case Bush.walnutBush:
                        bushTypeDurability = ModEntry.Config.BushTypeDurability.WalnutBushes;
                        break;
                }
            }

            newDamage /= bushTypeDurability; //divide damage by the bush's durability multiplier (note: durability > 1 reduces damage, durability < 1 increases damage)

            if (bush.health == 0f && bush.size.Value == 4) //if this is the player's first hit on a walnut bush
                return Math.Min(newDamage, 0.9f); //limit damage to prevent destroying it in a single hit (which causes issues with walnut drops)

            return newDamage;
        }

        /// <summary>Allows bushes to be destroyed when they normally wouldn't, if conditions match this mod's settings.</summary>
        /// <param name="__instance">The <see cref="Bush"/> being checked.</param>
        /// <param name="__result">True if this bush is destroyable.</param>
        private static void isDestroyable_Postfix(Bush __instance, ref bool __result)
        {
            try
            {
                /* check location settings */

                bool allowedAtThisLocation = false;

                if (ModEntry.Config.DestroyableBushLocations?.Count > 0) //if the location list has any entries
                {
                    foreach (string locationName in ModEntry.Config.DestroyableBushLocations) //for each name in the list
                    {
                        if (locationName.Equals(__instance.Location?.Name ?? "", StringComparison.OrdinalIgnoreCase)) //if the listed name matches the bush's location name
                        {
                            allowedAtThisLocation = true;
                        }
                    }
                }
                else //if no locations are listed
                    allowedAtThisLocation = true;

                if (!allowedAtThisLocation) //if this is location is NOT allowed
                    return; //don't change anything

                /* check bush size settings */

                switch (__instance.size.Value)
                {
                    case Bush.smallBush:
                        if (ModEntry.Config.DestroyableBushTypes.SmallBushes) //if settings allow this bush size to be destroyed
                            __result = true; //override the original result
                        return;
                    case Bush.mediumBush:
                        if (ModEntry.Config.DestroyableBushTypes.MediumBushes)
                            __result = true;
                        return;
                    case Bush.largeBush:
                        if (ModEntry.Config.DestroyableBushTypes.LargeBushes)
                            __result = true;
                        return;
                    case Bush.walnutBush:
                        if (ModEntry.Config.DestroyableBushTypes.WalnutBushes)
                            __result = true;
                        return;

                }
            }
            catch (Exception ex)
            {
                ModEntry.Instance.Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_BushesAreDestroyable)}\" has encountered an error. Bushes might be indestructible. Full error message:\n{ex.ToString()}", LogLevel.Error);
            }
        }
    }
}
