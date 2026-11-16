using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BeastAnimationRepair
{
    const string Folder = "Assets/EnemyAI/";
    static double next;
    static BeastAnimationRepair() { EditorApplication.update += Tick; }
    static void Tick()
    {
        if (EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 2;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(Folder + "animation-repair-report.txt")) return;
        try { Repair(); }
        catch (Exception e) { File.WriteAllText(Folder + "animation-repair-report.txt", "ERROR: " + e); Debug.LogException(e); }
    }
    [MenuItem("El Retirado/Corregir animaciones en el sitio")]
    public static void Repair()
    {
        if (EditorApplication.isPlaying) return;
        var lines = new List<string>();
        foreach (string name in new[] { "Reposo", "Caminar", "Correr" })
        {
            string clipPath = Folder + name + ".anim";
            string backupPath = Folder + "Backup/" + name + "_antes_in_place.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (!clip) throw new Exception("Falta " + name);
            if (!File.Exists(backupPath)) AssetDatabase.CopyAsset(clipPath, backupPath);
            // Always derive from the original copy, so this operation is idempotent.
            var source = AssetDatabase.LoadAssetAtPath<AnimationClip>(backupPath);
            EditorUtility.CopySerialized(source, clip); clip.name = name;
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type != typeof(Transform) || (binding.path != "mixamorig:Hips" && binding.path != "")) continue;
                if (!binding.propertyName.StartsWith("m_LocalPosition.")) continue;
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null || curve.length == 0) continue;
                float start = curve.Evaluate(0), end = curve.Evaluate(clip.length);
                float drift = end - start;
                bool horizontal = binding.propertyName.EndsWith(".x") || binding.propertyName.EndsWith(".z");
                var keys = curve.keys;
                for (int i = 0; i < keys.Length; i++)
                {
                    // Remove travel along the ground, retaining cyclic hip sway and natural vertical bob.
                    keys[i].value -= drift * (keys[i].time / clip.length) + (horizontal ? start : 0);
                    keys[i].inTangent -= drift / clip.length;
                    keys[i].outTangent -= drift / clip.length;
                }
                curve.keys = keys;
                AnimationUtility.SetEditorCurve(clip, binding, curve);
                lines.Add(name + " " + binding.path + " " + binding.propertyName + ": original drift=" + drift.ToString("F6") + ", corrected drift=" + (curve.Evaluate(clip.length) - curve.Evaluate(0)).ToString("F6"));
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }
        AssetDatabase.SaveAssets();
        File.WriteAllLines(Folder + "animation-repair-report.txt", lines);
        Debug.Log("[Bestia] Animaciones convertidas a movimiento en el sitio.\n" + string.Join("\n", lines));
    }
}
