using HarmonyLib;
using StardewModdingAPI;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;

namespace DestroyableBushes
{
    /// <summary>A Harmony patch that makes bushes destroyable based on config.json file settings.</summary>
    public static class HarmonyPatch_BushesAreDestroyable
    {
        /// <summary>Applies this Harmony patch to the game through the provided instance.</summary>
        /// <param name="harmony">This mod's Harmony instance.</param>
        public static void ApplyPatch(Harmony harmony)
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


        /*
            Old C#:
                if (size == 4) //note: walnut bushes are size 4
                    return false;
             
            New C#:
                _ = size; //discard size's value after loading it
                if (0 == 4)
                    return false;
             
            Old IL:
                IL_000b: ldarg.0
	            IL_000c: ldfld class Netcode.NetInt StardewValley.TerrainFeatures.Bush::size
	            IL_0011: call !0 class Netcode.NetFieldBase`2int32, class Netcode.NetInt::op_Implicit(class Netcode.NetFieldBase`2!0, !1)
	            IL_0016: ldc.i4.4
	            IL_0017: bne.un.s IL_001b
             
            New IL:
                IL_000b: ldarg.0
	            IL_000c: ldfld class Netcode.NetInt StardewValley.TerrainFeatures.Bush::size
	            IL_0011: call !0 class Netcode.NetFieldBase`2int32, class Netcode.NetInt::op_Implicit(class Netcode.NetFieldBase`2!0, !1)
                    (?): pop
                    (?): ldc.i4.0
	            IL_0016: ldc.i4.4
	            IL_0017: bne.un.s IL_001b
        */

        /// <summary>Allows walnut bushes to be destroyed.</summary>
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
                        patched.InsertRange(x + 2, [
                            new CodeInstruction(OpCodes.Pop),
                            new CodeInstruction(OpCodes.Ldc_I4_0)
                        ]);

                        ModEntry.Instance.Monitor.VerboseLog($"Transpiler replaced \"Bush.size == 4\" with \"0 == 4\" at line {x}.");
                    }
                }

                return patched; //return the patched instructions
            }
            catch (Exception ex)
            {
                ModEntry.Instance.Monitor.LogOnce($"Harmony patch \"{nameof(HarmonyPatch_BushesAreDestroyable)}\" has encountered an error. Transpiler \"{nameof(performToolAction_Transpiler)}\" will not be applied. Full error message:\n{ex.ToString()}", LogLevel.Error);
                return instructions; //return the original instructions
            }
        }

        /// <summary>Makes all bushes destroyable by appropriate tools.</summary>
        /// <remarks>
        /// This causes <see cref="Bush.isDestroyable()"/> to return true, allowing axes with at least 1 upgrade to destroy bushes.
        /// </remarks>
        /// <param name="__instance">The <see cref="Bush"/> being checked.</param>
        /// <param name="__result">True if this bush is destroyable.</param>
        private static void isDestroyable_Postfix(Bush __instance, ref bool __result)
        {
            try
            {
                if (ModEntry.Config.DestroyableBushLocations?.Count > 0) //if the location list has any entries
                {
                    foreach (string locationName in ModEntry.Config.DestroyableBushLocations) //for each name in the list
                    {
                        if (locationName.Equals(__instance.Location?.Name ?? "", StringComparison.OrdinalIgnoreCase)) //if the listed name matches the bush's location name
                        {
                            __result = true; //return true
                            return;
                        }
                    }
                }
                else //if the location list has no entries
                {
                    switch (__instance.size.Value)
                    {
                        case Bush.smallBush:
                            if (ModEntry.Config.DestroyableBushTypes.SmallBushes) //if allowed to destroy this bush size
                                __result = true; //return true
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

                //return the original result without modifying it
                return;
            }
            catch (Exception ex)
            {
                ModEntry.Instance.Monitor.LogOnce($"Harmony patch \"{nameof(isDestroyable_Postfix)}\" has encountered an error:\n{ex.ToString()}", LogLevel.Error);
            }
        }
    }
}
