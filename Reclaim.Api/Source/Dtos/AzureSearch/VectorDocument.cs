
using System;
using System.Collections.Generic;
using Azure.Search.Documents.Indexes;

namespace Reclaim.Api.Dtos.AzureSearch {
    public class VectorDocument {
        [SimpleField(IsKey = true)]
        public string Id { get; set; }

        [SearchableField]
        public string Content { get; set; }

        [SimpleField]
        public float[] Vector { get; set; }

        [SimpleField]
        public float[] Coordinates { get; set; }

        [SimpleField]
        public int PageNumber { get; set; }

        [SimpleField]
        public int PageWidth { get; set; }

        [SimpleField]
        public int PageHeight { get; set; }
    }
}
