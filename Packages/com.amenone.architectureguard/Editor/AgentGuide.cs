using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Amenone.ArchitectureGuard.Editor
{
    public static class AgentGuide
    {
        private const string CopyMenuPath = "Tools/Amenone/ArchitectureGuard/Copy Agent Guide To Project";
        private const string FileName = "AGENT_GUIDE.md";
        private const string ProjectCopyDisplayPath = "docs/architectureguard/" + FileName;
        private const string WarnedSessionKey = "Amenone.ArchitectureGuard.AgentGuide.Warned";
        private const string HeaderPrefix = "<!-- このファイルは com.amenone.architectureguard ";
        private const char ByteOrderMark = '﻿';

        private static readonly byte[] Utf8ByteOrderMark = Encoding.UTF8.GetPreamble();

        private static string ProjectCopyPath =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName, "docs", "architectureguard", FileName);

        [MenuItem(CopyMenuPath)]
        public static void CopyToProject()
        {
            var package = FindPackage() ?? throw new InvalidOperationException("com.amenone.architectureguard のパッケージが見つかりません。");
            var copy = WithHeader(WithoutByteOrderMark(File.ReadAllBytes(GuidePathOf(package))), package.version);
            if (File.Exists(ProjectCopyPath) && File.ReadAllBytes(ProjectCopyPath).SequenceEqual(copy))
            {
                Debug.Log($"{ProjectCopyPath} は com.amenone.architectureguard {package.version} の {FileName} と同じなので、書き換えませんでした。");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(ProjectCopyPath));
            File.WriteAllBytes(ProjectCopyPath, copy);
            Debug.Log($"com.amenone.architectureguard {package.version} の {FileName} を {ProjectCopyPath} へ写しました。");
        }

        [InitializeOnLoadMethod]
        private static void WarnOnceIfProjectCopyIsStale()
        {
            if (SessionState.GetBool(WarnedSessionKey, false))
            {
                return;
            }

            try
            {
                if (!IsProjectCopyStale())
                {
                    return;
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return;
            }

            SessionState.SetBool(WarnedSessionKey, true);
            Debug.LogWarning($"{ProjectCopyDisplayPath} が、com.amenone.architectureguard の {FileName} と違います。メニューの {CopyMenuPath} で写し直してください。プロジェクト固有の事は AGENTS.md に書いてください。");
        }

        private static bool IsProjectCopyStale()
        {
            var package = FindPackage();
            if (package is null || !File.Exists(ProjectCopyPath))
            {
                return false;
            }

            var copiedGuide = WithoutHeader(ComparableText(File.ReadAllBytes(ProjectCopyPath)));
            return copiedGuide != ComparableText(File.ReadAllBytes(GuidePathOf(package)));
        }

        private static PackageInfo FindPackage() => PackageInfo.FindForAssembly(typeof(AgentGuide).Assembly);

        private static string GuidePathOf(PackageInfo package) => Path.Combine(package.resolvedPath, FileName);

        private static byte[] WithHeader(byte[] guide, string version)
        {
            var newLine = Encoding.UTF8.GetString(guide).Contains("\r\n") ? "\r\n" : "\n";
            var header = $"{HeaderPrefix}{version} から写した物で、直接書き換えない。プロジェクト固有の事は AGENTS.md に書く -->{newLine}";
            return Encoding.UTF8.GetBytes(header).Concat(guide).ToArray();
        }

        private static byte[] WithoutByteOrderMark(byte[] bytes) =>
            bytes.Take(Utf8ByteOrderMark.Length).SequenceEqual(Utf8ByteOrderMark) ? bytes.Skip(Utf8ByteOrderMark.Length).ToArray() : bytes;

        // git の改行の設定や手での写し方で BOM と改行が変わっても、中身が同じなら同じと見なす。
        private static string ComparableText(byte[] bytes) =>
            Encoding.UTF8.GetString(bytes).TrimStart(ByteOrderMark).Replace("\r\n", "\n");

        // 手で写した物には先頭の行が無いので、その時は全体を本文として比べる。
        private static string WithoutHeader(string copy)
        {
            if (!copy.StartsWith(HeaderPrefix, StringComparison.Ordinal))
            {
                return copy;
            }

            var headerEnd = copy.IndexOf('\n');
            return headerEnd < 0 ? string.Empty : copy.Substring(headerEnd + 1);
        }
    }
}
