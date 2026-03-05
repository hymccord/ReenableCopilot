using System.Runtime.InteropServices;

using HarmonyLib;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;

namespace ReenableCopilot;

[Guid("80B8EFDB-F137-42A9-A648-7FD0E37D1156")]
[ScenarioPreloadRegistration("Startup")]
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
public sealed class ReenableCopilotPackage : AsyncPackage
{
    protected override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        var harmony = new Harmony("com.github.user.ReenableCopilot");
        harmony.PatchAll();

        return Task.CompletedTask;
    }
}


[HarmonyPatch(typeof(RegistryKey), nameof(RegistryKey.GetValue), typeof(string))]
public static class Patch
{
    [HarmonyPostfix]
    public static void GetValue_Postfix(string name, ref object __result)
    {
        if (name is "DisableCopilot" or "DisableCopilotForIndividuals" or "DisableAgentMode")
        {
            __result = 0;
        }
    }
}

[VisualStudioContribution]
public class ExtensionEntrypoint : Extension
{
    public override ExtensionConfiguration ExtensionConfiguration => new()
    {
        RequiresInProcessHosting = true,
    };

    protected override void InitializeServices(IServiceCollection serviceCollection)
    {
        base.InitializeServices(serviceCollection);
    }
}
