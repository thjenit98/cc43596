using System.Diagnostics;
using Elasticsearch.Net;
using Microsoft.AspNetCore.Mvc;
using Nest;

namespace lab_FullTextSearch.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductController : ControllerBase
{
    private readonly string CONNECTION_URI = "http://localhost:9200";
    private readonly string INDEX_NAME = "elasticindex";

    [HttpPost]
    [Route("create-product")]
    public object CreateProduct([FromBody] List<ProductDetail> products)
    {
        Stopwatch timer = new Stopwatch();
        timer.Start();

        ElasticClient elasticClient = GetElasticConnection(INDEX_NAME);

        var esIndexExists = elasticClient.Indices.Exists(INDEX_NAME);
        if (!esIndexExists.Exists)
        {
            // Create index
            CreateIndexResponse createIndexResponse = elasticClient.Indices.Create(
                INDEX_NAME,
                x => x
                // Setting Analyzers Phonetic
                .Settings(s => s
                    .Analysis(a => a
                        .TokenFilters(tf => tf
                            .Phonetic("phonetic_soundex", p => p
                                // DoubleMetaphone is an improved version of Metaphone and Soundex, helping to match words with similar pronunciations for more accurate results
                                .Encoder(PhoneticEncoder.DoubleMetaphone)
                            )
                        )
                        .Analyzers(an => an
                            .Custom("phonetic_analyzer", ca => ca
                                .Tokenizer("standard")
                                .Filters("lowercase", "phonetic_soundex")
                            )
                        )
                    )
                )
                // Mapping Analyzers to Model
                .Map<ProductDetail>(m => m
                    .Properties(p => p
                        .Text(t => t
                            .Name(n => n.ProductName)
                            .Analyzer("phonetic_analyzer")
                            .Fields(f => f
                                .Text(t => t.Name("keyword").Analyzer("standard"))
                            )
                        )
                    )
                )
                // Mapping any more models
            );
        }

        // Create Documents
        var indexResponse = elasticClient.IndexMany(products
            .Select(product => new ProductDetail()
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Price = product.Price,
                Description = product.Description,
                Colors = product.Colors,
                Tags = product.Tags
            })
        );

        timer.Stop();

        return new
        {
            Ok = 200,
            Time = timer.ElapsedMilliseconds,
            DebugInformation = indexResponse.DebugInformation,
            ServerError = indexResponse.ServerError?.Error,
        };
    }

    [HttpPost]
    [Route("search-product")]
    public object EsSearch([FromBody] string term)
    {
        ElasticClient elasticClient = GetElasticConnection(INDEX_NAME);

        // Searching with Fuzzy Auto
        var responseData = elasticClient.Search<ProductDetail>(s => s
            .Query(q => q
                .Bool(b => b
                    .Should(
                        sh => sh.Match(m => m
                            .Field(f => f.ProductName)
                            .Query(term)
                            .Fuzziness(Fuzziness.Auto)
                        ),
                        sh => sh.Match(m => m
                            .Field(f => f.ProductName.Suffix("keyword"))
                            .Query(term)
                            .Fuzziness(Fuzziness.Auto)
                        )
                    )
                )
            )
        );

        return responseData.Documents.ToList();
    }

    // Establish a connection to Elasticsearch.
    private ElasticClient GetElasticConnection(string indexName)
    {
        var nodes = new Uri[] { new Uri(CONNECTION_URI) };
        var connectionPool = new StaticConnectionPool(nodes);
        var connectionSettings = new ConnectionSettings(connectionPool).DisableDirectStreaming();
        var elasticClient = new ElasticClient(connectionSettings.DefaultIndex(indexName));

        return elasticClient;
    }
}
