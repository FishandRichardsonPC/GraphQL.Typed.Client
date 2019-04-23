using System;
using System.IO;
using System.Text;
using System.Xml;

namespace GraphQL.Typed.Client.BuildTasks
{
	public class SetupWatcher: Microsoft.Build.Utilities.Task
	{
		public string SolutionPath { get; set; }
		public string SchemaPath { get; set; }
		public string SrcPath { get; set; }

		public override bool Execute()
		{
			var directoryPath = Path.GetDirectoryName(this.SolutionPath);
			var solutionName = Path.GetFileNameWithoutExtension(this.SolutionPath);

			var srcDoc = new XmlDocument();
			var text = File.ReadAllText(this.SrcPath).Replace("{ConfigFile}", this.SchemaPath);
			srcDoc.LoadXml(text);
			var srcNode = srcDoc.SelectNodes("//option[@name=\"name\"][@value=\"Build GraphQL\"]")?[0]?.ParentNode;
			var argsNode = srcNode?.SelectNodes("//option[@name=\"arguments\"]")?[0];
			var argsAttr = argsNode?.Attributes?.GetNamedItem("Value") ?? argsNode?.Attributes?.GetNamedItem("value");

			var jsPath = Path.GetFullPath(Path.Combine(this.SrcPath, "..", "generateTypes.js"));
			if (argsAttr != null)
			{
				argsAttr.Value = argsAttr.Value.Replace("generateTypes.js", jsPath);
			}

			var watcherTasksPath = Path.Combine(
				directoryPath,
				".idea",
				".idea." + solutionName,
				".idea",
				"watcherTasks.xml"
			);

			var textResult = new StringWriter();

			if (File.Exists(watcherTasksPath))
			{
				var dstDoc = new XmlDocument();
				try
				{
					dstDoc.Load(watcherTasksPath);
				}
				catch (Exception e)
				{
					try
					{
						using (var fs = new FileStream(
							watcherTasksPath,
							FileMode.Open,
							FileAccess.Read
						))
						{
							fs.Seek(0, SeekOrigin.Begin);
							using (var reader = new StreamReader(fs))
							{
								dstDoc.LoadXml(reader.ReadToEnd());
							}
						}
					}
					catch (Exception e2)
					{
						throw new AggregateException(e, e2);
					}
				}

				var dstNameNode = dstDoc.SelectNodes("//option[@name=\"name\"][@value=\"Build GraphQL\"]");

				srcNode = dstDoc.ImportNode(srcNode, true);

				if (dstNameNode?.Count > 0)
				{
					var dstNode = dstNameNode[0].ParentNode;
					dstNode?.ParentNode?.ReplaceChild(srcNode, dstNode);
				}
				else
				{
					var taskNode = dstDoc.SelectNodes("//component[@name=\"ProjectTasksOptions\"]")?[0];

					if (taskNode == null)
					{
						taskNode = dstDoc.CreateNode(XmlNodeType.Element, "component", "");
						dstDoc.AppendChild(taskNode);
					}

					taskNode.AppendChild(srcNode);
				}

				dstDoc.Save(textResult);
			}
			else
			{
				srcDoc.Save(textResult);
			}

			var buffer = Encoding.ASCII.GetBytes(textResult.ToString());
			using (var fs = new FileStream(
				watcherTasksPath,
				File.Exists(watcherTasksPath) ? FileMode.Truncate : FileMode.Create,
				FileAccess.Write
			))
			{
				fs.Write(buffer, 0, buffer.Length);
			}

			return true;
		}
	}
}
