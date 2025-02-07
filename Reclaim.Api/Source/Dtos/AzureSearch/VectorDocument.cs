using System;
using System.Collections.Generic;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace Reclaim.Api.Dtos.AzureSearch
{
    public class VectorDocument
    {
        [SimpleField(IsKey = true)]
        public string ID { get; set; }

        [SimpleField]
        public int DocumentID { get; set; }

        [SimpleField]
        public int? InvestigatorID { get; set; }

        [SimpleField]
        public int ClaimID { get; set; }

        [SimpleField]
        public string FileName { get; set; }

        [SimpleField]
        public int PageNumber { get; set; }

        [SearchableField]
        public string Content { get; set; }

        [SimpleField]
        public List<string> BoundingBoxes { get; set; }

        [VectorSearchField(Dimensions = 1536)]
        public float[] Embedding { get; set; }
    }

    public class VectorDocumentWithNumberedLines
    {
        public VectorDocument VectorDocument { get; set; }
        public List<NumberedLine> NumberedLines { get; set; }
    }

    public class NumberedLine
    {
        public int Number { get; set; }
        public string Line { get; set; }
    }

    public class VectorDocumentReference
    {
        public int DocumentID { get; set; }
        public string FileName { get; set; }
        public int PageNumber { get; set; }
        public List<string> BoundingBoxes { get; set; }
    }

    public class QueryResult
    {
        public string Answer { get; set; }
        public List<VectorDocumentReference> References { get; set; }
    }

    public class AnswerExtractionResult
    {
        public string Answer { get; set; }
        public List<int> LineNumbers { get; set; }
    }
}