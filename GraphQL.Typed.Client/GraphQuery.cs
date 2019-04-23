using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GraphQL.Common.Exceptions;
using GraphQL.Common.Request;
using Newtonsoft.Json.Linq;

namespace GraphQL.Typed.Client
{
	public class GraphQuery
	{
		private readonly IGraphQlClientBuilder _graphQlClientBuilder;

		public GraphQuery(IGraphQlClientBuilder graphQlClientBuilder)
		{
			this._graphQlClientBuilder = graphQlClientBuilder;
		}

		public async Task<T> Fetch<T>(string queryResourceName, object variables = null)
		{
			if (variables == null)
			{
				variables = new { };
			}
			using (var client = this._graphQlClientBuilder.Build())
			{
				var splitName = queryResourceName.Split('.');
				var result = await client.PostAsync(
					new GraphQLRequest
					{
						Query = this.GetResource<T>(queryResourceName),
						OperationName = splitName.ElementAtOrDefault(splitName.Length - 2) ,
						Variables = variables
					});
				if (result.Errors != null && result.Errors.Any())
				{
					throw new AggregateException(result.Errors.Select((v) => new GraphQLException(v)));
				}

				if (result.Data is T)
				{
					return result.Data;
				}

				if (result.Data is JObject obj)
				{
					return obj.ToObject<T>();
				}

				throw new ArgumentException($"Data type returned by GraphQLRequest could not not be converted to {nameof(T)}");

			}
		}

		private string GetResource<T>(string queryResourceName)
		{
			var assembly = Assembly.GetAssembly(typeof(T));

            using (var stream = assembly.GetManifestResourceStream(queryResourceName))
            {
	            using (var reader = new StreamReader(stream))
	            {
		            return reader.ReadToEnd();
	            }
            }
		}
	}
}
