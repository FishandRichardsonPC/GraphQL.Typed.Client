using GraphQL.Client;

namespace GraphQL.Typed.Client
{
	public interface IGraphQlClientBuilder
	{
		GraphQLClient Build();
	}
}