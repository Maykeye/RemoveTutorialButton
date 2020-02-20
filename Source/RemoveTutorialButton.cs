using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace RemoveTutBut
{
    [StaticConstructorOnStartup]
    [UsedImplicitly] // called by rimworld 
    public static class RemoveTutorialButton
    {
        static RemoveTutorialButton()
        {
            var harmony = new Harmony("RemoveTutBut.HelloWorld");
            harmony.PatchAll(Assembly.GetExecutingAssembly()); 
        }

    }

    [HarmonyPatch(typeof(MainMenuDrawer))]
    [HarmonyPatch(nameof(MainMenuDrawer.DoMainMenuControls))]
    [UsedImplicitly]　//called by Harmony
    class MainMenuDrawer_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Sanity check: version
            if (VersionControl.CurrentMajor != 1 || VersionControl.CurrentMinor != 1)
            {
                Log.Error($"RemoveTutorialButton doesn't support version {VersionControl.CurrentVersion}");
                return instructions;
            }

            var code = new List<CodeInstruction>(instructions);
            int hashSoFar = 17;
            
            // Calculate hash and location to ensure we don't overwrite something we are not supposed to
            // We use this https://stackoverflow.com/questions/1646807/quick-and-simple-hash-code-combinations
            // for consistent hash calculation
            int i;
            for(i = 0; i < code.Count; i++)
            {
                var instruction = code[i];
                
                // Add current instruction to the hash using tuple pair
                hashSoFar = hashSoFar * 31 + (int) instruction.opcode.Value;
                
                // Break at first callvirt (which is supposed to add Tutorial button to list of buttons)
                if (instruction.opcode == OpCodes.Callvirt)
                {
                    break;
                }
            }

            //For current 1.1
            const int expectedCallPosition = 50;
            const int expectedHash = 1947548572; //hash of all instructions prior to call

            // Another sanity check. 
            if (i == expectedCallPosition && hashSoFar == expectedHash)
            {
                // Original call is 5 bytes long. We replace it with
                // Two pops and three NOPs (each instruction is 1 byte long) to 
                // Pop Action
                code[expectedCallPosition] = new CodeInstruction(OpCodes.Pop);
                // Pop List
                code.Insert(expectedCallPosition+1, new CodeInstruction(OpCodes.Pop));
                // Rewrite tail of call with nops
                code.Insert(expectedCallPosition+2, new CodeInstruction(OpCodes.Nop));
                code.Insert(expectedCallPosition+2, new CodeInstruction(OpCodes.Nop));
                code.Insert(expectedCallPosition+2, new CodeInstruction(OpCodes.Nop));
            }
            else
            {
                Log.Error($"RemoveTutBut: Couldn't patch out tutorial button: unexpected code: {i}, {hashSoFar}");
            }

            

            return code;
        }
    }
}
