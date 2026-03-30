using System.Reflection;
using System.Runtime.InteropServices;

using HarmonyLib;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;

namespace CopilotPatch;

[Guid("80B8EFDB-F137-42A9-A648-7FD0E37D1156")]
[ScenarioPreloadRegistration("Startup", new Type[] { })]
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
public sealed class GenAIPackage : AsyncPackage
{
    private Harmony _harmony;

    protected override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        _harmony = new Harmony("com.github.user.genaipackage");
        _harmony.PatchAll();

        WatchForCopilotUI();

        return Task.CompletedTask;
    }

    private void WatchForCopilotUI()
    {
        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
    }

    private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
    {
        if (args.LoadedAssembly.GetName().Name == "Microsoft.VisualStudio.Copilot.Core")
        {
            AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
            ApplyCopilotPatches(args.LoadedAssembly);
        }
    }

    private void ApplyCopilotPatches(Assembly assembly)
    {
        var type = assembly.GetType("Microsoft.VisualStudio.Copilot.Core.BringYourOwnKey.OpenAIModelProvider");
        if (type == null)
            return;

        var getter = AccessTools.PropertyGetter(type, "BaseApi");
        if (getter == null)
            return;

        _harmony.Patch(getter, postfix: new HarmonyMethod(typeof(GenAiMilPatch), nameof(GenAiMilPatch.Postfix)));
    }
}


[HarmonyPatch(typeof(RegistryKey), nameof(RegistryKey.GetValue), typeof(string))]
public class Patch
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

public class GenAiMilPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref string __result)
    {
        __result = "https://api.genai.mil/v1";
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
