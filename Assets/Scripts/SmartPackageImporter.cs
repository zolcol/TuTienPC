using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class SmartPackageImporter : EditorWindow
{
    // Chỉ áp dụng khử trùng lặp cho Shader và C# script
    private static readonly HashSet<string> TargetDeduplicateExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".cs",
        ".shader",
        ".shadergraph",
        ".shadersubgraph",
        ".compute",
        ".cginc",
        ".hlsl"
    };

    private class PackageItem
    {
        public string TempDir;
        public string PkgGuid;
        public string RelativePath;
        public string GroupKey;
        public bool IsExactBase;
        public int SuffixIndex;
    }

    private class DuplicateMapping
    {
        public string SourcePath;
        public string TargetPath;
        public string SourceGuid;
        public string TargetGuid;
        public string TempDir;
        public bool IsInternal;
    }

    private string packagePath = "";
    private Vector2 scrollPosPreview;
    private Vector2 scrollPosLogs;

    private List<DuplicateMapping> duplicateList = new List<DuplicateMapping>();
    private List<string> newAssetsList = new List<string>();
    private List<string> logs = new List<string>();

    private bool hasScanned = false;
    private string activeTempDir = "";

    [MenuItem("Tools/Smart Package Importer")]
    public static void ShowWindow()
    {
        GetWindow<SmartPackageImporter>("Smart Importer");
    }

    private void OnDisable()
    {
        CleanupTempDirectory();
    }

    private void OnGUI()
    {
        GUILayout.Label("Cấu hình Import Package (Khử trùng Shader & C#)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        packagePath = EditorGUILayout.TextField("File Package:", packagePath);
        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            string selected = EditorUtility.OpenFilePanel("Chọn .unitypackage", "", "unitypackage");
            if (!string.IsNullOrEmpty(selected))
            {
                packagePath = selected;
                ResetScanState();
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        if (!hasScanned)
        {
            if (GUILayout.Button("1. Quét & Phân tích Trùng lặp", GUILayout.Height(30)))
            {
                if (string.IsNullOrEmpty(packagePath) || !File.Exists(packagePath))
                {
                    EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn file .unitypackage hợp lệ!", "OK");
                    return;
                }
                AnalyzePackage(packagePath);
            }
        }
        else
        {
            GUILayout.Space(5);
            int internalCount = duplicateList.Count(d => d.IsInternal);
            int projectCount = duplicateList.Count(d => !d.IsInternal);

            EditorGUILayout.HelpBox(
                $"Phân tích hoàn tất (Chỉ áp dụng cho Shader & C#):\n" +
                $"- Trùng với Project: {projectCount}\n" +
                $"- Trùng nội bộ Package: {internalCount}\n" +
                $"- Tài nguyên sẽ nạp: {newAssetsList.Count}",
                MessageType.Info);

            GUILayout.Label("Danh sách Shader / C# sẽ được gộp sang file Master:", EditorStyles.boldLabel);
            scrollPosPreview = EditorGUILayout.BeginScrollView(scrollPosPreview, GUILayout.Height(200));

            if (duplicateList.Count == 0)
            {
                EditorGUILayout.LabelField("Không có Shader hoặc Script C# nào bị trùng lặp.");
            }
            else
            {
                foreach (var dup in duplicateList)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    string tag = dup.IsInternal ? "[NỘI BỘ PACKAGE]" : "[TRÙNG PROJECT]";
                    EditorGUILayout.LabelField($"{tag} {dup.SourcePath}", EditorStyles.miniBoldLabel);
                    EditorGUILayout.LabelField($"-> Dùng file chuẩn: {dup.TargetPath}", EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField($"Map GUID: {dup.SourceGuid} => {dup.TargetGuid}", EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button("2. Xác nhận Import & Thay thế GUID", GUILayout.Height(35)))
            {
                ExecuteImport();
            }

            GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
            if (GUILayout.Button("Hủy bỏ", GUILayout.Height(35), GUILayout.Width(100)))
            {
                ResetScanState();
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        GUILayout.Label("Nhật ký xử lý:", EditorStyles.boldLabel);
        scrollPosLogs = EditorGUILayout.BeginScrollView(scrollPosLogs, GUILayout.Height(120));
        foreach (var log in logs)
        {
            EditorGUILayout.LabelField(log);
        }
        EditorGUILayout.EndScrollView();
    }

    private void AnalyzePackage(string pkgPath)
    {
        ResetScanState();
        activeTempDir = Path.Combine(Application.dataPath, "../Temp/SmartImportUnpack");

        try
        {
            logs.Add("Bắt đầu giải nén gói package...");
            ExtractTarGz(pkgPath, activeTempDir);

            logs.Add("Đang lọc và phân tích trùng lặp Shader / C#...");
            var subDirs = Directory.GetDirectories(activeTempDir);
            var targetItems = new List<PackageItem>();

            // 1. Phân loại tài nguyên
            foreach (var dir in subDirs)
            {
                string pkgGuid = Path.GetFileName(dir);
                string pathnameFile = Path.Combine(dir, "pathname");
                if (!File.Exists(pathnameFile)) continue;

                string relativePath = File.ReadAllLines(pathnameFile)[0].Trim().Replace('\\', '/');
                string assetFile = Path.Combine(dir, "asset");
                bool isDirectory = !File.Exists(assetFile);

                string ext = Path.GetExtension(relativePath);

                // Bỏ qua thư mục và các file không phải Shader/C#
                if (isDirectory || !TargetDeduplicateExtensions.Contains(ext))
                {
                    newAssetsList.Add(relativePath);
                    continue;
                }

                // Với Shader và C#: Bóc tách tên và đưa vào danh sách đối soát trùng
                string dirPath = Path.GetDirectoryName(relativePath)?.Replace('\\', '/') ?? "";
                string fileName = Path.GetFileNameWithoutExtension(relativePath);

                Match match = Regex.Match(fileName, @"^(.*)_(\d+)$");
                string baseName = match.Success ? match.Groups[1].Value : fileName;
                int suffixIndex = match.Success ? int.Parse(match.Groups[2].Value) : -1;

                string groupKey = Path.Combine(dirPath, baseName + ext).Replace('\\', '/').ToLowerInvariant();

                targetItems.Add(new PackageItem
                {
                    TempDir = dir,
                    PkgGuid = pkgGuid,
                    RelativePath = relativePath,
                    GroupKey = groupKey,
                    IsExactBase = !match.Success,
                    SuffixIndex = suffixIndex
                });
            }

            // 2. Gom nhóm xử lý trùng lặp riêng cho Shader và C#
            var groups = targetItems.GroupBy(x => x.GroupKey);

            foreach (var group in groups)
            {
                string projectMatch = FindExistingAssetMatch(group.First().RelativePath);

                if (!string.IsNullOrEmpty(projectMatch))
                {
                    // A. Đã tồn tại trong Project -> Dùng file trong Project làm chuẩn
                    string projectGuid = AssetDatabase.AssetPathToGUID(projectMatch);
                    foreach (var item in group)
                    {
                        duplicateList.Add(new DuplicateMapping
                        {
                            SourcePath = item.RelativePath,
                            TargetPath = projectMatch,
                            SourceGuid = item.PkgGuid,
                            TargetGuid = projectGuid,
                            TempDir = item.TempDir,
                            IsInternal = false
                        });
                    }
                }
                else
                {
                    // B. Chưa có trong Project -> Khử trùng nội bộ package
                    var sortedList = group.OrderBy(x => x.IsExactBase ? 0 : 1)
                                          .ThenBy(x => x.SuffixIndex)
                                          .ThenBy(x => x.RelativePath)
                                          .ToList();

                    // File đầu tiên làm Master
                    var masterItem = sortedList[0];
                    newAssetsList.Add(masterItem.RelativePath);

                    // Các file còn lại là bản sao bị loại
                    for (int i = 1; i < sortedList.Count; i++)
                    {
                        var dupItem = sortedList[i];
                        duplicateList.Add(new DuplicateMapping
                        {
                            SourcePath = dupItem.RelativePath,
                            TargetPath = masterItem.RelativePath,
                            SourceGuid = dupItem.PkgGuid,
                            TargetGuid = masterItem.PkgGuid,
                            TempDir = dupItem.TempDir,
                            IsInternal = true
                        });
                    }
                }
            }

            hasScanned = true;
            logs.Add($"Quét xong. Phát hiện {duplicateList.Count} Shader/Script trùng | Giữ lại {newAssetsList.Count} tài nguyên.");
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            logs.Add("Lỗi quét: " + ex.Message);
            CleanupTempDirectory();
        }
    }

    private void ExecuteImport()
    {
        try
        {
            // 1. Tạo bảng tra GUID và xóa file trùng khỏi thư mục tạm
            var guidMap = new Dictionary<string, string>();
            foreach (var item in duplicateList)
            {
                guidMap[item.SourceGuid] = item.TargetGuid;
                if (Directory.Exists(item.TempDir))
                {
                    Directory.Delete(item.TempDir, true);
                }
            }

            // 2. Gán lại GUID trong toàn bộ file text của package (bao gồm cả Material, Prefab để trỏ về đúng Shader chuẩn)
            logs.Add("Đang cập nhật lại liên kết GUID cho Material, Prefab, Scene...");
            var remainingDirs = Directory.GetDirectories(activeTempDir);
            string[] textExtensions = { ".mat", ".prefab", ".unity", ".asset", ".anim", ".controller", ".meta", ".cs", ".shader" };

            foreach (var dir in remainingDirs)
            {
                string assetFile = Path.Combine(dir, "asset");
                string metaFile = Path.Combine(dir, "asset.meta");

                ReplaceGuidsInFile(assetFile, guidMap, textExtensions);
                ReplaceGuidsInFile(metaFile, guidMap, textExtensions);
            }

            // 3. Sao chép các file còn lại vào thư mục Assets
            logs.Add("Đang copy tài nguyên vào Project...");
            foreach (var dir in remainingDirs)
            {
                string pathnameFile = Path.Combine(dir, "pathname");
                if (!File.Exists(pathnameFile)) continue;

                string relativeDestPath = File.ReadAllLines(pathnameFile)[0].Trim().Replace('\\', '/');
                string fullDestPath = Path.GetFullPath(relativeDestPath);
                string destDir = Path.GetDirectoryName(fullDestPath);

                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                string assetFile = Path.Combine(dir, "asset");
                string metaFile = Path.Combine(dir, "asset.meta");

                if (File.Exists(assetFile))
                {
                    File.Copy(assetFile, fullDestPath, true);
                }
                else if (!File.Exists(fullDestPath))
                {
                    Directory.CreateDirectory(fullDestPath);
                }

                if (File.Exists(metaFile))
                {
                    File.Copy(metaFile, fullDestPath + ".meta", true);
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Thành công", $"Import hoàn tất!\nĐã gộp và map lại {duplicateList.Count} Shader/C# trùng lặp.", "OK");
            logs.Add("Đã nạp xong và làm mới AssetDatabase.");
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            logs.Add("Lỗi import: " + ex.Message);
        }
        finally
        {
            ResetScanState();
        }
    }

    private static string FindExistingAssetMatch(string relativePath)
    {
        if (File.Exists(relativePath) || Directory.Exists(relativePath))
            return relativePath;

        string dir = Path.GetDirectoryName(relativePath);
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            return null;

        string fileName = Path.GetFileNameWithoutExtension(relativePath);
        string ext = Path.GetExtension(relativePath);

        string baseName = Regex.Replace(fileName, @"_\d+$", "");

        // Ưu tiên 1: File gốc abc.ext
        string exactBaseFile = Path.Combine(dir, baseName + ext).Replace('\\', '/');
        if (File.Exists(exactBaseFile))
            return exactBaseFile;

        // Ưu tiên 2: Biến thể số nhỏ nhất abc_0.ext, abc_1.ext
        string pattern = $@"^{Regex.Escape(baseName)}_\d+$";
        string[] candidateFiles = Directory.GetFiles(dir, "*" + ext);
        Array.Sort(candidateFiles);

        foreach (var file in candidateFiles)
        {
            string nameOnly = Path.GetFileNameWithoutExtension(file);
            if (Regex.IsMatch(nameOnly, pattern, RegexOptions.IgnoreCase))
            {
                return file.Replace('\\', '/');
            }
        }

        return null;
    }

    private static void ReplaceGuidsInFile(string filePath, Dictionary<string, string> map, string[] validExtensions)
    {
        if (!File.Exists(filePath) || map.Count == 0) return;

        string content = File.ReadAllText(filePath);
        bool modified = false;

        foreach (var kvp in map)
        {
            if (content.Contains(kvp.Key))
            {
                content = content.Replace(kvp.Key, kvp.Value);
                modified = true;
            }
        }

        if (modified)
        {
            File.WriteAllText(filePath, content);
        }
    }

    private static void ExtractTarGz(string gzArchiveName, string destFolder)
    {
        if (Directory.Exists(destFolder)) Directory.Delete(destFolder, true);
        Directory.CreateDirectory(destFolder);

        using (FileStream inStream = File.OpenRead(gzArchiveName))
        using (GZipStream gzipStream = new GZipStream(inStream, CompressionMode.Decompress))
        {
            byte[] header = new byte[512];
            while (true)
            {
                int read = gzipStream.Read(header, 0, 512);
                if (read < 512) break;

                bool emptyBlock = true;
                for (int i = 0; i < 512; i++)
                {
                    if (header[i] != 0) { emptyBlock = false; break; }
                }
                if (emptyBlock) break;

                string fileName = Encoding.ASCII.GetString(header, 0, 100).Trim('\0', ' ');
                if (string.IsNullOrEmpty(fileName)) break;

                string sizeOctal = Encoding.ASCII.GetString(header, 124, 12).Trim('\0', ' ');
                long size = Convert.ToInt64(sizeOctal, 8);

                string outputFilePath = Path.Combine(destFolder, fileName);
                string outputDir = Path.GetDirectoryName(outputFilePath);
                if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

                if (!fileName.EndsWith("/"))
                {
                    using (FileStream outStream = File.Create(outputFilePath))
                    {
                        byte[] buffer = new byte[4096];
                        long remaining = size;
                        while (remaining > 0)
                        {
                            int bytesToRead = (int)Math.Min(remaining, buffer.Length);
                            int bytesRead = gzipStream.Read(buffer, 0, bytesToRead);
                            if (bytesRead <= 0) break;
                            outStream.Write(buffer, 0, bytesRead);
                            remaining -= bytesRead;
                        }
                    }

                    long padding = (512 - (size % 512)) % 512;
                    if (padding > 0)
                    {
                        byte[] pad = new byte[padding];
                        gzipStream.Read(pad, 0, (int)padding);
                    }
                }
            }
        }
    }

    private void ResetScanState()
    {
        hasScanned = false;
        duplicateList.Clear();
        newAssetsList.Clear();
        CleanupTempDirectory();
    }

    private void CleanupTempDirectory()
    {
        if (!string.IsNullOrEmpty(activeTempDir) && Directory.Exists(activeTempDir))
        {
            try { Directory.Delete(activeTempDir, true); } catch { }
            activeTempDir = "";
        }
    }
}