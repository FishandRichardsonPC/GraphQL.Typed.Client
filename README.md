# GraphQL.Typed.Client
[![Build Status](https://dev.azure.com/fishandrichardson-oss/GraphQL.Typed.Client/_apis/build/status/FishandRichardsonPC.GraphQL.Typed.Client?branchName=master)](https://dev.azure.com/fishandrichardson-oss/GraphQL.Typed.Client/_build/latest?definitionId=1&branchName=master)
[![SemVer](https://img.shields.io/nuget/v/GraphQL.Typed.Client.svg)](https://semver.org)
[![Nuget](https://img.shields.io/nuget/dt/GraphQL.Typed.Client.svg)](https://www.nuget.org/packages/GraphQL.Typed.Client)


C# Library and ms build tasks for managing .graphql files within a c# project. This project will create c# files using
[quicktype](https://quicktype.io/) for any .graphql files in your project which will use the
[GraphQL.Client](https://github.com/graphql-dotnet/graphql-client) library to request the results

# Usage
Note: This package only works .net versions which support .net standard. You will also need to use dependency injection.

## Consuming this package with an existing schema
1. Acquire the schema.json file for the graphql server you want to request data from
2. Add that file to your project
3. Add the GraphQL.Typed.Client nuget package
4. In your csproj file add a `<GraphQLSchema>` tag inside the first property group, inside that tag add the path to your schema file
5. Optional: If using JetBrains Rider add `<GraphQLSetupRiderFileWatcher>True</GraphQLSetupRiderFileWatcher>` inside the first property group to auto generate a file watcher
6. Optional: If using a plugin which supports .graphqlconfig files add `<GraphQLSetupConfig>True</GraphQLSetupConfig>` inside the first property group to auto generate one
7. Create a class which implements `IGraphQlClientBuilder`. This class should configure the builder to correctly hit the server that your schema belongs to
8. Add that class using the `IGraphQlClientBuilder` interface to your injected services
9. Create a .graphql file in your project with a graph query or mutation in it.
    * This file should be named {DescriptiveName}Query.graphql or {DescriptiveName}Mutation.graphql
    * The query or mutation should be named the same as the filename
10. Hit build on your project (or hit save if you are in Rider and added the element on step 6)
11. You will now have a cs file containing a class/interface matching the graphql file name, it will need to be injected into your services
12. Add the new class or interface to the consuming class and call the `Fetch` method

## Producing a package with your own schema
This package can also be used to produce a nuget package which will generate types for an included schema.
1. Create a new .net standard library project
2. Add the GraphQL.Typed.Client nuget package
3. Edit the `PackageReference` tag adding `PrivateAssets="none"`
4. Add a build folder
5. Add a {Package.Name}.targets file. This file MUST have the same file name as the resulting package name
6. Add the following to the targets file
    ```
    <Project>
        <PropertyGroup>
            <GraphQLSchema>$(MSBuildThisFileDirectory)\..\schema.json</GraphQLSchema>
        </PropertyGroup>

        <PropertyGroup>
            <RelativeSchemaPath>$([MSBuild]::MakeRelative('$(MSBuildProjectDirectory)', '$(GraphQLSchema)'))</RelativeSchemaPath>
        </PropertyGroup>

        <ItemGroup>
            <None Include="$(RelativeSchemaPath)" Visible="false"/>
        </ItemGroup>
    </Project>
    ```
7. Add your schma.json file to your project
8. Add the following to your csproj file, replacing `{Package.Name}` with the name of your package
    ```
    <ItemGroup>
        <None Include="build\{Package.Name}.targets">
            <Pack>true</Pack>
            <PackagePath>build</PackagePath>
        </None>
    </ItemGroup>
    <ItemGroup>
        <None Include="schema.json">
            <Pack>true</Pack>
            <PackagePath>.</PackagePath>
        </None>
    </ItemGroup>
    ```
    Note: If you see either file appear twice in the tree remove any other elements which refer to those files
9. Add the Nuget attributes to your csproj and hit build. You should now have an installable package which will configure projects to build cs files off graphql files

# Releasing
All Pull Requests should be available as a prerelease on nuget.org. To create an official release create a release in github
with the new version number, after the build completes it will be uploaded to nuget.org