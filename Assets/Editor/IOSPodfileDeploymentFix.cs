#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

// AdMob podlarının minimum iOS sürümünü 15.0'a çeker (Xcode 26+ 12.0'ı kabul etmiyor).
// External Dependency Manager Podfile'ı 40. sırada oluşturur, pod install'u 50. sırada çalıştırır;
// bu script arada (45) Podfile'a post_install bloğunu ekler.
public static class IOSPodfileDeploymentFix
{
    [PostProcessBuild(45)]
    public static void OnPostProcessBuild(BuildTarget target, string buildPath)
    {
        if (target != BuildTarget.iOS) return;
        // Şifreleme beyanı: her yüklemede export compliance sorusu sorulmasın
        string plistPath = Path.Combine(buildPath, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
        plist.WriteToFile(plistPath);

        string podfile = Path.Combine(buildPath, "Podfile");
        if (!File.Exists(podfile)) return;
        string content = File.ReadAllText(podfile);
        if (content.Contains("post_install")) return;
        File.AppendAllText(podfile,
            "\npost_install do |installer|\n" +
            "  installer.pods_project.targets.each do |target|\n" +
            "    target.build_configurations.each do |config|\n" +
            "      config.build_settings['IPHONEOS_DEPLOYMENT_TARGET'] = '15.0'\n" +
            "    end\n" +
            "  end\n" +
            "end\n");
    }
}
#endif
