#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

/// <summary>
/// ✅ Unity .csproj 自动补丁系统（基于 OnGeneratedCSProject 接口）
/// 适配 Unity 2021.2+
/// 支持链式配置 + 多会话
/// </summary>
public class CsprojPatcher : AssetPostprocessor
{
	// 保存所有注册的 PatchSession
	private static readonly List<PatchSession> sessions = new();

	/// <summary>
	/// 开启一个新的补丁配置会话
	/// </summary>
	public static PatchSession Begin()
	{
		var s = new PatchSession();
		sessions.Add(s);
		return s;
	}

	/// <summary>
	/// Unity 官方回调：每生成一个 .csproj 文件时调用。
	/// 我们在这里修改内容并返回。
	/// </summary>
	private static string OnGeneratedCSProject(string path, string content)
	{
		string fileName = Path.GetFileNameWithoutExtension(path);

		foreach (var session in sessions)
		{
			// 如果没有目标限制，或者匹配到目标，则执行补丁
			bool match = session.TargetNames.Count == 0 && session.TargetPaths.Count == 0
						 || session.TargetNames.Contains(fileName)
						 || session.TargetPaths.Contains(Path.GetFullPath(path));

			if (!match)
				continue;

			content = ApplyPatch(session, path, content);
		}

		return content;
	}

	/// <summary>
	/// 实际的补丁逻辑（修改 XML 文本）
	/// </summary>
	private static string ApplyPatch(PatchSession s, string path, string content)
	{
		bool modified = false;
		var builder = new StringBuilder();

		// ===== 添加 <ItemGroup> =====
		builder.AppendLine();
		builder.AppendLine("  <ItemGroup>");

		foreach (var prj in s.ProjectRefs)
		{
			if (!content.Contains(prj))
			{
				builder.AppendLine($@"    <ProjectReference Include=""{prj}"" />");
				modified = true;
			}
		}

		foreach (var folder in s.SourceFolders)
		{
			if (!content.Contains(folder))
			{
				builder.AppendLine($@"    <Compile Include=""{folder}\**\*.cs"" Link=""{Path.GetFileName(folder)}\%(RecursiveDir)%(Filename)%(Extension)"" />");
				modified = true;
			}
		}

		foreach (var analyzer in s.Analyzers)
		{
			if (!content.Contains(analyzer))
			{
				builder.AppendLine($@"    <Analyzer Include=""{analyzer}"" />");
				modified = true;
			}
		}

		builder.AppendLine("  </ItemGroup>");
		builder.AppendLine("</Project>");

		if (modified)
			content = content.Replace("</Project>", builder.ToString());

		// ===== 添加 DefineConstants =====
		if (s.Defines.Count > 0)
		{
			var pattern = new Regex(@"<DefineConstants>(.*?)</DefineConstants>", RegexOptions.Singleline);
			if (pattern.IsMatch(content))
			{
				content = pattern.Replace(content, m => $"<DefineConstants>{m.Groups[1].Value};{string.Join(";", s.Defines)}</DefineConstants>");
			}
			else
			{
				content = content.Replace("</PropertyGroup>", $"  <DefineConstants>{string.Join(";", s.Defines)}</DefineConstants>\n  </PropertyGroup>");
			}
		}

		// ===== 添加 LangVersion =====
		if (!string.IsNullOrEmpty(s.LangVersion) && !content.Contains("<LangVersion>"))
		{
			content = content.Replace("</PropertyGroup>", $"  <LangVersion>{s.LangVersion}</LangVersion>\n  </PropertyGroup>");
		}

		// ===== 添加 NoWarn =====
		if (s.NoWarns.Count > 0 && !content.Contains("<NoWarn>"))
		{
			content = content.Replace("</PropertyGroup>", $"  <NoWarn>{string.Join(";", s.NoWarns)}</NoWarn>\n  </PropertyGroup>");
		}

		return content;
	}

	// ================== Builder 类 ==================
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

		public PatchSession AddProjectReference(string path)
		{
			if (!ProjectRefs.Contains(path)) ProjectRefs.Add(path);
			return this;
		}

		public PatchSession AddSourceFolder(string path)
		{
			if (!SourceFolders.Contains(path)) SourceFolders.Add(path);
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

		public PatchSession AddLangVersion(string version)
		{
			LangVersion = version;
			return this;
		}

		public PatchSession AddNoWarn(params string[] codes)
		{
			foreach (var c in codes)
				if (!NoWarns.Contains(c))
					NoWarns.Add(c);
			return this;
		}

		public void Apply()
		{ /* 实际执行由 Unity 调用 OnGeneratedCSProject 完成 */ }
	}
}

#endif