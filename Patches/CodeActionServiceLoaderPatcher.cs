using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions;

namespace BusinessCentral.LinterCop.Patches
{
    /// <summary>
    /// This class is responsible for patching the <see cref="CodeActionServiceLoader"/> class inside the Microsoft.Dynamics.Nav.CodeAnalysis.Workspace.dll
    /// It will allow us to add our assembly to the loading of CodeActions, which is done with the Managed Extensibility Framework (MEF).
    /// </summary>
    public static class CodeActionServiceLoaderPatcher
    {
        private static MethodInfo _originalLoadDefaultAssembliesMethod;
        private static MethodInfo _addOwnAssemblyToBuilderMethodInfo;

        public static void TryPatch()
        {
#if DEBUG
            System.Diagnostics.Debugger.Launch();
#endif
            try
            {
                Patch();
            }
            catch (FileNotFoundException e)
            {
                if (e.FileName.IndexOf("0Harmony", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // ignore it for now
                    // diagnostics will work, codefixer will not
                }
                else
                {
                    throw;
                }
            }
        }

        public static void Patch()
        {
            var harmony = new Harmony("LinterCop.BusinessCentral");

            var assembly = typeof(CodeAction).Assembly;
            // The CodeActionServiceLoader type is marked as internal, so we have to get it via its full qualified name.
            var codeActionServiceLoaderType =
                assembly.GetType("Microsoft.Dynamics.Nav.CodeAnalysis.CodeActions.CodeActionServiceLoader");
            var originalConstructor = AccessTools.Constructor(codeActionServiceLoaderType);
            _originalLoadDefaultAssembliesMethod =
                AccessTools.Method(codeActionServiceLoaderType, "LoadDefaultAssemblies");
            var constructorTranspiler =
                AccessTools.Method(typeof(CodeActionServiceLoaderPatcher), nameof(ConstructorTranspiler));
            _addOwnAssemblyToBuilderMethodInfo = AccessTools.Method(typeof(CodeActionServiceLoaderPatcher),
                nameof(AddOwnAssemblyToBuilder));

            harmony.Patch(originalConstructor, null, null, new HarmonyMethod(constructorTranspiler));
        }

#if DEBUG
        [HarmonyDebug]
#endif
        private static IEnumerable<CodeInstruction> ConstructorTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.Calls(_originalLoadDefaultAssembliesMethod))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Call, _addOwnAssemblyToBuilderMethodInfo);
                }
            }
        }

        public static void AddOwnAssemblyToBuilder(HashSet<Assembly> builder)
        {
            if (builder == null)
                return;

            var assembly = Assembly.GetExecutingAssembly();
            if (!builder.Contains(assembly))
                builder.Add(assembly);
        }
    }
}