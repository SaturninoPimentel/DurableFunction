using Newtonsoft.Json;

namespace DurableFunction
{
    public class AnalysisResult
    {
        [JsonProperty("categories")]
        public Category[] Categories { get; set; }

        [JsonProperty("description")]
        public Description Description { get; set; }

        [JsonProperty("id")]
        public string Id { get => RequestId; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }
    }

    public class Description
    {
        [JsonProperty("tags")]
        public string[] Tags { get; set; }

        [JsonProperty("captions")]
        public Caption[] Captions { get; set; }
    }

    public class Caption
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("confidence")]
        public float Confidence { get; set; }
    }

    public class Metadata
    {
        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }
    }

    public class Category
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("score")]
        public float Score { get; set; }

        [JsonProperty("detail")]
        public Detail Detail { get; set; }
    }

    public class Detail
    {
        [JsonProperty("landmarks")]
        public object[] Landmarks { get; set; }
    }
}