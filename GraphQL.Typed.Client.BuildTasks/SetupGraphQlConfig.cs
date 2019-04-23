using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace GraphQL.Typed.Client.BuildTasks
{
	public class SetupGraphQlConfig: Microsoft.Build.Utilities.Task
	{
		public string SolutionPath { get; set; }
		public string SchemaPath { get; set; }
		public string SrcPath { get; set; }

		private void LoadDocument(XmlDocument doc, string filePath)
		{
			var buffer = File.ReadAllBytes(filePath);
			using (var reader = JsonReaderWriterFactory.CreateJsonReader(buffer, new XmlDictionaryReaderQuotas()))
			{
				doc.Load(reader);
			}
		}

		public override bool Execute()
		{
			var directoryPath = Path.GetDirectoryName(this.SolutionPath);

			var srcDoc = new XmlDocument();
			this.LoadDocument(srcDoc, this.SrcPath);
			var srcNode = srcDoc.SelectNodes("//schemaPath")?[0];

			if (srcNode != null)
			{
				srcNode.InnerText = srcNode.InnerText.Replace("schema.json", this.SchemaPath);
			}

			var configPath = Path.Combine(directoryPath, ".graphqlconfig");

			using (var textResult = new MemoryStream())
			{
				using (var writer = JsonReaderWriterFactory.CreateJsonWriter(textResult))
				{
					if (File.Exists(configPath))
					{
						var dstDoc = new XmlDocument();
						this.LoadDocument(dstDoc, configPath);

						var dstNode = dstDoc.SelectNodes("//schemaPath")?[0];

						srcNode = dstDoc.ImportNode(srcNode, true);

						if (dstNode != null)
						{
							dstNode.ParentNode?.ReplaceChild(srcNode, dstNode);
						}
						else
						{
							var schemaNode = dstDoc.SelectNodes("//schemaPath")?[0];

							if (schemaNode == null)
							{
								schemaNode = dstDoc.CreateNode(XmlNodeType.Text, "schemaPath", "");
								dstDoc.AppendChild(schemaNode);
							}

							schemaNode.AppendChild(srcNode);
						}

						dstDoc.Save(writer);
					}
					else
					{
						srcDoc.Save(writer);
					}

					textResult.Seek(0, SeekOrigin.Begin);
					string result;
					using (var reader = new StreamReader(textResult))
					{
						result = reader.ReadToEnd();
					}

					var buffer = Encoding.ASCII.GetBytes(result);
					using (var fs = new FileStream(
						configPath,
						File.Exists(configPath) ? FileMode.Truncate : FileMode.Create,
						FileAccess.Write
					))
					{
						fs.Write(buffer, 0, buffer.Length);
					}
				}
			}

			return true;
		}
	}
}
