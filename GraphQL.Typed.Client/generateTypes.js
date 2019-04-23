const fs = require('fs');
const path = require('path');
const {promisifyAll} = require('bluebird');

const pfs = promisifyAll(fs);

const {
    quicktype,
    InputData,
    NewtonsoftCSharpTargetLanguage,
    NewtonsoftCSharpRenderer,
    getOptionValues,
    newtonsoftCSharpOptions
} = require('quicktype-core');
const { GraphQLInput } = require('quicktype-graphql-input');

class TargetLanguage extends NewtonsoftCSharpTargetLanguage {
    constructor() {
        // In the constructor we call the super constructor with fixed display name,
        // names, and extension, so we don't have to do it when instantiating the class.
        // Our class is not meant to be extensible in turn, so that's okay.
        super("C#", ["csharp"], "cs");
    }

    makeRenderer(renderContext, untypedOptionValues) {
        return new Renderer(this, renderContext, getOptionValues(newtonsoftCSharpOptions, untypedOptionValues));
    }
}

let wrapperClass = "";
let name = "";
let namespace = "";

class Renderer extends NewtonsoftCSharpRenderer {
    emitSourceStructure() {
        this.emitDefaultLeadingComments();

        this.ensureBlankLine();

        this.emitLine("namespace ", this._csOptions.namespace);
        this.emitBlock(() => {
            this.emitUsings();
            this.emitLine("using GraphQL.Typed.Client;");
            this.emitLine("using System.Threading.Tasks;");

            this.ensureBlankLine();

            this.emitLine("public interface I", wrapperClass);
            this.emitBlock(() => {
                this.emitLine(`Task<${wrapperClass}.Data> Fetch(object variables = null);`);
            });

            this.emitLine("public class ", wrapperClass, ": I", wrapperClass);
            this.emitBlock(() => {
                this.emitLine('private readonly GraphQuery _graphQuery;');

                this.ensureBlankLine();

                this.emitLine(`public ${wrapperClass}(GraphQuery graphQuery)`);
                this.emitBlock(() => {
                    this.emitLine('this._graphQuery = graphQuery;');
                });

                this.ensureBlankLine();

                this.emitLine(`public Task<${wrapperClass}.Data> Fetch(object variables = null)`);
                this.emitBlock(() => {
                    this.emitLine(`return this._graphQuery.Fetch<${wrapperClass}.Data>("${namespace}.${wrapperClass}.graphql", variables);`);
                });

                this.emitTypesAndSupport();
            });
        });
    }
}

async function main(program, args) {
    // Exactly one command line argument allowed, the name of the graphql file
    if (args.length !== 2) {
        console.error(`Usage: ${program} schemaFileName fileName`);
        process.exit(1);
    }

    var schemaPath = path.resolve(args[0]);
    var filePath = path.resolve(args[1]);

    return pfs.readFileAsync(schemaPath, 'utf8')
        .then((schema) => pfs.readFileAsync(filePath, 'utf8').then((query) => ({
            schema,
            query
        })))
        .then(({schema, query}) => {
            wrapperClass = path.basename(filePath);
            wrapperClass = wrapperClass.substr(0, wrapperClass.lastIndexOf("."));

            namespace = path.dirname(filePath);
            let idx = namespace.lastIndexOf("\\");
            if(idx === -1) {
                namespace.lastIndexOf("/")
            }
            namespace = namespace.substr(idx + 1);

            name = wrapperClass
                .replace(/Query$/, '')
                .replace(/Mutation$/, '');

            if(name == wrapperClass) {
                return Promise.reject("The file name must match the format <Name>Query.graphql or <Name>Mutation.graphql");
            }

            const inputData = new InputData();
            inputData.addSource(
                "graphql",
                {
                    kind: "graphql",
                    name,
                    schema: JSON.parse(schema),
                    query
                },
                () => new GraphQLInput()
            );

            const lang = new TargetLanguage();

            return quicktype({
                lang,
                inputData,
                rendererOptions: {
                    features: 'attributes-only',
                    namespace: namespace
                }
            });
        })
        .then(({lines}) => {
            return fs.writeFileAsync(filePath + '.generated.cs', lines.join("\n"))
        })
        .catch((err) => {
            console.error(err);
            process.exit(1);
        });
}

main(process.argv[1], process.argv.slice(2));