#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

/// <summary>
/// ✅ Unity .csproj 自动补丁系统（基于 OnGeneratedCSProject 接口）
/// 保持原有结构与调用方式，支持：
/// - SourceFolder
/// - Analyzer
/// - ProjectReference
/// - Define
/// - LangVersion
/// - NoWarn
/// </summary>
public class CsprojPatcher : AssetPostprocessor
{
	// ================== 保持原有结构 ==================
	private static readonly List<PatchSession> sessions = new();

	public static PatchSession Begin()
	{
		var s = new PatchSession();
		sessions.Add(s);
		return s;
	}

	/// <summary>
	/// Unity 在生成每个 .csproj 时调用
	/// </summary>
	private static string OnGeneratedCSProject(string path, string content)
	{
		string fileName = Path.GetFileNameWithoutExtension(path);

		foreach (var session in sessions)
		{
			bool match = session.TargetNames.Count == 0 && session.TargetPaths.Count == 0
				|| session.TargetNames.Contains(fileName)
				|| session.TargetPaths.Contains(Path.GetFullPath(path));

			if (!match)
				continue;

			content = ApplyPatch(session, path, content);
		}

		return content;
	}

	// ================== 主补丁逻辑 ==================
	private static string ApplyPatch(PatchSession s, string path, string content)
	{
		bool modified = false;
		var builder = new StringBuilder();

		// --- 1️⃣ 添加源码目录 ---
		if (s.SourceFolders.Count > 0)
		{
			builder.AppendLine();
			builder.AppendLine("  <ItemGroup>");
			foreach (var folder in s.SourceFolders)
			{
				if (!content.Contains(folder))
				{
					builder.AppendLine($@"    <Compile Include=""{folder}\**\*.cs"" />");
					modified = true;
				}
			}
			builder.AppendLine("  </ItemGroup>");
		}

		// --- 2️⃣ 添加 Analyzer ---
		if (s.Analyzers.Count > 0)
		{
			builder.AppendLine();
			builder.AppendLine("  <ItemGroup>");
			foreach (var analyzer in s.Analyzers)
			{
				if (!content.Contains(analyzer))
				{
					builder.AppendLine($@"    <Analyzer Include=""{analyzer}"" />");
					modified = true;
				}
			}
			builder.AppendLine("  </ItemGroup>");
		}

		// --- 3️⃣ 添加 ProjectReference ---
		if (s.ProjectRefs.Count > 0)
		{
			builder.AppendLine();
			builder.AppendLine("  <ItemGroup>");
			foreach (var reference in s.ProjectRefs)
			{
				string name = Path.GetFileNameWithoutExtension(reference);
				if (!content.Contains(name))
				{
					builder.AppendLine($@"    <ProjectReference Include=""{reference}"">");
					builder.AppendLine($@"      <Name>{name}</Name>");
					builder.AppendLine("    </ProjectReference>");
					modified = true;
				}
			}
			builder.AppendLine("  </ItemGroup>");
		}

		// --- 4️⃣ 添加 Define ---
		if (s.Defines.Count > 0)
		{
			var match = Regex.Match(content, @"<DefineConstants>(.*?)</DefineConstants>", RegexOptions.Singleline);
			if (match.Success)
			{
				string old = match.Groups[1].Value;
				string merged = old;
				foreach (var d in s.Defines)
					if (!merged.Contains(d))
						merged += ";" + d;

				content = content.Replace(match.Value, $"<DefineConstants>{merged}</DefineConstants>");
				modified = true;
			}
		}

		// --- 5️⃣ 设置 LangVersion ---
		if (!string.IsNullOrEmpty(s.LangVersion))
		{
			var match = Regex.Match(content, @"<LangVersion>(.*?)</LangVersion>", RegexOptions.Singleline);
			if (match.Success)
			{
				content = content.Replace(match.Value, $"<LangVersion>{s.LangVersion}</LangVersion>");
			}
			else
			{
				int insertPos = content.IndexOf("<PropertyGroup>", StringComparison.OrdinalIgnoreCase);
				if (insertPos > 0)
				{
					content = content.Insert(insertPos + "<PropertyGroup>".Length,
						$"\n    <LangVersion>{s.LangVersion}</LangVersion>");
				}
			}
			modified = true;
		}

		// --- 6️⃣ 添加 NoWarn ---
		if (s.NoWarns.Count > 0)
		{
			var match = Regex.Match(content, @"<NoWarn>(.*?)</NoWarn>", RegexOptions.Singleline);
			if (match.Success)
			{
				string old = match.Groups[1].Value;
				string merged = old;
				foreach (var n in s.NoWarns)
					if (!merged.Contains(n))
						merged += ";" + n;

				content = content.Replace(match.Value, $"<NoWarn>{merged}</NoWarn>");
				modified = true;
			}
			else
			{
				int insertPos = content.IndexOf("<PropertyGroup>", StringComparison.OrdinalIgnoreCase);
				if (insertPos > 0)
				{
					string newWarn = string.Join(";", s.NoWarns);
					content = content.Insert(insertPos + "<PropertyGroup>".Length,
						$"\n    <NoWarn>{newWarn}</NoWarn>");
				}
				modified = true;
			}
		}

		// --- 7️⃣ 插入新节点 ---
		if (modified)
		{
			int pos = content.LastIndexOf("</ItemGroup>", StringComparison.OrdinalIgnoreCase);
			if (pos >= 0)
				content = content.Insert(pos + "</ItemGroup>".Length, builder.ToString());
		}

		return content;
	}

	// ================== 配置构造器 ==================
	public class PatchSession
	{
		internal readonly List<string> TargetNames = new();
		internal readonly List<string> TargetPaths = new();
		internal readonly List<string> ProjectRefs = new();
		internal readonly List<string> SourceFolders = new();
		internal readonly List<string> Defines = new();
		internal readonly List<string> Analyzers = new();
		internal readonly List<string> NoWarns = new();
		internal string LangVersion;

		// --- 基础目标 ---
		public PatchSession Target(string name)
		{
			if (!TargetNames.Contains(name)) TargetNames.Add(name);
			return this;
		}

		public PatchSession TargetPath(string path)
		{
			var full = Path.GetFullPath(path);
			if (!TargetPaths.Contains(full)) TargetPaths.Add(full);
			return this;
		}

		// --- 添加条目 ---
		public PatchSession AddSourceFolder(string path)
		{
			if (!SourceFolders.Contains(path)) SourceFolders.Add(path);
			return this;
		}

		public PatchSession AddProjectReference(string path)
		{
			if (!ProjectRefs.Contains(path)) ProjectRefs.Add(path);
			return this;
		}

		public PatchSession AddAnalyzer(string path)
		{
			if (!Analyzers.Contains(path)) Analyzers.Add(path);
			return this;
		}

		public PatchSession AddDefine(string define)
		{
			if (!Defines.Contains(define)) Defines.Add(define);
			return this;
		}

		public PatchSession AddNoWarn(params string[] codes)
		{
			foreach (var c in codes)
				if (!NoWarns.Contains(c))
					NoWarns.Add(c);
			return this;
		}

		public PatchSession AddLangVersion(string version)
		{
			LangVersion = version;
			return this;
		}

		// --- 结束并提交 ---
		public void Apply()
		{
			// Unity 的 OnGeneratedCSProject 会自动触发执行，不需要立即改文件
		}
	}
}

#endif