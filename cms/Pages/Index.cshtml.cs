using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InteronBlog.Pages;

public class IndexModel : PageModel
{
    public void OnGet()
    {
    }

    public string GetSchemaJson()
    {
        return @"{
  ""@context"": ""https://schema.org"",
  ""@graph"": [
    {
      ""@type"": ""Organization"",
      ""@id"": ""https://interon.co.za/#organization"",
      ""name"": ""AI Readiness Platform"",
      ""url"": ""https://interon.co.za"",
      ""description"": ""We help businesses become machine-readable through AI Readiness audits, SEO optimization, and structured data implementation."",
      ""knowsAbout"": [
        ""AI Readiness"",
        ""Search Engine Optimization"",
        ""Generative Engine Optimization"",
        ""Schema.org Markup"",
        ""Structured Data"",
        ""JSON-LD"",
        ""Machine Learning Discoverability""
      ],
      ""hasOfferCatalog"": {
        ""@type"": ""OfferCatalog"",
        ""name"": ""AI Readiness Services"",
        ""itemListElement"": [
          {
            ""@type"": ""Offer"",
            ""itemOffered"": {
              ""@type"": ""Service"",
              ""name"": ""Free AI Readiness Audit"",
              ""description"": ""Comprehensive analysis of your website's AI readiness, SEO fundamentals, schema markup, and GEO signals.""
            }
          },
          {
            ""@type"": ""Offer"",
            ""itemOffered"": {
              ""@type"": ""Service"",
              ""name"": ""Schema Implementation"",
              ""description"": ""Professional implementation of Organization, Service, Product, and other critical schema types.""
            }
          },
          {
            ""@type"": ""Offer"",
            ""itemOffered"": {
              ""@type"": ""Service"",
              ""name"": ""AI Readiness Optimization"",
              ""description"": ""Full optimization to make your business discoverable by AI systems like ChatGPT and Google AI.""
            }
          }
        ]
      }
    },
    {
      ""@type"": ""WebSite"",
      ""@id"": ""https://interon.co.za/#website"",
      ""url"": ""https://interon.co.za"",
      ""name"": ""AI Readiness Platform"",
      ""description"": ""Free AI Readiness audits and education for business owners"",
      ""publisher"": {""@id"": ""https://interon.co.za/#organization""}
    },
    {
      ""@type"": ""WebPage"",
      ""@id"": ""https://interon.co.za/#webpage"",
      ""url"": ""https://interon.co.za"",
      ""name"": ""AI Readiness Platform - Make Your Business Machine-Readable"",
      ""isPartOf"": {""@id"": ""https://interon.co.za/#website""},
      ""about"": {""@id"": ""https://interon.co.za/#organization""},
      ""description"": ""Free AI Readiness audit for your website. Discover if AI systems can find, understand, and recommend your business.""
    },
    {
      ""@type"": ""FAQPage"",
      ""mainEntity"": [
        {
          ""@type"": ""Question"",
          ""name"": ""What is AI Readiness?"",
          ""acceptedAnswer"": {
            ""@type"": ""Answer"",
            ""text"": ""AI Readiness refers to how well your online presence can be understood by AI systems like ChatGPT, Google's AI Overview, and voice assistants. A machine-readable business has structured data that clearly tells AI what you do, what you offer, and how to recommend you.""
          }
        },
        {
          ""@type"": ""Question"",
          ""name"": ""Why does AI Readiness matter for my business?"",
          ""acceptedAnswer"": {
            ""@type"": ""Answer"",
            ""text"": ""AI systems are increasingly how people discover businesses. When someone asks ChatGPT or Google AI for recommendations, these systems look for businesses with clear, structured information. Without AI Readiness, your business may be invisible to these new discovery channels.""
          }
        },
        {
          ""@type"": ""Question"",
          ""name"": ""What does the free audit check?"",
          ""acceptedAnswer"": {
            ""@type"": ""Answer"",
            ""text"": ""Our free audit analyzes four key areas: AI Readiness signals (can machines understand your business), Schema markup (structured data implementation), SEO fundamentals (technical health), and GEO signals (Generative Engine Optimization). You receive a detailed report with specific recommendations.""
          }
        },
        {
          ""@type"": ""Question"",
          ""name"": ""What is schema markup?"",
          ""acceptedAnswer"": {
            ""@type"": ""Answer"",
            ""text"": ""Schema markup is structured data added to your website that helps search engines and AI systems understand your content. Think of it like a nutrition label for your website - it provides machine-readable facts about your business, services, products, and more using the Schema.org vocabulary.""
          }
        }
      ]
    }
  ]
}";
    }
}
