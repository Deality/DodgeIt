using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Üst menü > DodgeIt:
//  - "Build Test APK (Telefona Kur)": telefona doğrudan kurulan APK, kurup açar.
//  - "Build Google Play AAB": Play Console'a yüklenecek App Bundle. Play her telefona sadece o
//    telefonun mimarisine (ARMv7 / ARM64) ait parçayı indirir, indirilen boyut küçük kalır.
// İkisi de developer console / script debugger / profiler olmadan "ham" release build alır.
public static class DodgeItBuildMenu
{
    const string ApkPath = "Builds/Android/DodgeIt.apk";
    const string AabPath = "Builds/Android/DodgeIt.aab";

    [MenuItem("DodgeIt/Build Test APK (Telefona Kur)")]
    public static void BuildTestApk()
    {
        ApplyReleaseSettings(appBundle: false);
        Build(ApkPath, BuildOptions.AutoRunPlayer);
    }

    // Telefon bağlı değilken ya da komut satırından (Unity kapalıyken):
    // Unity.exe -batchmode -projectPath . -executeMethod DodgeItBuildMenu.BuildTestApkCli
    // Keystore şifreleri girilmemişse (Unity her açılışta unutuyor) test APK'sı Unity'nin debug anahtarıyla imzalanır.
    [MenuItem("DodgeIt/Build Test APK (Sadece Dosya)")]
    public static void BuildTestApkCli()
    {
        ApplyReleaseSettings(appBundle: false);
        bool useDebugKey = PlayerSettings.Android.useCustomKeystore &&
            (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass));
        if (useDebugKey) PlayerSettings.Android.useCustomKeystore = false;
        bool ok;
        try { ok = Build(ApkPath, BuildOptions.None); }
        finally { if (useDebugKey) PlayerSettings.Android.useCustomKeystore = true; }
        if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
    }

    [MenuItem("DodgeIt/Build Google Play AAB")]
    public static void BuildPlayAab()
    {
        if (!PlayerSettings.Android.useCustomKeystore)
        {
            EditorUtility.DisplayDialog("Keystore gerekli",
                "Google Play, AAB'nin senin kendi imza anahtarınla (keystore) imzalanmasını ister.\n\n" +
                "Project Settings > Player > Android > Publishing Settings > Keystore Manager'dan bir keystore oluştur " +
                "(şifreleri ve dosyayı güvenli bir yerde sakla; kaybedersen güncelleme yükleyemezsin), sonra tekrar dene.",
                "Tamam");
            return;
        }
        ApplyReleaseSettings(appBundle: true);
        Build(AabPath, BuildOptions.None);
    }

    static void ApplyReleaseSettings(bool appBundle)
    {
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.connectProfiler = false;
        EditorUserBuildSettings.buildAppBundle = appBundle;
        PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.Android, Il2CppCodeGeneration.OptimizeSize);
    }

    static bool Build(string path, BuildOptions options)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = path,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = options
        });

        var s = report.summary;
        if (s.result == BuildResult.Succeeded)
            Debug.Log($"[DodgeIt Build] {path} hazır: {new FileInfo(path).Length / 1048576f:F1} MB ({s.totalTime:mm\\:ss})");
        else
            Debug.LogError($"[DodgeIt Build] {path} başarısız: {s.result}, {s.totalErrors} hata");
        return s.result == BuildResult.Succeeded;
    }
}
