using GlobalSuppressions;
using Newtonsoft.Json;

namespace GyroPrompt.Basic_Functions.Object_Modifiers
{
    public class DataSerializer
    {
        public string serializeInput(object input)
        {
            object serializedData = JsonConvert.SerializeObject(input);
            return serializedData.ToString();
        }

        public string deserializeInput(object input)
        {
            object deserializedData = JsonConvert.DeserializeObject(input.ToString());
            return deserializedData.ToString();
        }
    }
}
