using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterTrainer.PBI
{
    public class PBIItem : ICloneable
    {
        public DateTime Timestamp { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float Anger { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float Contempt { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float Disgust { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float Fear { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float Happiness { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float Neutral { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float Sadness { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float Surprise { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float AvgHappiness { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float HappinessHigh { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float HappinessLow { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float AvgSadness { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float SadnessHigh { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float SadnessLow { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float AvgAnger { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float AngerHigh { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float AngerLow { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float AvgNeutral { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float NeutralHigh { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float NeutralLow { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float AvgContempt { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float ContemptHigh { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float ContemptLow { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float AvgDisgust { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float DisgustHigh { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float DisgustLow { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float AvgSurprise { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float SurpriseHigh { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float SurpriseLow { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float AvgFear { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float FearHigh { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float FearLow { get; set; }
        public int NumOfInstances { get; set; }
        public int NumberOfWords { get; set; }
        public int NumberOfUniqueWords { get; set; }
        public int FacesRecognized { get; set; }
        public string Beseda { get; set; }
        public int FacesInMeasurment { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float ATAvgHappiness { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float ATAvgSadness { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float ATAvgAnger { get; set; }
        public string Speaker { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float ATAvgNeutral { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float ATAvgContempt { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float ATAvgDisgust { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float ATAvgSurprise { get; set; }
        [JsonConverter(typeof(DecimalFormatConverter))]
        public float ATAvgFear { get; set; }
        public string Language { get; set; }
        public string Gender { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
